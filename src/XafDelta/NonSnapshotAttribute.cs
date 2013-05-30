using System;

namespace XafDelta
{
    /// <summary>
    /// Non snapshot attribute. Disables snapshot for class, interface or member applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    public sealed class NonSnapshotAttribute : Attribute
    {
    }
}
