using DevExpress.Xpo;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Object reference interface used in packages
    /// </summary>
    public interface IPackageObjectReference : IObjectReference
    {
        /// <summary>
        /// Gets the object's known mapping in format "NodeId/aObjectId/n".
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        string KnownMapping { get; }
    }
}