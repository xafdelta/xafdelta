using System;
using System.Linq;
using DevExpress.Persistent.AuditTrail;
using DevExpress.Xpo;
using DevExpress.Persistent.Validation;
using XafDelta.Protocol;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Package record. Protocol record data container for package.
    /// For internal use only.
    /// </summary>
    [IsLocal]
    public sealed class PackageRecord : XPObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageRecord"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public PackageRecord(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return OperationType + " DateTime=" + ModifiedOn.ToLocalTime()
                    + (AuditedObject == null ? "" : " AuditedObject=(" + AuditedObject + ")")
                    + (string.IsNullOrEmpty(PropertyName) ? "" : "PropertyName=(" + PropertyName + ")")
                    + (NewObject == null ? "" : " NewObject=(" + NewObject + ")")
                    + (OldObject == null ? "" : " OldObject=(" + OldObject + ")")
                    + (NewObject != null ? "" : " NewValue=(" + NewValue + ")")
                    + (OldObject != null ? "" : " OldValue=(" + OldValue + ")");
        }

        /// <summary>
        /// Creates package record based on protocol record.
        /// </summary>
        /// <param name="packageSession">The package session.</param>
        /// <param name="protocolRecord">The protocol record.</param>
        /// <param name="targetNode">The target node.</param>
        /// <returns>Package record</returns>
        public static PackageRecord CreateForProtocolRecord(PackageSession packageSession, 
            ProtocolRecord protocolRecord, ReplicationNode targetNode)
        {
            var destinationSession = packageSession.Session;
            var result = new PackageRecord(destinationSession)
            {
                PackageSession = packageSession,
                UserName = protocolRecord.UserName,
                Description = protocolRecord.Description,
                ModifiedOn = protocolRecord.ModifiedOn,
                NewBlobValue = protocolRecord.NewBlobValue,
                PropertyName = protocolRecord.PropertyName,
                NewValue = protocolRecord.NewValue,
                OldValue = protocolRecord.OldValue,
                OperationType = protocolRecord.OperationType
            };

            if(protocolRecord.AuditedObject != null && protocolRecord.AuditedObject.Target != null)
            {
                result.AuditedObject = 
                    PackageObjectReference.CreatePackageObjectReference(protocolRecord.AuditedObject.Target,
                        destinationSession, targetNode);

                result.AuditedObject.ReplicationKey = protocolRecord.ReplicationKey;
            }


            if (protocolRecord.NewObject != null)
                result.NewObject = PackageObjectReference.CreatePackageObjectReference(protocolRecord.NewObject.Target,
                    destinationSession, targetNode);

            if (protocolRecord.OldObject != null)
                result.OldObject = PackageObjectReference.CreatePackageObjectReference(protocolRecord.OldObject.Target,
                    destinationSession, targetNode);

            return result;
        }

        /// <summary>
        /// Gets or sets the package session.
        /// </summary>
        /// <value>
        /// The package session.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        [Association("PackageSessionRecords")]
        public PackageSession PackageSession
        {
            get { return GetPropertyValue<PackageSession>("PackageSession"); }
            set { SetPropertyValue("PackageSession", value); }
        }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        [Indexed]
        public string UserName
        {
            get { return GetPropertyValue<string>("UserName"); }
            set { SetPropertyValue("UserName", value); }
        }

        /// <summary>
        /// Gets or sets the modified on.
        /// </summary>
        /// <value>
        /// The modified on.
        /// </value>
        [Indexed]
        [Custom("DisplayFormat", "{0:o}")]
        public DateTime ModifiedOn
        {
            get { return GetPropertyValue<DateTime>("ModifiedOn"); }
            set { SetPropertyValue("ModifiedOn", value); }
        }

        /// <summary>
        /// Gets or sets the type of the operation.
        /// </summary>
        /// <value>
        /// The type of the operation.
        /// </value>
        [Indexed]
        public string OperationType
        {
            get { return GetPropertyValue<string>("OperationType"); }
            set { SetPropertyValue("OperationType", value); }
        }

        /// <summary>
        /// Gets the type of the audit operation.
        /// </summary>
        /// <value>
        /// The type of the audit operation.
        /// </value>
        public AuditOperationType AuditOperationType
        {
            get
            {
                var result = AuditOperationType.CustomData;
                var type = typeof (AuditOperationType);
                if (Enum.GetNames(type).Contains(OperationType))
                    result = (AuditOperationType) Enum.Parse(type, OperationType);
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        [MemberDesignTimeVisibility(true)]
        [Size(2048)]
        [Delayed]
        public string Description
        {
            get { return GetPropertyValue<string>("Description"); }
            set { SetPropertyValue("Description", value); }
        }

        /// <summary>
        /// Gets or sets the audited object.
        /// </summary>
        /// <value>
        /// The audited object.
        /// </value>
        [MemberDesignTimeVisibility(false)]
        [Aggregated]
        public PackageObjectReference AuditedObject
        {
            get { return GetPropertyValue<PackageObjectReference>("AuditedObject"); }
            set { SetPropertyValue("AuditedObject", value); }
        }

        /// <summary>
        /// Gets or sets the old object.
        /// </summary>
        /// <value>
        /// The old object.
        /// </value>
        [Aggregated]
        [MemberDesignTimeVisibility(false)]
        public PackageObjectReference OldObject
        {
            get { return GetPropertyValue<PackageObjectReference>("OldObject"); }
            set { SetPropertyValue("OldObject", value); }
        }

        /// <summary>
        /// Gets or sets the new object.
        /// </summary>
        /// <value>
        /// The new object.
        /// </value>
        [MemberDesignTimeVisibility(false)]
        [Aggregated]
        public PackageObjectReference NewObject
        {
            get { return GetPropertyValue<PackageObjectReference>("NewObject"); }
            set { SetPropertyValue("NewObject", value); }
        }

        /// <summary>
        /// Gets or sets the old value.
        /// </summary>
        /// <value>
        /// The old value.
        /// </value>
        [Size(1024)]
        [Delayed]
        public string OldValue
        {
            get { return GetPropertyValue<string>("OldValue"); }
            set { SetPropertyValue("OldValue", value); }
        }

        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        /// <value>
        /// The new value.
        /// </value>
        [Delayed]
        [Size(1024)]
        public string NewValue
        {
            get { return GetPropertyValue<string>("NewValue"); }
            set { SetPropertyValue("NewValue", value); }
        }


        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>
        /// The name of the property.
        /// </value>
        public string PropertyName
        {
            get { return GetPropertyValue<string>("PropertyName"); }
            set { SetPropertyValue("PropertyName", value); }
        }

        /// <summary>
        /// Gets or sets the new BLOB value.
        /// </summary>
        /// <value>
        /// The new BLOB value.
        /// </value>
        [Delayed]
        public byte[] NewBlobValue
        {
            get { return GetPropertyValue<byte[]>("NewBlobValue"); }
            set { SetPropertyValue("NewBlobValue", value); }
        }
    }

}
