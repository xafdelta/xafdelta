using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Messaging service. Support service for replication messaging items.
    /// For internal use only.
    /// </summary>
    internal sealed class MessagingService : BaseService
    {
        public MessagingService(XafDeltaModule owner) : base(owner)
        {
        }

        /// <summary>
        /// Initializes the service.
        /// </summary>
        /// <param name="application">The application.</param>
        public void Initialize(XafApplication application)
        {
            application.ObjectSpaceCreated += application_ObjectSpaceCreated;
            application.Disposed += application_Disposed;
        }

        /// <summary>
        /// Handles the Disposed event of the application control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        void application_Disposed(object sender, EventArgs e)
        {
            var application = (XafApplication) sender;
            application.ObjectSpaceCreated += application_ObjectSpaceCreated;
            application.Disposed += application_Disposed;
        }

        /// <summary>
        /// Handles the ObjectSpaceCreated event of the application control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.ExpressApp.ObjectSpaceCreatedEventArgs"/> instance containing the event data.</param>
        void application_ObjectSpaceCreated(object sender, ObjectSpaceCreatedEventArgs e)
        {
            // subscribe to RollingBack event of object space
            e.ObjectSpace.RollingBack += objectSpace_RollingBack;
            e.ObjectSpace.Disposed += objectSpace_Disposed;
        }

        /// <summary>
        /// Handles the Disposed event of the objectSpace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void objectSpace_Disposed(object sender, EventArgs e)
        {
            var objectSpace = (IObjectSpace)sender;
            objectSpace.RollingBack -= objectSpace_RollingBack;
            objectSpace.Disposed -= objectSpace_Disposed;
        }

        /// <summary>
        /// Handles the RollingBack event of the objectSpace control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private static void objectSpace_RollingBack(object sender, CancelEventArgs e)
        {
            // rollback all active units of work for modified packages. destroy temporary files.
            var objectSpace = (IObjectSpace)sender;
            (from p in objectSpace.ModifiedObjects.Cast<object>() where p is Package select (Package)p).
                ToList().ForEach(x => x.CloseUnitOfWork(false));
        }

        /// <summary>
        /// Creates the output package.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="packageType">Type of the package.</param>
        /// <returns>Output package</returns>
        public Package CreateOutputPackage(IObjectSpace objectSpace, ReplicationNode recipient, PackageType packageType)
        {
            var result = objectSpace.CreateObject<Package>();

            result.ApplicationName = XafDeltaModule.XafApp.ApplicationName;
            result.SenderNodeId = Owner.CurrentNodeId;
            result.RecipientNodeId = recipient.NodeId;
            result.PackageType = packageType;

            // for broadcast package recipient node Id is "AllNodes"
            if (result.SenderNodeId == result.RecipientNodeId)
                result.RecipientNodeId = ReplicationNode.AllNodes;

            // assign package id
            if (packageType == PackageType.Snapshot)
            {
                recipient.SnapshotDateTime = DateTime.UtcNow;
                recipient.LastSavedSnapshotNumber++;
                result.PackageId = recipient.LastSavedSnapshotNumber;
            }
            else
            {
                recipient.LastSavedPackageNumber++;
                result.PackageId = recipient.LastSavedPackageNumber;
            }

            return result;
        }
    }
}