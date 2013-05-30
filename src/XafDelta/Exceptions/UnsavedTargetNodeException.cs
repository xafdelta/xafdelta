using System;
using XafDelta.Localization;

namespace XafDelta.Exceptions
{
    /// <summary>
    /// Unsaved target node exception
    /// </summary>
    [Serializable]
    public class UnsavedTargetNodeException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsavedTargetNodeException"/> class.
        /// </summary>
        public UnsavedTargetNodeException(): base(Localizer.ShouldSaveTargetNode)
        {
            
        }
    }
}