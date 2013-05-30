using System;
using XafDelta.Localization;

namespace XafDelta.Exceptions
{
    /// <summary>
    /// Invalid replication node id exception
    /// </summary>
    [Serializable]
    public class InvalidReplicationNodeIdException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidReplicationNodeIdException"/> class.
        /// </summary>
        public InvalidReplicationNodeIdException()
            : base(string.Format(Localizer.NodeIdAllNodes, ReplicationNode.AllNodes))
        {
            
        }
    }
}
