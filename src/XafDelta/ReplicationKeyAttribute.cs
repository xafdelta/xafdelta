using System;

namespace XafDelta
{
    /// <summary>
    /// Replication key attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ReplicationKeyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicationKeyAttribute"/> class.
        /// </summary>
        public ReplicationKeyAttribute() : this(false, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicationKeyAttribute"/> class.
        /// </summary>
        /// <param name="isCaseInsensitive">if set to <c>true</c> [is case insensitive].</param>
        /// <param name="isSpaceInsensitive">if set to <c>true</c> [is space insensitive].</param>
        public ReplicationKeyAttribute(bool isCaseInsensitive, bool isSpaceInsensitive)
        {
            IsCaseInsensitive = isCaseInsensitive;
            IsSpaceInsensitive = isSpaceInsensitive;
        }

        /// <summary>
        /// Gets a value indicating whether XafDelta have to use case insensitive seach while looking for object by replication key value.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is case insensitive; otherwise, <c>false</c>.
        /// </value>
        public bool IsCaseInsensitive { get; set; }

        /// <summary>
        /// Gets a value indicating whether XafDelta have to ignore space symbols while looking for object by replication key value.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is space insensitive; otherwise, <c>false</c>.
        /// </value>
        public bool IsSpaceInsensitive { get; set; }
    }
}
