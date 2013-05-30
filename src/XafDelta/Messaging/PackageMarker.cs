using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Package marker. Holds package specification.
    /// For internal use only.
    /// </summary>
    [IsLocal]
    public sealed class PackageMarker : XPObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageMarker"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public PackageMarker(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Creates marker for package.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="package">The package.</param>
        /// <returns>Package marker</returns>
        public static PackageMarker CreateForPackage(Session session, Package package)
        {
            var result = new PackageMarker(session)
                             {
                                 ApplicationName = package.ApplicationName,
                                 SenderNodeId = package.SenderNodeId,
                                 RecipientNodeId = package.RecipientNodeId,
                                 PackageId = package.PackageId,
                                 PackageType = package.PackageType,
                                 PackageDateTime = package.PackageDateTime.ToUniversalTime(),
                                 UserName = package.UserName
                             };

            if(package.PackageType == PackageType.Snapshot)
            {
                var recipient = ReplicationNode.FindNode(package.Session, result.RecipientNodeId);
                var sender = ReplicationNode.GetCurrentNode(package.Session);
                var num = XafDeltaModule.Instance.RoutingType == RoutingType.BroadcastRouting 
                              ? sender.LastSavedPackageNumber 
                              : recipient.LastSavedPackageNumber;
                if (recipient != null)
                    result.LastSavedPackageNumber = num;
            }
            return result;
        }

        /// <summary>
        /// Gets or sets the application id.
        /// </summary>
        /// <value>
        /// The application id.
        /// </value>
        public string ApplicationName
        {
            get { return GetPropertyValue<string>("ApplicationName"); }
            set { SetPropertyValue("ApplicationName", value); }
        }

        /// <summary>
        /// Gets or sets the sender node id.
        /// </summary>
        /// <value>
        /// The sender node id.
        /// </value>
        [Size(255)]
        [RuleRequiredField("", DefaultContexts.Save)]
        public string SenderNodeId
        {
            get { return GetPropertyValue<string>("SenderNodeId"); }
            set { SetPropertyValue("SenderNodeId", value); }
        }

        /// <summary>
        /// Gets or sets the recipient node id.
        /// </summary>
        /// <value>
        /// The recipient node id.
        /// </value>
        [Size(255)]
        [RuleRequiredField("", DefaultContexts.Save)]
        public string RecipientNodeId
        {
            get { return GetPropertyValue<string>("RecipientNodeId"); }
            set { SetPropertyValue("RecipientNodeId", value); }
        }

        /// <summary>
        /// Gets or sets the package id.
        /// </summary>
        /// <value>
        /// The package id.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        public int PackageId
        {
            get { return GetPropertyValue<int>("PackageId"); }
            set { SetPropertyValue("PackageId", value); }
        }

        /// <summary>
        /// Gets or sets the type of the package.
        /// </summary>
        /// <value>
        /// The type of the package.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        public PackageType PackageType
        {
            get { return GetPropertyValue<PackageType>("PackageType"); }
            set { SetPropertyValue("PackageType", value); }
        }

        /// <summary>
        /// Gets or sets the package date time.
        /// </summary>
        /// <value>
        /// The package date time.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        public DateTime PackageDateTime
        {
            get { return GetPropertyValue<DateTime>("PackageDateTime"); }
            set { SetPropertyValue("PackageDateTime", value); }
        }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName
        {
            get { return GetPropertyValue<string>("UserName"); }
            set { SetPropertyValue("UserName", value); }
        }

        /// <summary>
        /// Gets or sets the last saved package number.
        /// </summary>
        /// <value>
        /// The last saved package number.
        /// </value>
        public int LastSavedPackageNumber
        {
            get { return GetPropertyValue<int>("LastSavedPackageNumber"); }
            set { SetPropertyValue("LastSavedPackageNumber", value); }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>Package marker singleton for specified object space</returns>
        public static PackageMarker GetInstance(IObjectSpace objectSpace)
        {
            return objectSpace.FindObject<PackageMarker>(null, true);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="package"></param>
        /// <returns>Package marker singleton for specified object space</returns>
        public static PackageMarker GetInstance(Session session, Package package)
        {
            var result = session.FindObject<PackageMarker>(null, true) ?? CreateForPackage(session, package);
            return result;
        }
    }

}
