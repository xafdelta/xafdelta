using System;

namespace XafDelta
{
    /// <summary>
    /// Non replicable attribute. Disables replication for class, interface or member applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property )]
    public sealed class NonReplicableAttribute : Attribute
    {
    }
}
