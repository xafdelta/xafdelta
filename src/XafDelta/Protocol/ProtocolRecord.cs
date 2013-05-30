using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using XafDelta.Messaging;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Protocol record
    /// </summary>
    [IsLocal]
    public class ProtocolRecord : AuditDataItemPersistent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolRecord"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public ProtocolRecord(Session session) : base (session)
        {
        }

        /// <summary>
        /// Gets or sets the new BLOB value.
        /// </summary>
        /// <value>
        /// The new BLOB value.
        /// </value>
        public byte[] NewBlobValue
        {
            get { return GetPropertyValue<byte[]>("NewBlobValue"); }
            set { SetPropertyValue("NewBlobValue", value); }
        }

        /// <summary>
        /// Gets or sets the replication id.
        /// </summary>
        /// <value>
        /// The replication id.
        /// </value>
        [Size(255)]
        public string ReplicationKey
        {
            get { return GetPropertyValue<string>("ReplicationKey"); }
            set { SetPropertyValue("ReplicationKey", value); }
        }

        /// <summary>
        /// Gets or sets the protocol session.
        /// </summary>
        /// <value>
        /// The protocol session.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        [Association("ProtocolRecords")]
        public ProtocolSession ProtocolSession
        {
            get { return GetPropertyValue<ProtocolSession>("ProtocolSession"); }
            set { SetPropertyValue("ProtocolSession", value); }
        }

       

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object TargetObject { get { return AuditedObject == null ? null : AuditedObject.Target; } }

        /// <summary>
        /// Creates for package record.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="record">The record.</param>
        /// <param name="nodeId">The node id.</param>
        /// <returns></returns>
        public static ProtocolRecord CreateForPackageRecord(IObjectSpace objectSpace,
            PackageRecord record, string nodeId)
        {
            if (objectSpace == null) throw new ArgumentNullException("objectSpace");
            if (record == null) throw new ArgumentNullException("record");


            var session = ((ObjectSpace)objectSpace).Session;
            var result = objectSpace.CreateObject<ProtocolRecord>();

            if (record.AuditedObject != null && record.AuditedObject.IsAssigned)
                result.AuditedObject = new AuditedObjectWeakReference(session,
                    OidMap.FindApplicationObject(objectSpace, record.AuditedObject, nodeId));

            result.OperationType = record.OperationType;
            result.ModifiedOn = record.ModifiedOn;
            result.Description = record.Description;
            result.NewBlobValue = record.NewBlobValue;

            if (record.NewObject != null)
                result.NewObject = new XPWeakReference(session,
                    OidMap.FindApplicationObject(objectSpace, record.NewObject, nodeId));

            if (record.OldObject != null)
                result.OldObject = new XPWeakReference(session,
                    OidMap.FindApplicationObject(objectSpace, record.OldObject, nodeId));

            result.NewValue = record.NewValue;
            result.OldValue = record.OldValue;
            result.PropertyName = record.PropertyName;

            if (record.AuditedObject != null && record.AuditedObject.IsAssigned)
                result.ReplicationKey = record.AuditedObject.ReplicationKey;

            return result;
        }
    }
}
