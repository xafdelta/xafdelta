using System;

namespace XafDelta
{
    /// <summary>
    /// Snapshot selector attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class SnapshotSelectorAttribute : SelectorAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public SnapshotSelectorAttribute(string expression) 
            : base(expression)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="selectorType">Type of the selector.</param>
        public SnapshotSelectorAttribute(string expression, SelectorType selectorType) 
            : base(expression, selectorType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="selectorType">Type of the selector.</param>
        /// <param name="name">The name.</param>
        public SnapshotSelectorAttribute(string expression, SelectorType selectorType, string name) 
            : base(expression, selectorType, name)
        {
            
        }
    }
}
