using System;
using XafDelta.Localization;

namespace XafDelta.Exceptions
{
    /// <summary>
    /// Invalid package data exception. 
    /// Raises while import when package specification does not match marker data.
    /// </summary>
    [Serializable]
    public class InvalidPackageDataException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPackageDataException"/> class.
        /// </summary>
        /// <param name="errorText">The error text.</param>
        public InvalidPackageDataException(string errorText) : 
            base(Localizer.InvalidPackageData + errorText)
        {
        }
    }
}