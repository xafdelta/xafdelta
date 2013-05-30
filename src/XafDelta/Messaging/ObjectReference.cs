using System;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo.Metadata;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Object relication reference. 
    /// For internal use only.
    /// </summary>
    [IsLocal]
    public class ObjectReference : XPObject, IObjectReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectReference"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public ObjectReference(Session session)
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
            return (ClassName == null ? "" : ClassName + " ") 
                + (string.IsNullOrEmpty(ObjectId) ? "" : "Key=" + ObjectId);
        }

        /// <summary>
        /// Gets or sets the name of the assembly.
        /// </summary>
        /// <value>
        /// The name of the assembly.
        /// </value>
        [Size(100)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        public string AssemblyName
        {
            get { return GetPropertyValue<string>("AssemblyName"); }
            set { SetPropertyValue("AssemblyName", value); }
        }

        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        /// <value>
        /// The name of the class.
        /// </value>
        [Size(100)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        public string ClassName
        {
            get { return GetPropertyValue<string>("ClassName"); }
            set { SetPropertyValue("ClassName", value); }
        }

        /// <summary>
        /// Gets or sets the assembly qualified name.
        /// </summary>
        /// <value>
        /// The assembly qualified name.
        /// </value>
        [Size(255)]
        public string AssemblyQualifiedName
        {
            get { return GetPropertyValue<string>("AssemblyQualifiedName"); }
            set { SetPropertyValue("AssemblyQualifiedName", value); }
        }

        /// <summary>
        /// Gets or sets the object id.
        /// </summary>
        /// <value>
        /// The object id.
        /// </value>
        [Size(50)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        public string ObjectId
        {
            get { return GetPropertyValue<string>("ObjectId"); }
            set { SetPropertyValue("ObjectId", value); }
        }

        /// <summary>
        /// Gets or sets the replication key.
        /// </summary>
        /// <value>
        /// The replication key.
        /// </value>
        [Size(255)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        public string ReplicationKey
        {
            get { return GetPropertyValue<string>("ReplicationKey"); }
            set { SetPropertyValue("ReplicationKey", value); }
        }

        /// <summary>
        /// Gets the class info in context of current session.
        /// </summary>
        /// <returns>Class info</returns>
        public XPClassInfo GetTargetClassInfo()
        {
            return Session.GetClassInfo(AssemblyName, ClassName);
        }

        /// <summary>
        /// Gets the class info.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>Class info</returns>
        public XPClassInfo GetTargetClassInfo(Session session)
        {
            return session.GetClassInfo(AssemblyName, ClassName);
        }

        /// <summary>
        /// Gets the class info.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>Class info</returns>
        public XPClassInfo GetTargetClassInfo(IObjectSpace objectSpace)
        {
            return GetTargetClassInfo(((ObjectSpace)objectSpace).Session);
        }

        /// <summary>
        /// Gets the type of the reference.
        /// </summary>
        /// <value>
        /// The type of the reference.
        /// </value>
        public Type ReferenceType { get { return Type.GetType(AssemblyQualifiedName); } }

        /// <summary>
        /// Gets a value indicating whether this instance is assigned.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is assigned; otherwise, <c>false</c>.
        /// </value>
        public bool IsAssigned { get { return ReferenceType != null && !string.IsNullOrEmpty(ObjectId); } }

        /// <summary>
        /// Store object data in reference.
        /// </summary>
        /// <param name="source">The source.</param>
        public virtual void Assign(object source)
        {
            if (source == null)
            {
                AssemblyName = "";
                ClassName = "";
                ObjectId = "";
                AssemblyQualifiedName = "";
                ReplicationKey = "";
            }
            else
            {
                var modelClass = XafDeltaModule.XafApp.FindModelClass(source.GetType());
                AssemblyName =  modelClass.TypeInfo.AssemblyInfo.Assembly.GetName().Name;
                ClassName = modelClass.TypeInfo.FullName;
                ObjectId = XPWeakReference.KeyToString(modelClass.TypeInfo.KeyMember.GetValue(source));
                AssemblyQualifiedName = modelClass.TypeInfo.Type.AssemblyQualifiedName;
                ReplicationKey = ExtensionsHelper.GetReplicationKey(source);
            }
        }

        /// <summary>
        /// Creates the object reference for source object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destinationSession">The destination session.</param>
        /// <returns>Object reference</returns>
        public static ObjectReference CreateObjectReference(object source, Session destinationSession)
        {
            ObjectReference result = null;
            if (source != null && source is IXPObject)
            {
                result = new ObjectReference(destinationSession);
                result.Assign(source);
            }
            return result;
        }

        
    }
}
