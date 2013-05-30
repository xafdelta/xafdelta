using System;

namespace XafDelta
{
    /// <summary>
    /// Disables protocoling for class, interface or property applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    public sealed class NotForProtocolAttribute: Attribute
    {
    }
}
