namespace XafDelta
{
    /// <summary>
    /// Base class for internal XafDelta services.
    /// For internal use only.
    /// </summary>
    internal abstract class BaseService
    {
        /// <summary>
        /// Gets the owner XafDeltaModule.
        /// </summary>
        public XafDeltaModule Owner { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseService"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        protected BaseService(XafDeltaModule owner)
        {
            Owner = owner;
        }
    }
}
