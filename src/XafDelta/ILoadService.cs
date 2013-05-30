namespace XafDelta
{
    /// <summary>
    /// Load service
    /// </summary>
    internal interface ILoadService
    {
        /// <summary>
        /// Gets a value indicating whether this instance is loading.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is loading; otherwise, <c>false</c>.
        /// </value>
        bool IsLoading { get; }
    }
}
