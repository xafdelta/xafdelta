using System;
using XafDelta.Localization;

namespace XafDelta.Exceptions
{
    /// <summary>
    /// Invalid char in node id exception
    /// </summary>
    [Serializable]
    public class InvalidCharInNodeIdException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCharInNodeIdException"/> class.
        /// </summary>
        /// <param name="invalidChars">The invalid chars.</param>
        public InvalidCharInNodeIdException(char[] invalidChars)
            : base(string.Format(Localizer.NodeIdInvalidChars, new String(invalidChars)))
        {
            
        }
    }
}
