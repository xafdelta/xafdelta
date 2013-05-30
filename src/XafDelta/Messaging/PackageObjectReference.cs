using System.Text;
using DevExpress.Xpo;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Package object reference
    /// For internal use only.
    /// </summary>
    [MapInheritance(MapInheritanceType.ParentTable)]
    [IsLocal]
    public class PackageObjectReference : ObjectReference, IPackageObjectReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageObjectReference"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public PackageObjectReference(Session session) : base(session)
        {
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
        /// Creates the package object reference.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="destinationSession">The destination session.</param>
        /// <param name="targetNode">The target replication node</param>
        /// <returns>Package object reference</returns>
        public static PackageObjectReference CreatePackageObjectReference(object source, 
            Session destinationSession, ReplicationNode targetNode)
        {
            PackageObjectReference result = null;
            if (source != null && source is IXPObject)
            {
                result = new PackageObjectReference(destinationSession);
                result.Assign(source);
                var maps = OidMap.GetOidMaps((IXPObject)source);
                var sb = new StringBuilder();
                foreach (var oidMap in maps)
                    sb.AppendFormat("{0}\a{1}\n", oidMap.NodeId, oidMap.ObjectId);
                result.KnownMapping = sb.ToString();
            }
            return result;
        }
    }
}