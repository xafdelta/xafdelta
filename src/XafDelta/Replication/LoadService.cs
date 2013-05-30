using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using XafDelta.Localization;
using XafDelta.Messaging;

namespace XafDelta.Replication
{
    /// <summary>
    /// Load service
    /// </summary>
    internal sealed class LoadService: BaseService, ILoadService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadService"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public LoadService(XafDeltaModule owner) : base(owner)
        {
        }

        private static long loadNesting;
        /// <summary>
        /// Gets a value indicating whether service is in loading state.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoading { get { return Interlocked.Read(ref loadNesting) > 0; } }

        public void BeginLoad()
        {
            Interlocked.Increment(ref loadNesting);
        }

        public void EndLoad()
        {
            Interlocked.Decrement(ref loadNesting);
        }

        /// <summary>
        /// Loads pending packages.
        /// </summary>
        /// <param name="worker">The worker.</param>
        public bool Load(ActionWorker worker)
        {
            if (IsLoading) return false;

            bool loadResult;
            Owner.DoBeforeLoad(new LoadEventArgs(worker));
            worker.ReportProgress(Localizer.LoadingStarted);
            Interlocked.Increment(ref loadNesting);
            try
            {
                using (var applicationObjectSpace = XafDeltaModule.XafApp.CreateObjectSpace())
                {
                    var currentNodeId = ReplicationNode.GetCurrentNodeId(applicationObjectSpace);

                    // select pending input packages
                    var inputPackages =
                        applicationObjectSpace.GetObjects<Package>(CriteriaOperator.Parse(
                        "ApplicationName = ? And (RecipientNodeId = ? Or RecipientNodeId = ?) " +
                        "And (SenderNodeId <> ?)", 
                        Owner.ApplicationName, currentNodeId, ReplicationNode.AllNodes, 
                        Owner.CurrentNodeId)).
                        Where(x => x.LoadedDateTime == DateTime.MinValue).ToList();

                    worker.ReportProgress(string.Format(Localizer.PackagesSelectedForLoading, inputPackages.Count));

                    // load each package until cancellation or error occured
                    loadResult = true;
                    foreach (var inputPackage in inputPackages.OrderBy(x => x.PackageDateTime).TakeWhile(z => !worker.CancellationPending))
                    {
                        // loadResult &= LoadPackage(new LoadPackageContext(inputReplica,worker, applicationObjectSpace, currentNodeId));
                        loadResult &= LoadPackage(worker, inputPackage);
                        if(!loadResult) break;
                    }
                       
                    applicationObjectSpace.CommitChanges();
                }
            }
            finally
            {
                Interlocked.Decrement(ref loadNesting);
                worker.ReportProgress(Color.Blue, Localizer.LoadingIsFinished);
            }
            Owner.DoAfterLoad(new LoadEventArgs(worker));
            return loadResult;
        }

        /// <summary>
        /// Loads the package.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="package">The package.</param>
        public bool LoadPackage(ActionWorker worker, Package package)
        {
            bool result;
            Interlocked.Increment(ref loadNesting);
            try
            {
                using (var applicationObjectSpace = XafDeltaModule.XafApp.CreateObjectSpace())
                {
                    var currentNodeId = ReplicationNode.GetCurrentNodeId(applicationObjectSpace);
                    result = LoadPackage(new LoadPackageContext(package, worker,
                            applicationObjectSpace, currentNodeId));
                    applicationObjectSpace.CommitChanges();
                }
                var packageObjectSpace = ObjectSpace.FindObjectSpaceByObject(package);
                packageObjectSpace.CommitChanges();
            }
            finally
            {
                Interlocked.Decrement(ref loadNesting);
            }
            return result;
        }


        /// <summary>
        /// Loads the package.
        /// </summary>
        /// <param name="context">The load package params.</param>
        public bool LoadPackage(LoadPackageContext context)
        {
            // prevent loading self packages
            if (context.Package.SenderNodeId == context.CurrentNodeId) 
                return false;

            // don't load packages builded before last loaded snapshot
            var lastLoadedSnapshotDateTime = (from c in context.ObjectSpace.GetObjects<ReplicationNode>() select c.InitDateTime).Max();
            if(context.Package.PackageDateTime < lastLoadedSnapshotDateTime)
                return true;

            var senderNode = ReplicationNode.FindNode(context.ObjectSpace, context.Package.SenderNodeId);
            context.Worker.ReportProgress(string.Format(Localizer.LoadingPackage, context.Package));

            var loadResult = false;
            if (packageIsValid(context, senderNode))
            {
                Owner.DoBeforeLoadPackage(new LoadPackageEventArgs(context.Package));
                context.Package.CreateLogRecord(PackageEventType.Loading, "");
                var errorString = "";
                switch (context.Package.PackageType)
                {
                    case PackageType.Protocol:
                        errorString = Owner.ProtocolReplicationService.LoadProtocolPackage(context);
                        break;
                    case PackageType.Snapshot:
                        errorString = Owner.SnapshotService.LoadSnapshotPackage(context);
                        break;
                }
                loadResult = string.IsNullOrEmpty(errorString);

                // rollback changes on error or abort
                if (!loadResult)
                {
                    Owner.DoPackageLoadingError(new PackageLoadingErrorArgs(context, errorString));
                    context.ObjectSpace.Rollback();

                    // add and save log record
                    context.Package.CreateLogRecord(PackageEventType.Failed, errorString);
                    context.ObjectSpace.CommitChanges();

                    context.Worker.ReportError(Localizer.PackageLoadingIsFailed, context.Package);
                }
                else
                {
                    // update last loaded package id for sender node
                    senderNode = senderNode ?? ReplicationNode.FindNode(context.ObjectSpace,
                                                                        context.Package.SenderNodeId);

                    if (senderNode != null)
                    {
                        switch (context.Package.PackageType)
                        {
                            case PackageType.Protocol:
                                senderNode.LastLoadedPackageNumber = context.Package.PackageId;
                                break;
                            case PackageType.Snapshot:
                                senderNode.LastLoadedSnapshotNumber = context.Package.PackageId;
                                senderNode.InitDateTime = context.Package.PackageDateTime;
                                break;
                        }
                    }

                    context.Package.CreateLogRecord(PackageEventType.Loaded, "");

                    // save changes
                    context.ObjectSpace.CommitChanges();
                    Owner.DoAfterLoadPackage(new LoadPackageEventArgs(context.Package));

                    context.Worker.ReportProgress(Color.Blue, Localizer.PackageLoadingCompleted, context.Package);
                }
            }
            else
            {
                Owner.DoInvalidPackage(new InvalidPackageArgs(context));
                context.Worker.ReportError( Localizer.PackageRejected, context.Package);
            }
            return loadResult;
        }

        /// <summary>
        /// Verify package the is valid.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="senderNode">The sender node.</param>
        /// <returns></returns>
        private bool packageIsValid(LoadPackageContext context, ReplicationNode senderNode)
        {
            var packageIsValid = true;
            var errors = new List<string>();
            if (!Owner.AnonymousPackagesAllowed && senderNode == null)
            {
                packageIsValid = false;
                var errorText = string.Format(Localizer.SenderNodeIsNotFound,
                                              context.Package.SenderNodeId, context.Package);
                context.Worker.ReportError( errorText);
                errors.Add(errorText);
            }

            var expectedPackageId = 1;
            if (senderNode != null)
            {
                switch (context.Package.PackageType)
                {
                    case PackageType.Protocol:
                        expectedPackageId = senderNode.LastLoadedPackageNumber + 1;
                        break;
                    case PackageType.Snapshot:
                        expectedPackageId = senderNode.LastLoadedSnapshotNumber + 1;
                        break;
                }
            }

            if (context.Package.PackageId != expectedPackageId)
            {
                packageIsValid = false;
                var errorText = string.Format(Localizer.InvalidPackageId,
                                              context.Package, context.Package.PackageId, expectedPackageId);
                context.Worker.ReportError( errorText);
                errors.Add(errorText);
            }

            if(context.Package.PackageData == null || context.Package.PackageData.Length == 0)
            {
                packageIsValid = false;
                var errorText = string.Format(Localizer.PackageDataIsEmpty, context.Package);
                context.Worker.ReportError( errorText);
                errors.Add(errorText);
            }

            if(!packageIsValid)
            {
                var errorString = string.Join("\n", errors.ToArray());
                context.Package.CreateLogRecord(PackageEventType.Rejected, errorString);
            }

            return packageIsValid;
        }
 
    }

    #region Event args

    /// <summary>
    /// Package loading error args
    /// </summary>
    public class PackageLoadingErrorArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageLoadingErrorArgs"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="errorString"></param>
        public PackageLoadingErrorArgs(LoadPackageContext context, string errorString)
        {
            ErrorString = errorString;
            Package = context.Package;
            Worker = context.Worker;
            ApplicationObjectSpace = context.ObjectSpace;
            CurrentNodeId = context.CurrentNodeId;
        }

        /// <summary>
        /// Gets the description of error occured while loading.
        /// </summary>
        public string ErrorString { get; private set; }

        /// <summary>
        /// Gets the failed package.
        /// </summary>
        public Package Package { get; private set; }

        /// <summary>
        /// Gets the action worker assigned to load operation.
        /// </summary>
        public ActionWorker Worker { get; private set; }

        /// <summary>
        /// Gets the application object space.
        /// </summary>
        public IObjectSpace ApplicationObjectSpace { get; private set; }

        /// <summary>
        /// Gets the current node id.
        /// </summary>
        public string CurrentNodeId { get; private set; }
    }

    /// <summary>
    /// Arguments for <see cref="XafDeltaModule.InvalidPackage"/> event.
    /// </summary>
    public class InvalidPackageArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPackageArgs"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public InvalidPackageArgs(LoadPackageContext context)
        {
            ApplicationObjectSpace = context.ObjectSpace;
            Package = context.Package;
        }

        /// <summary>
        /// Gets or sets the invalid package.
        /// </summary>
        /// <value>
        /// The nivalid package.
        /// </value>
        public Package Package { get; private set; }

        /// <summary>
        /// Gets or sets the application object space.
        /// </summary>
        /// <value>
        /// The application object space.
        /// </value>
        public IObjectSpace ApplicationObjectSpace { get; private set; }
    }

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.BeforeLoadRecord"/> and <see cref="XafDeltaModule.AfterLoadPackage"/> events.
    /// </summary>
    public class LoadPackageEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the package loading.
        /// </summary>
        public Package Package { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadPackageEventArgs"/> class.
        /// </summary>
        /// <param name="package">The package.</param>
        public LoadPackageEventArgs(Package package)
        {
            Package = package;
        }
    }

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.BeforeLoad"/> and <see cref="XafDeltaModule.AfterLoad"/> events.
    /// </summary>
    public class LoadEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the action worker assigned to loading process.
        /// </summary>
        public ActionWorker Worker { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadEventArgs"/> class.
        /// </summary>
        /// <param name="worker">The worker.</param>
        public LoadEventArgs(ActionWorker worker)
        {
            Worker = worker;
        }
    }

    #endregion

}