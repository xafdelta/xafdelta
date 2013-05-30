using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.Metadata;
using XafDelta.Exceptions;
using XafDelta.Localization;
using XafDelta.Messaging;
using XafDelta.Protocol;
using NestedObjectSpace = DevExpress.ExpressApp.NestedObjectSpace;

namespace XafDelta.Replication
{
    /// <summary>
    /// Protocol Replication Service
    /// </summary>
    internal sealed class ProtocolReplicationService : BaseService
    {
        public ProtocolReplicationService(XafDeltaModule owner) : base(owner)
        {
        }

        #region Load

        /// <summary>
        /// Loads the package.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public string LoadProtocolPackage(LoadPackageContext context)
        {
            var result = string.Empty;

            try
            {
                using (var uow = context.Package.UnitOfWork)
                {
                    var topSessions = new XPCollection<PackageSession>(uow, CriteriaOperator.Parse("IsNull(Parent)"));

                    var senderNode = ReplicationNode.FindNode(context.ObjectSpace, context.Package.SenderNodeId);

                    topSessions.OrderBy(x => x.CommitedOn.Add(x.UtcOffset))
                        /* skip sessions commited before last loaded snapshot */
                        .Where(x => senderNode == null || x.CommitedOn > senderNode.InitDateTime) 
                        .TakeWhile(z => !context.Worker.CancellationPending).ToList()
                        .ForEach(y => loadTopSession(new LoadPackageSessionContext(context, y)));

                    context.ObjectSpace.CommitChanges();
                }
            }
            catch (Exception exception)
            {
                result = string.Format(Localizer.PackageLoadError, exception.Message);
                context.Worker.ReportError( result);
            }

            if (string.IsNullOrEmpty(result))
            {
                if (context.Worker.CancellationPending)
                    result = Localizer.LoadingAborted;

                context.Worker.ReportProgress(Color.Blue, Localizer.LoadingIs, 
                    (context.Worker.CancellationPending ? Localizer.Aborted 
                    : Localizer.Finished));
            }

            return result;
        }

        private void loadTopSession(LoadPackageSessionContext context)
        {
            var objectSpace = context.ObjectSpace;
            var criteria = CriteriaOperator.Parse("SessionId = ?", context.PackageSession.SessionId);


            // avoid duplicate session loading
            if (objectSpace.FindObject<ProtocolSession>(criteria, true) == null)
            {
                var sessionTree = context.PackageSession.AllChildren;
                var allRecords = (from s in sessionTree from r in s.PackageRecords 
                                  orderby r.ModifiedOn select r).ToList();

                allRecords = allRecords.Distinct().ToList();

                var args = new LoadSessionArgs(context, allRecords);
                var activeObjectSpaces = new Dictionary<Guid, IObjectSpace>();
                try
                {
                    if (!Owner.DoBeforeLoadSession(args))
                    {
                        allRecords.ForEach(y => loadPackageRecord(new LoadPackageRecordContext(context, 
                            y, activeObjectSpaces)));
                    }
                    if (activeObjectSpaces.Count > 0)
                    {
                        commitObjectSpaceOnLoad(activeObjectSpaces.Values.ElementAt(0), activeObjectSpaces);
                        activeObjectSpaces.Clear();
                    }
                }
                finally
                {
                    if (activeObjectSpaces.Count > 0)
                    {
                        // first element in values is top session
                        activeObjectSpaces.Values.ElementAt(0).Rollback();
                        activeObjectSpaces.Values.ElementAt(0).Dispose();
                        activeObjectSpaces.Clear();
                    }
                }

                createProtocolRecords(allRecords, context);

                Owner.DoAfterLoadSession(args);
            }
            else
            {
                var errorText = string.Format(Localizer.SessionAlreadyLoaded, 
                    context.PackageSession.SessionId);

                context.Worker.ReportProgress(Color.BlueViolet, errorText);
                context.Package.CreateLogRecord(PackageEventType.SessionAlreadyLoaded, errorText);
            }
        }

        private void createProtocolRecords(IEnumerable<PackageRecord> allRecords, LoadPackageContext context)
        {
            if (Owner.CreateExternalProtocolRecords)
            {
                using (var objectSpace = XafDeltaModule.XafApp.CreateObjectSpace())
                {
                    foreach (var packageRecord in allRecords)
                    {
                        var protocolRecord = ProtocolRecord.CreateForPackageRecord(objectSpace,
                                                                                   packageRecord,
                                                                                   context.Package.SenderNodeId);

                        protocolRecord.ProtocolSession = ExternalProtocolSession.GetSession(objectSpace, packageRecord.PackageSession);
                    }
                    objectSpace.CommitChanges();
                }
            }
        }

        /// <summary>
        /// Commits the object space on load.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="activeObjectSpaces">The active object spaces.</param>
        private void commitObjectSpaceOnLoad(IObjectSpace objectSpace, Dictionary<Guid, IObjectSpace> activeObjectSpaces)
        {
            var children = from s in activeObjectSpaces.Values
                           where s is NestedObjectSpace && ((NestedObjectSpace) s).ParentObjectSpace == objectSpace
                           select s;
            foreach (var child in children)
                commitObjectSpaceOnLoad(child, activeObjectSpaces);

            var newMaps = from c in objectSpace.ModifiedObjects.Cast<object>()
                          where c is OidMap && ((OidMap) c).NewObject != null 
                          select c as OidMap;

            objectSpace.CommitChanges();
            
            newMaps.ToList().ForEach(x => x.FixReference());

            objectSpace.CommitChanges();

            objectSpace.Dispose();
            var sessionId = (from a in activeObjectSpaces where a.Value == objectSpace select a.Key).FirstOrDefault();
            activeObjectSpaces.Remove(sessionId);
        }

        /// <summary>
        /// Loads the package record.
        /// </summary>
        /// <param name="context">The context.</param>
        private void loadPackageRecord(LoadPackageRecordContext context)
        {
            var args = new LoadRecordArgs(context);
            var objectSpace = getPlaybackObjectSpace(context.PackageSession, context.ActiveObjectSpaces);
            if (!Owner.DoBeforeLoadRecord(args))
            {
                var operationType =
                    (ProtocolEventType) Enum.Parse(typeof (ProtocolEventType), context.PackageRecord.OperationType);

                if (operationType == ProtocolEventType.CommitSession)
                {
                    commitObjectSpaceOnLoad(objectSpace, context.ActiveObjectSpaces);
                }
                else
                {
                    var senderNodeId = context.Package.SenderNodeId;
                    var targetObject = OidMap.FindApplicationObject(objectSpace,
                                                                    context.PackageRecord.AuditedObject, senderNodeId);

                    ITypeInfo targetTypeInfo = null;
                    if (targetObject != null)
                        targetTypeInfo = objectSpace.TypesInfo.FindTypeInfo(targetObject.GetType());

                    XPMemberInfo memberInfo = null;
                    var propertyIsEmpty = string.IsNullOrEmpty(context.PackageRecord.PropertyName);
                    if (!propertyIsEmpty && targetObject != null && targetTypeInfo != null)
                    {
                        memberInfo = ((ISessionProvider)targetObject).Session.GetClassInfo(targetObject)
                            .GetMember(context.PackageRecord.PropertyName); 
                    }

                    bool oldObjectShouldExists;
                    var oldObject = restoreObjectFromRef(objectSpace, senderNodeId,
                                                         context.PackageRecord.OldObject, out oldObjectShouldExists);

                    bool newObjectShouldExists;
                    var newObject = restoreObjectFromRef(objectSpace, senderNodeId,
                                                         context.PackageRecord.NewObject, out newObjectShouldExists);

                    // collision resolving
                    var replres = resolveCollisions(context, oldObject, newObjectShouldExists,
                                                    newObject, operationType, oldObjectShouldExists, targetObject,
                                                    memberInfo, propertyIsEmpty);

                    if (replres == CollisionResult.Default)
                    {
                        switch (operationType)
                        {
                            case ProtocolEventType.MethodCall:
                                if(targetObject != null)
                                {
                                    if (targetTypeInfo != null)
                                    {
                                        var classType = targetTypeInfo.Type;
                                        if (context.PackageRecord != null 
                                            && !string.IsNullOrEmpty(context.PackageRecord.PropertyName))
                                        {
                                            var callParameters = (MethodCallParams)oldObject;

                                            var methodInfo = (from m in classType.GetMethods() 
                                                              where m.Name == context.PackageRecord.PropertyName 
                                                              && m.GetParameters().Count() == callParameters.MethodParamValues.Count 
                                                              select m).FirstOrDefault();

                                            if (methodInfo != null)
                                            {
                                                var paramValues = callParameters.GetParamValues();
                                                methodInfo.Invoke(targetObject, paramValues);
                                            }
                                        }
                                    }
                                }
                                break;

                            case ProtocolEventType.AddedToCollection:
                                if (memberInfo != null && memberInfo.IsAssociationList && oldObject != null)
                                {
                                    var collObj = memberInfo.GetValue(targetObject);
                                    if (collObj != null)
                                    {
                                        if(collObj.GetType().GetInterface("IList") != null)
                                        {
                                            var list = (IList) collObj;
                                            if(!list.Contains(oldObject))
                                                list.Add(oldObject);
                                        }
                                    }
                                }
                                break;

                            case ProtocolEventType.AggregatedObjectChanged:
                                break;

                            case ProtocolEventType.CollectionObjectChanged:
                                break;

                            case ProtocolEventType.InitialValueAssigned:
                            case ProtocolEventType.ObjectChanged:
                                if (memberInfo != null)
                                {
                                    object newValue = null;
                                    if (context.PackageRecord.NewBlobValue != null)
                                    {
                                        newValue = ValueTransform.RestoreFromBlob(context.PackageRecord.NewBlobValue);
                                    }
                                    else
                                    {
                                        // NewBlobValue is empty - restore value from context.PackageRecord.NewValue
                                        if (context.PackageRecord.NewValue != null)
                                        {
                                            if (memberInfo.ReferenceType == null)
                                            {
                                                if (!string.IsNullOrEmpty(context.PackageRecord.NewValue))
                                                    newValue = ValueTransform.StringToObject(context.PackageRecord.NewValue,
                                                        memberInfo.MemberType);
                                                else
                                                    newValue = memberInfo.MemberType.IsValueType ? 
                                                        Activator.CreateInstance(memberInfo.MemberType) : null;
                                            }
                                            else
                                                newValue = newObject;
                                        }
                                    }

                                    // replace aggregated members initialization with oidmap redirection
                                    if (operationType == ProtocolEventType.InitialValueAssigned
                                        && memberInfo.IsAggregated && !memberInfo.IsAssociationList && newValue != null)
                                    {
                                        var oldAggregatedObj = memberInfo.GetValue(targetObject);
                                        if (oldAggregatedObj != null)
                                        {
                                            var mapItem = OidMap.GetOidMap(newValue, context.Package.SenderNodeId);
                                            if (mapItem != null)
                                            {
                                                // redirect aggregated object reference to oldAggregatedObj
                                                if (mapItem.Target == null)
                                                    mapItem.NewObject = oldAggregatedObj;
                                                else
                                                    mapItem.Target.Target = oldAggregatedObj;
                                            }
                                        }
                                        else
                                            memberInfo.SetValue(targetObject, newValue);
                                    }

                                    // skip object initialization for objects already exists in app database
                                    if (operationType == ProtocolEventType.InitialValueAssigned
                                        && !((ISessionProvider)targetObject).Session.IsNewObject(targetObject))
                                        break;


                                    memberInfo.SetValue(targetObject, newValue);
                                }
                                break;

                            case ProtocolEventType.ObjectCreated:
                                if (targetObject == null)
                                {
                                    var reference = context.PackageRecord.AuditedObject;
                                    var className = reference.ClassName;
                                    var classType = objectSpace.TypesInfo.FindTypeInfo(className).Type;

                                    targetObject = objectSpace.CreateObject(classType);
                                    OidMap.CreateOidMap(objectSpace, reference, senderNodeId, targetObject);
                                }
                                break;

                            case ProtocolEventType.ObjectDeleted:
                                if (targetObject != null)
                                    objectSpace.Delete(targetObject);
                                break;

                            case ProtocolEventType.RemovedFromCollection:
                                if (memberInfo != null && memberInfo.IsAssociationList && oldObject != null)
                                {
                                    var collObj = memberInfo.GetValue(targetObject);
                                    if (collObj != null)
                                    {
                                        if(collObj is XPBaseCollection)
                                        {
                                            var collection = (XPBaseCollection) collObj;
                                            if (collection.Cast<object>().Contains(oldObject))
                                                collection.BaseRemove(oldObject);
                                        }
                                        else if(collObj.GetType().GetInterface("IList") != null)
                                        {
                                            var list = (IList) collObj;
                                            if(list.Contains(oldObject))
                                                list.Remove(oldObject);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            Owner.DoAfterLoadRecord(args);
        }

        /// <summary>
        /// Gets the playback object space.
        /// </summary>
        /// <param name="packageSession">The package session.</param>
        /// <param name="activeObjectSpaces">The active object spaces.</param>
        /// <returns></returns>
        private IObjectSpace getPlaybackObjectSpace(PackageSession packageSession, 
            Dictionary<Guid, IObjectSpace> activeObjectSpaces)
        {
            IObjectSpace result;
            if(!activeObjectSpaces.TryGetValue(packageSession.SessionId, out result))
            {
                if (packageSession.Parent != null)
                {
                    var parentObs = getPlaybackObjectSpace(packageSession.Parent, activeObjectSpaces);
                    result = parentObs.CreateNestedObjectSpace();
                }
                else
                {
                    result = XafDeltaModule.XafApp.CreateObjectSpace();
                }
                activeObjectSpaces.Add(packageSession.SessionId, result);
            }
            return result;
        }

        /// <summary>
        /// Resolves the collisions.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="oldObject">The old object.</param>
        /// <param name="newObjectShouldExists">if set to <c>true</c> [new object should exists].</param>
        /// <param name="newObject">The new object.</param>
        /// <param name="operationType">Type of the operation.</param>
        /// <param name="oldObjectShouldExists">if set to <c>true</c> [old object should exists].</param>
        /// <param name="targetObject">The target object.</param>
        /// <param name="memberInfo">The member info.</param>
        /// <param name="propertyIsEmpty">if set to <c>true</c> [property is empty].</param>
        /// <returns></returns>
        private CollisionResult resolveCollisions(LoadPackageRecordContext context, object oldObject,
                                                  bool newObjectShouldExists, object newObject,
                                                  ProtocolEventType operationType,
                                                  bool oldObjectShouldExists, object targetObject,
                                                  XPMemberInfo memberInfo, bool propertyIsEmpty)
        {
            var replres = CollisionResult.Default;

            if (operationType == ProtocolEventType.ObjectCreated && targetObject != null)
                replres = resolveReplicationCollision(context,
                                                      CollisionType.TargetObjectAlreadyExists, targetObject);

            if (operationType != ProtocolEventType.ObjectCreated && targetObject == null)
                replres = resolveReplicationCollision(context,
                                                      CollisionType.TargetObjectIsNotFound);

            if (!propertyIsEmpty && memberInfo == null)
                replres = resolveReplicationCollision(context,
                                                      CollisionType.MemberIsNotFound, targetObject);

            if (!propertyIsEmpty && memberInfo != null && !memberInfo.IsAssociationList && memberInfo.IsReadOnly)
                replres = resolveReplicationCollision(context,
                                                      CollisionType.ChangeReadOnlyMember, targetObject);

            if (oldObjectShouldExists && oldObject == null)
                replres = resolveReplicationCollision(context,
                                                      CollisionType.OldObjectIsNotFound, targetObject);

            if (newObjectShouldExists && newObject == null)
                replres = resolveReplicationCollision(context,
                                                      CollisionType.NewObjectIsNotFound, targetObject);
            return replres;
        }

        /// <summary>
        /// Restores the object from reference.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="senderNodeId">The sender node id.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="objectShouldExists">if set to <c>true</c> [object should exists].</param>
        /// <returns></returns>
        private static object restoreObjectFromRef(IObjectSpace objectSpace, string senderNodeId,
                                                   ObjectReference reference, out bool objectShouldExists)
        {
            object result = null;
            objectShouldExists = reference != null && reference.IsAssigned;
            if (objectShouldExists)
                result = OidMap.FindApplicationObject(objectSpace, reference, senderNodeId);
            return result;
        }

        /// <summary>
        /// Resolves the replication collision.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="collisionType">Type of the collision.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        private CollisionResult resolveReplicationCollision(LoadPackageRecordContext context,
                                                            CollisionType collisionType, params object[] args)
        {
            var e = new ResolveReplicationCollisionArgs(context, collisionType, args);
            Owner.DoResolveReplicationCollision(e);
            if (e.ResolveResult == CollisionResult.Error)
                throw new ReplicationCollisionException(collisionType);

            context.Worker.ReportProgress(Color.Goldenrod, 
                Localizer.CollisionDetected, 
                collisionType, context.PackageRecord, e.ResolveResult);

            return e.ResolveResult;
        }

        #endregion

        #region Build

        /// <summary>
        /// Protocol session source
        /// </summary>
        private class BuildSessionInfo
        {
            public static readonly Dictionary<ReplicationNode, List<BuildSessionInfo>> PacketTargets =
                new Dictionary<ReplicationNode, List<BuildSessionInfo>>();

            private BuildSessionInfo(ReplicationNode targetNode, ProtocolSession protocolSession)
            {
                TargetNode = targetNode;
                ProtocolSession = protocolSession;
                ProtocolRecords = new List<ProtocolRecord>();
            }

            public static BuildSessionInfo GetInstance(ReplicationNode targetNode, ProtocolSession protocolSession)
            {
                List<BuildSessionInfo> list;
                if (!PacketTargets.TryGetValue(targetNode, out list))
                {
                    list = new List<BuildSessionInfo>();
                    PacketTargets.Add(targetNode, list);
                }
                var result = (from c in list where ReferenceEquals(c.ProtocolSession, protocolSession) select c).FirstOrDefault();
                if (result == null)
                {
                    result = new BuildSessionInfo(targetNode, protocolSession);
                    list.Add(result);
                    if (protocolSession.Parent != null)
                        result.parent = GetInstance(targetNode, protocolSession.Parent);
                }
                return result;
            }
            public ReplicationNode TargetNode { get; private set; }
            private BuildSessionInfo parent { get; set; }
            public IEnumerable<BuildSessionInfo> AllParents
            {
                get
                {
                    var result = new List<BuildSessionInfo>();
                    if (parent != null)
                        result.AddRange(parent.AllParents);
                    result.Add(this);
                    return result;
                }
            }
            public ProtocolSession ProtocolSession { get; private set; }
            public List<ProtocolRecord> ProtocolRecords { get; private set; }
        }

        /// <summary>
        /// Create and save packages for pending records.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <returns></returns>
        public bool BuildPackages(ActionWorker worker)
        {
            bool result;
            worker.ReportProgress(Localizer.BuildingPackages);

            using (var applicationObjectSpace = XafDeltaModule.XafApp.CreateObjectSpace())
            {
                Owner.DoBeforeBuildPackages(new BeforeBuildPackagesArgs(applicationObjectSpace));

                try
                {
                    var unsavedSessions =
                        applicationObjectSpace.GetObjects<ProtocolSession>(CriteriaOperator.Parse("Not SessionIsSaved"));

                    // select session (don't replicate external sessions for broadcasts)
                    var sessionsToSave = (from c in unsavedSessions where !(c is ExternalProtocolSession) 
                                              || (Owner.ReplicateExternalData 
                                              && Owner.RoutingType != RoutingType.BroadcastRouting)
                                          orderby c.CommitedOn select c).ToList();

                    var rootContext = new SaveProtocolParams(worker, applicationObjectSpace,
                                                              new Dictionary<ReplicationNode, PackageSaveInfo>(),
                                                              applicationObjectSpace.GetObjects<ReplicationNode>(),
                                                              ReplicationNode.GetCurrentNode(applicationObjectSpace));

                    prepareDataForSave(rootContext, sessionsToSave);
                    createPackages(rootContext);

                    // close created packages
                    rootContext.CreatedPackages.Values.ToList().ForEach(x => x.Package.CloseUnitOfWork(true));

                    result = !worker.CancellationPending;

                    if(result)
                        Owner.DoAfterBuildPackages(new AfterBuildPackagesArgs(from c in 
                            rootContext.CreatedPackages.Values select c.Package));

                }
                catch (Exception exception)
                {
                    worker.ReportError( 
                        Localizer.PackageSaveFailed, exception.Message);
                    result = false;
                }

                // on success, commit changes to app database and package storage
                if (result)
                    applicationObjectSpace.CommitChanges();
            }


            if (result)
                worker.ReportProgress(Color.Blue, Localizer.PackageSavingIs,
                                                  (worker.CancellationPending ? Localizer.Aborted : Localizer.Finished));


            return result;
        }

        /// <summary>
        /// Creates the packages for prepared data.
        /// </summary>
        /// <param name="rootContext">The root context.</param>
        private void createPackages(SaveProtocolParams rootContext)
        {
            if (rootContext.Worker.CancellationPending) return;
            removeEmptySessions();
            var i = 0;
            foreach (var sessionList in BuildSessionInfo.PacketTargets.Values)
            {
                rootContext.Worker.ReportPercent((double)i++ / (double)BuildSessionInfo.PacketTargets.Values.Count());
                if (rootContext.Worker.CancellationPending) break;
                foreach (var saveSessionInfo in sessionList)
                {
                    if (rootContext.Worker.CancellationPending) break;
                    var sessionParams = new SaveProtocolSessionParams(rootContext, saveSessionInfo.ProtocolSession);
                    foreach (var protocolRecord in saveSessionInfo.ProtocolRecords)
                    {
                        if (rootContext.Worker.CancellationPending) break;
                        var recordParams = new SaveProtocolRecordParams(sessionParams, protocolRecord);
                        createPackageRecord(recordParams, saveSessionInfo.TargetNode);
                    }
                }
            }
            BuildSessionInfo.PacketTargets.Clear();
        }

        /// <summary>
        /// Removes sessions having the only commit event.
        /// </summary>
        private void removeEmptySessions()
        {
            foreach (var targetNode in BuildSessionInfo.PacketTargets.Keys)
            {
                var sessionList = BuildSessionInfo.PacketTargets[targetNode];

                // select session having real events (except commits)
                var notEmptySessions = from c in sessionList where 
                                           (from r in c.ProtocolRecords 
                                            where r.OperationType != "CommitSession" select r).Count()>0 
                                       select c;

                var notEmptyTree = (from c in notEmptySessions from p in c.AllParents select p).Distinct();

                sessionList.RemoveAll(x => !notEmptyTree.Contains(x));
            }

            var emptyTargets = from t in BuildSessionInfo.PacketTargets.Keys
                               where BuildSessionInfo.PacketTargets[t].Count == 0
                               select t;

            emptyTargets.ToList().ForEach(x => BuildSessionInfo.PacketTargets.Remove(x));
        }

        /// <summary>
        /// Prepares the data for save.
        /// </summary>
        /// <param name="rootContext">The root context.</param>
        /// <param name="sessionsToSave">The sessions to save.</param>
        private void prepareDataForSave(SaveProtocolParams rootContext, IEnumerable<ProtocolSession> sessionsToSave)
        {
            BuildSessionInfo.PacketTargets.Clear();
            var i = 0;
            // for each unsaved session route data
            foreach (var protocolSession in sessionsToSave.TakeWhile(x => !rootContext.Worker.CancellationPending))
            {
                rootContext.Worker.ReportPercent((double)i++ / (double)sessionsToSave.Count());
                routeProtocolSession(new SaveProtocolSessionParams(rootContext, protocolSession));
            }
        }

        /// <summary>
        /// Specify target nodes for protocol session.
        /// </summary>
        /// <param name="context">The context.</param>
        private void routeProtocolSession(SaveProtocolSessionParams context)
        {
            context.Worker.ReportProgress(string.Format(Localizer.SavingSessionData,
                                                          context.ProtocolSession.Oid));

            var protocolRecords = context.ProtocolSession.ProtocolRecords.OrderBy(x => x.ModifiedOn);

            // route each protocol record
            // (don't interrupt saving on cancel because session data should stay consistent in package)
            foreach (var record in protocolRecords)
                routeProtocolRecord(new SaveProtocolRecordParams(context, record));

            // mark current protocol session as saved
            context.ProtocolSession.SessionIsSaved = true;

            context.Worker.ReportProgress(string.Format(Localizer.SessionDataSaved,
                                                           context.ProtocolSession.Oid));
        }

        /// <summary>
        /// Specify protocol record recipients.
        /// </summary>
        /// <param name="context">The context.</param>
        private void routeProtocolRecord(SaveProtocolRecordParams context)
        {
            var recipients = getProtocolRecordRecipients(context);
            recipients.ForEach(x => BuildSessionInfo.GetInstance(x, 
                context.ProtocolSession).ProtocolRecords.Add(context.ProtocolRecord));
        }

        /// <summary>
        /// Creates the package record.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="recipient">The recipient.</param>
        private void createPackageRecord(SaveProtocolRecordParams context, ReplicationNode recipient)
        {
            // skip protocol records registered before last snapshot
            if (context.ProtocolRecord.ModifiedOn <= recipient.SnapshotDateTime) return;

            // look for recipient package in context.CreatedPackages dictionary. 
            // if not found then create new one.
            if(!context.CreatedPackages.ContainsKey(recipient))
            {
                var package = Owner.MessagingService.CreateOutputPackage(context.ObjectSpace, 
                    recipient, PackageType.Protocol);
                package.CreateLogRecord(PackageEventType.Created);

                context.CreatedPackages.Add(recipient, new PackageSaveInfo { Package = package });
                context.Worker.ReportProgress(Color.Green, Localizer.PackageCreated, package.FileName);
            }

            // create PackageSession if needed
            var saveInfo = context.CreatedPackages[recipient];

            var packageSession = PackageSession.CreateForProtocolSession(saveInfo.Package.UnitOfWork,
                context.ProtocolSession, context.CurrentNode.NodeId);

            saveInfo.CurrentSession = packageSession;

            // create package protocol record
            PackageRecord.CreateForProtocolRecord(saveInfo.CurrentSession, context.ProtocolRecord, recipient);
        }

        /// <summary>
        /// Gets the protocol record recipients.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private List<ReplicationNode> getProtocolRecordRecipients(SaveProtocolRecordParams context)
        {
            var result = new List<ReplicationNode>();
            var reference = context.ProtocolRecord.AuditedObject;
            if (reference != null && reference.Target != null)
            {
                // target object type
                var ti = context.ObjectSpace.TypesInfo.FindTypeInfo(reference.Target.GetType());
                var objType = ti.Type;

                var modelClass = XafDeltaModule.XafApp.Model.Application.BOModel.GetClass(objType);

                // prepare context list for select recipients
                var genType = typeof (RecipientsContext<>).MakeGenericType(objType);

                var sourceNodes = context.AllNodes;
                // for broadcast routing type assume the only recipient node is current (sender) node
                if (Owner.RoutingType == RoutingType.BroadcastRouting)
                    sourceNodes = new List<ReplicationNode> {context.CurrentNode};

                var allContext = (from c in sourceNodes
                                  select createRecipientContext(context, c, genType)).ToList();

                result = selectRecipients(modelClass, allContext).ToList();

                // if protocol record's target object is IReplicable then call it's GetRecipients method
                if (reference.Target is IReplicable)
                    ((IReplicable) reference.Target).GetRecipients(
                        new GetRecipientsEventArgs(context.ProtocolRecord, context.ObjectSpace, result));
            }
            else
            {
                if (Owner.RoutingType == RoutingType.BroadcastRouting)
                    result.Add(context.CurrentNode);
                else
                    result.AddRange(context.AllNodes);
            }

            // raise module's GetRecipients event
            Owner.OnGetRecipients(new GetRecipientsEventArgs(context.ProtocolRecord,
                                                                  context.ObjectSpace, result));
            // remove invalid recipients
            if (Owner.RoutingType != RoutingType.BroadcastRouting)
            {
                result.Remove(context.CurrentNode);
                var disabledNodes = from n in result where n.Disabled select n;
                result = result.Except(disabledNodes).ToList();
            }

            if (context.ProtocolSession is ExternalProtocolSession)
            {
                // prevent package 'reflection' - don't replicate it to nodes already specified in route
                var externalSession = (ExternalProtocolSession) context.ProtocolSession;
                var passedNodes = externalSession.Route.Split('\n');
                result = (from c in result where !passedNodes.Contains(c.NodeId) select c).ToList();
            }

            return result;
        }

        /// <summary>
        /// Selects the recipients.
        /// </summary>
        /// <param name="modelClass">The model class.</param>
        /// <param name="allContext">All context.</param>
        /// <returns></returns>
        private static IEnumerable<ReplicationNode> selectRecipients(IModelClass modelClass, 
            IEnumerable<RecipientsContextBase> allContext)
        {
            var allContextList = allContext.ToList();

            IEnumerable<RecipientsContextBase> result = new List<RecipientsContextBase>();

            if (XafDeltaModule.Instance.SelectorMode == SelectorMode.BlackList)
                ((List<RecipientsContextBase>)result).AddRange(allContextList);

            foreach (var selector in modelClass.AllRecipientSelectors())
            {
                var expression = selector.Expression;

                IEnumerable<RecipientsContextBase> selectResult;
                if (string.IsNullOrEmpty(expression))
                    selectResult = allContextList;
                else
                    selectResult = from c in allContextList where (bool)c.Evaluate(expression) select c;

                result = selector.SelectorType == SelectorType.Include 
                    ? result.Union(selectResult).Distinct() 
                    : result.Except(selectResult);
            }
            return (from c in result select c.RecipientNode).Distinct();
        }

        /// <summary>
        /// Creates the recipient context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="genType">Type of the gen.</param>
        /// <returns></returns>
        private RecipientsContextBase createRecipientContext(SaveProtocolRecordParams context,
            ReplicationNode recipient, Type genType)
        {
            var result = (RecipientsContextBase)context.ObjectSpace.CreateObject(genType);
            result.RecipientNode = recipient;
            result.SenderNode = ReplicationNode.GetCurrentNode(context.ObjectSpace);
            result.ProtocolRecord = context.ProtocolRecord;
            /* 11.2.7 */
            result.RoutingType =
                ((IModelReplicationNode)XafDeltaModule.XafApp.Model.GetNode("Replication")).RoutingType;
            return result;
        }

        #endregion

        #region Execution contexts

        /// <summary>
        /// Save protocol record params
        /// </summary>
        internal class SaveProtocolParams
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SaveProtocolParams"/> class.
            /// </summary>
            /// <param name="worker">The worker.</param>
            /// <param name="applicationObjectSpace">The modelApplication object space.</param>
            /// <param name="createdPackagesDict">The created packages dict.</param>
            /// <param name="allNodes">All nodes.</param>
            /// <param name="currentNode">The current node.</param>
            public SaveProtocolParams(ActionWorker worker, IObjectSpace applicationObjectSpace, 
                Dictionary<ReplicationNode, PackageSaveInfo> createdPackagesDict, 
                IEnumerable<ReplicationNode> allNodes, 
                ReplicationNode currentNode)
            {
                Worker = worker;
                ObjectSpace = applicationObjectSpace;
                CreatedPackages = createdPackagesDict;
                AllNodes = allNodes;
                CurrentNode = currentNode;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SaveProtocolRecordParams"/> class.
            /// </summary>
            /// <param name="baseParams">The base params.</param>
            protected SaveProtocolParams(SaveProtocolParams baseParams)
                : this(baseParams.Worker, baseParams.ObjectSpace, 
                baseParams.CreatedPackages, baseParams.AllNodes, 
                baseParams.CurrentNode)
            {
            }

            /// <summary>
            /// Gets the worker.
            /// </summary>
            public ActionWorker Worker { get; private set; }
            /// <summary>
            /// Gets the modelApplication object space.
            /// </summary>
            public IObjectSpace ObjectSpace { get; private set; }
            /// <summary>
            /// Gets the created packages dictionary.
            /// </summary>
            public Dictionary<ReplicationNode, PackageSaveInfo> CreatedPackages { get; private set; }
            /// <summary>
            /// Gets or sets all nodes.
            /// </summary>
            /// <value>
            /// All nodes.
            /// </value>
            public IEnumerable<ReplicationNode> AllNodes { get; private set; }
            /// <summary>
            /// Gets or sets the current node.
            /// </summary>
            /// <value>
            /// The current node.
            /// </value>
            public ReplicationNode CurrentNode { get; private set; }
        }

        /// <summary>
        /// Save protocol session params
        /// </summary>
        internal class SaveProtocolSessionParams : SaveProtocolParams
        {
            /// <summary>
            /// Gets or sets the protocol session.
            /// </summary>
            /// <value>
            /// The protocol session.
            /// </value>
            public ProtocolSession ProtocolSession { get; private set; }
            /// <summary>
            /// Initializes a new instance of the <see cref="SaveProtocolSessionParams"/> class.
            /// </summary>
            /// <param name="baseParams">The base params.</param>
            /// <param name="protocolSession">The protocol session.</param>
            public SaveProtocolSessionParams(SaveProtocolParams baseParams, ProtocolSession protocolSession) : base(baseParams)
            {
                ProtocolSession = protocolSession;
            }
        }

        /// <summary>
        /// Save protocol session params
        /// </summary>
        internal class SaveProtocolRecordParams : SaveProtocolSessionParams
        {
            /// <summary>
            /// Gets the protocol record.
            /// </summary>
            public ProtocolRecord ProtocolRecord { get; private set; }
            /// <summary>
            /// Initializes a new instance of the <see cref="SaveProtocolRecordParams"/> class.
            /// </summary>
            /// <param name="baseParams">The base params.</param>
            /// <param name="protocolRecord">The protocol record.</param>
            public SaveProtocolRecordParams(SaveProtocolSessionParams baseParams, ProtocolRecord protocolRecord): 
                base(baseParams, baseParams.ProtocolSession)
            {
                ProtocolRecord = protocolRecord;
            }
        }

        /// <summary>
        /// Package save state
        /// </summary>
        internal class PackageSaveInfo
        {
            /// <summary>
            /// Gets or sets the Package.
            /// </summary>
            /// <value>
            /// The Package.
            /// </value>
            public Package Package { get; set; }
            /// <summary>
            /// Gets or sets the current session.
            /// </summary>
            /// <value>
            /// The current session.
            /// </value>
            public PackageSession CurrentSession { get; set; }
        }

        /// <summary>
        /// Load package session params
        /// </summary>
        internal class LoadPackageSessionContext : LoadPackageContext
        {
            public LoadPackageSessionContext(LoadPackageContext context, PackageSession packageSession) 
                : base(context.Package, context.Worker, context.ObjectSpace, context.CurrentNodeId)
            {
                PackageSession = packageSession;
            }

            /// <summary>
            /// Gets or sets the package session.
            /// </summary>
            /// <value>
            /// The package session.
            /// </value>
            public PackageSession PackageSession { get; private set; }
        }

        /// <summary>
        /// Load package record params
        /// </summary>
        internal class LoadPackageRecordContext : LoadPackageSessionContext
        {

            /// <summary>
            /// Initializes a new instance of the <see cref="LoadPackageRecordContext"/> class.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="packageRecord">The package record.</param>
            /// <param name="activeObjectSpaces">The active object spaces.</param>
            public LoadPackageRecordContext(LoadPackageSessionContext context, 
                PackageRecord packageRecord, Dictionary<Guid, IObjectSpace> activeObjectSpaces)
                : base(context, context.PackageSession)
            {
                PackageRecord = packageRecord;
                ActiveObjectSpaces = activeObjectSpaces;
            }

            /// <summary>
            /// Gets or sets the package protocol record.
            /// </summary>
            /// <value>
            /// The package protocol record.
            /// </value>
            public PackageRecord PackageRecord { get; private set; }
            public Dictionary<Guid, IObjectSpace> ActiveObjectSpaces { get; private set; }
        }

        #endregion
    }

    /// <summary>
    /// Collision type
    /// </summary>
    public enum CollisionType
    {
        /// <summary>
        /// Target object already exists on creating
        /// </summary>
        TargetObjectAlreadyExists,
        /// <summary>
        /// Old object is not found in application database on reference property change
        /// </summary>
        OldObjectIsNotFound,
        /// <summary>
        /// New object is not found in application database on reference property change (initialization)
        /// </summary>
        NewObjectIsNotFound,
        /// <summary>
        /// Target object is not found in application database on change or delete operation
        /// </summary>
        TargetObjectIsNotFound,
        /// <summary>
        /// Specified member is not found in target object on change or initialization operation
        /// </summary>
        MemberIsNotFound,
        /// <summary>
        /// Trying to change or initialize read only member of target object
        /// </summary>
        ChangeReadOnlyMember,
        /// <summary>
        /// The old value of property in application database is not equals one specified in replication protocol record
        /// </summary>
        InvalidOldValue
    }

    /// <summary>
    /// Collision result
    /// </summary>
    public enum CollisionResult
    {
        /// <summary>
        /// Default collision resolving (resolving by XafDelta)
        /// </summary>
        Default,
        /// <summary>
        /// Skip protocol record processing
        /// </summary>
        Skip,
        /// <summary>
        /// Throw resolving error
        /// </summary>
        Error
    }

    #region EventArgs

    /// <summary>
    /// Arument for <see cref="XafDeltaModule.AfterBuildPackages"/> event.
    /// </summary>
    public class AfterBuildPackagesArgs : EventArgs
    {
        /// <summary>
        /// Gets the packages created while build process.
        /// </summary>
        public IEnumerable<Package> Packages { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AfterBuildPackagesArgs"/> class.
        /// </summary>
        /// <param name="packages">The packages.</param>
        public AfterBuildPackagesArgs(IEnumerable<Package> packages)
        {
            Packages = packages;
        }
    }

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.BeforeBuildPackages"/> event.
    /// </summary>
    public class BeforeBuildPackagesArgs : EventArgs
    {
        /// <summary>
        /// Gets the application object space.
        /// </summary>
        public IObjectSpace ApplicationObjectSpace { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeBuildPackagesArgs"/> class.
        /// </summary>
        /// <param name="applicationObjectSpace">The application object space.</param>
        public BeforeBuildPackagesArgs(IObjectSpace applicationObjectSpace)
        {
            ApplicationObjectSpace = applicationObjectSpace;
        }
    }

    /// <summary>
    /// Arguments for <see cref="XafDeltaModule.GetRecipients"/> event.
    /// </summary>
    public class GetRecipientsEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the protocol record.
        /// </summary>
        public ProtocolRecord ProtocolRecord { get; private set; }

        /// <summary>
        /// Gets the application object space.
        /// </summary>
        public IObjectSpace ObjectSpace { get; private set; }

        /// <summary>
        /// Gets the recipients list for specified <seealso cref="ProtocolRecord"/>.
        /// </summary>
        public List<ReplicationNode> Recipients { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientsEventArgs"/> class.
        /// </summary>
        /// <param name="protocolRecord">The protocol record.</param>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="recipients">The recipients.</param>
        public GetRecipientsEventArgs(ProtocolRecord protocolRecord, IObjectSpace objectSpace,
                                      List<ReplicationNode> recipients)
        {
            ProtocolRecord = protocolRecord;
            ObjectSpace = objectSpace;
            Recipients = recipients;
        }
    }

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.BeforeLoadSession"/> and <see cref="XafDeltaModule.AfterLoadSession"/> events.
    /// </summary>
    public class LoadSessionArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadSessionArgs"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="records">The records.</param>
        internal LoadSessionArgs(ProtocolReplicationService.LoadPackageSessionContext context, 
            IEnumerable<PackageRecord> records)
        {
            Records = records;
            ObjectSpace = context.ObjectSpace;
            Package = context.Package;
            PackageSession = context.PackageSession;
        }

        /// <summary>
        /// Gets or sets the application object space.
        /// </summary>
        /// <value>
        /// The object space.
        /// </value>
        public IObjectSpace ObjectSpace { get; private set; }

        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        /// <value>
        /// The package.
        /// </value>
        public Package Package { get; private set; }

        /// <summary>
        /// Gets or sets the package session.
        /// </summary>
        /// <value>
        /// The package session.
        /// </value>
        public PackageSession PackageSession { get; private set; }

        /// <summary>
        /// Gets the package session records.
        /// </summary>
        public IEnumerable<PackageRecord> Records { get; private set; }

         /// <summary>
        /// Gets or sets a value indicating whether session loading is completed in user event handler. 
        /// Set it to <c>true</c> in <see cref="XafDeltaModule.BeforeLoadSession"/> to disable standard session loading.
        /// </summary>
        /// <value>
        ///   <c>true</c> if done; otherwise, <c>false</c>.
        /// </value>
        public bool Done { get; set; }
    }

    /// <summary>
    /// Arguments for <see cref="XafDeltaModule.BeforeLoadRecord"/> and <see cref="XafDeltaModule.AfterLoadRecord"/> events.
    /// </summary>
    public class LoadRecordArgs : EventArgs
    {
        internal LoadRecordArgs(ProtocolReplicationService.LoadPackageRecordContext context)
        {
            PackageRecord = context.PackageRecord;
        }

        /// <summary>
        /// Gets the package record.
        /// </summary>
        public PackageRecord PackageRecord { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether record loading is completed in user event handler.
        /// </summary>
        /// <value>
        ///   <c>true</c> if done; otherwise, <c>false</c>.
        /// </value>
        public bool Done { get; set; }
    }

    /// <summary>
    /// Arguments for <see cref="XafDeltaModule.ResolveReplicationCollision"/> event. 
    /// Use <see cref="ResolveResult"/> property to specify resolve result. 
    /// If you are resolve collisions in handler code then set <see cref="ResolveResult"/> 
    /// to <see cref="CollisionResult.Skip"/>.
    /// </summary>
    public class ResolveReplicationCollisionArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResolveReplicationCollisionArgs"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="collisionType">Type of the collision.</param>
        /// <param name="args">The args.</param>
        internal ResolveReplicationCollisionArgs(ProtocolReplicationService.LoadPackageRecordContext context,
            CollisionType collisionType,
            object[] args)
        {
            CollisionType = collisionType;
            Args = args;
            ResolveResult = CollisionResult.Default;
            ObjectSpace = context.ObjectSpace;
            PackageSession = context.PackageSession;
            Package = context.Package;
            CurrentNodeId = context.CurrentNodeId;
            PackageRecord = context.PackageRecord;
        }
        /// <summary>
        /// Gets the type of the collision detected while <see cref="PackageRecord"/> loading.
        /// </summary>
        /// <value>
        /// The type of the collision.
        /// </value>
        public CollisionType CollisionType { get; private set; }

        /// <summary>
        /// Gets the collision arguments.
        /// </summary>
        public object[] Args { get; private set; }

        /// <summary>
        /// Gets the resolve result. Default value is <see cref="CollisionResult.Default"/>
        /// </summary>
        public CollisionResult ResolveResult { get; private set; }

        /// <summary>
        /// Gets the package protocol record.
        /// </summary>
        public PackageRecord PackageRecord { get; private set; }

        /// <summary>
        /// Gets the current node id.
        /// </summary>
        public string CurrentNodeId { get; private set; }

        /// <summary>
        /// Gets the package.
        /// </summary>
        public Package Package { get; private set; }

        /// <summary>
        /// Gets the package session.
        /// </summary>
        public PackageSession PackageSession { get; private set; }

        /// <summary>
        /// Gets the object application space.
        /// </summary>
        public IObjectSpace ObjectSpace { get; private set; }
    }

    /// <summary>
    /// Arguments for <see cref="XafDeltaModule.GetMemberIsNotReplicable"/> event
    /// </summary>
    public class GetMemberIsNotReplicableArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the property info.
        /// </summary>
        /// <value>
        /// The property info.
        /// </value>
        public PropertyInfo PropertyInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether specified member is not replicable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if member is not replicable; otherwise, <c>false</c>.
        /// </value>
        public bool MemberIsNotReplicable { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMemberIsNotReplicableArgs"/> class.
        /// </summary>
        /// <param name="propertyInfo">The property info.</param>
        public GetMemberIsNotReplicableArgs(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }
    }

    #endregion
}

