using System;
using XafDelta.Localization;

namespace XafDelta.Exceptions
{
    /// <summary>
    /// Disabled target node exception
    /// </summary>
    [Serializable]
    public class DisabledTargetNodeException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisabledTargetNodeException"/> class.
        /// </summary>
        public DisabledTargetNodeException()
            : base(Localizer.CantCreateSnapshotForDisabled)
        {
            
        }
    }
}