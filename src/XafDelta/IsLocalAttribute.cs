using System;

namespace XafDelta
{
    /// <summary>
    /// Indicates whether element applied to is out of a replication process. 
    /// Equivalent of NonReplicable + NonSnapshot + NotForProtocol
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property)]
    public class IsLocalAttribute : Attribute
    {
    }
}
