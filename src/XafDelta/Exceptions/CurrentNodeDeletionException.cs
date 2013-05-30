using System;
using XafDelta.Localization;

namespace XafDelta.Exceptions
{
    /// <summary>
    /// Current node deletion exception. Raised when user attempts to delete the current replication node.
    /// </summary>
    [Serializable]
    public class CurrentNodeDeletionException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentNodeDeletionException"/> class.
        /// </summary>
        public CurrentNodeDeletionException() : base(Localizer.CantDeleteNode)
        {
        }
    }
}