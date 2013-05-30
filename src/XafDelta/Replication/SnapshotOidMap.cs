using System;
using System.Text;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using XafDelta.Messaging;

namespace XafDelta.Replication
{
    /// <summary>
    /// Snapshot object map.
    /// For internal use only.
    /// </summary>
    [IsLocal]
    public class SnapshotOidMap : BaseObject, IPackageObjectReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotOidMap"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public SnapshotOidMap(Session session) : base(session)
        {
        }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        public XPWeakReference Target
        {
            get { return GetPropertyValue<XPWeakReference>("Target"); }
            set { SetPropertyValue("Target", value); }
        }

        /// <summary>
        /// Gets or sets the name of the stored class.
        /// </summary>
        /// <value>
        /// The name of the stored class.
        /// </value>
        [Size(255)]
        public string StoredClassName
        {
            get { return GetPropertyValue<string>("StoredClassName"); }
            set { SetPropertyValue("StoredClassName", value); }
        }

        /// <summary>
        /// Gets the known mapping in format "NodeId/aObjectId/n".
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string KnownMapping
        {
            get { return GetPropertyValue<string>("KnownMapping"); }
            set { SetPropertyValue("KnownMapping", value); }
        }

        /// <summary>
        /// Gets or sets map's ordinal number in snapshot.
        /// </summary>
        /// <value>
        /// The map's ordinal number.
        /// </value>
        public int OrdNo
        {
            get { return GetPropertyValue<int>("OrdNo"); }
            set { SetPropertyValue("OrdNo", value); }
        }

        /// <summary>
        /// Creates the SnapshotOidMap for source object.
        /// </summary>
        /// <param name="sourceObject">The source object.</param>
        /// <param name="targetObject">The target object.</param>
        /// <returns></returns>
        public static SnapshotOidMap Create(object sourceObject, object targetObject)
        {
            var targetSession = ((ISessionProvider) targetObject).Session;
            var sourceSession = ((ISessionProvider) sourceObject).Session;

            /* 11.2.7 */
            var modelClass = XafDeltaModule.XafApp.FindModelClass(targetObject.GetType());

            var result = new SnapshotOidMap(targetSession)
                             {
                                 Target = new XPWeakReference(targetSession, targetObject),
                                 // 11.2.7 StoredClassName = targetObject.GetType().FullName
                                 StoredClassName = modelClass.TypeInfo.FullName
                             };

            var sb = new StringBuilder();
            sb.AppendFormat("{0}\a{1}\n", XafDeltaModule.Instance.CurrentNodeId, 
                XPWeakReference.KeyToString(sourceSession.GetKeyValue(sourceObject)));

            var maps = OidMap.GetOidMaps((IXPObject)sourceObject);
            foreach (var oidMap in maps)
                sb.AppendFormat("{0}\a{1}\n", oidMap.NodeId, oidMap.ObjectId);
            result.KnownMapping = sb.ToString();

            return result;
        }

        #region Implementation of IObjectReference

        /// <summary>
        /// Gets or sets the name of the assembly.
        /// </summary>
        /// <value>
        /// The name of the assembly.
        /// </value>
        public string AssemblyName
        {
            get { return Target.Session.GetClassInfo(Target.Target).AssemblyName; }
        }

        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        /// <value>
        /// The name of the class.
        /// </value>
        public string ClassName
        {
            get { return Target.Session.GetClassInfo(Target.Target).FullName; }
        }

        /// <summary>
        /// Gets or sets the assembly qualified name.
        /// </summary>
        /// <value>
        /// The assembly qualified name.
        /// </value>
        public string AssemblyQualifiedName
        {
            get { return Target.Session.GetClassInfo(Target.Target).ClassType.AssemblyQualifiedName; }
        }

        /// <summary>
        /// Gets or sets the object id.
        /// </summary>
        /// <value>
        /// The object id.
        /// </value>
        public string ObjectId
        {
            get
            {
                var result = "";
                if(!string.IsNullOrEmpty(KnownMapping))
                {
                    var mapLines = KnownMapping.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                    if(mapLines.Length > 0)
                        result = mapLines[0].Split('\a')[1];
                }
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the replication key.
        /// </summary>
        /// <value>
        /// The replication key.
        /// </value>
        public string ReplicationKey
        {
            get { return ExtensionsHelper.GetReplicationKey(Target.Target); }
        }

        #endregion
    }

}
