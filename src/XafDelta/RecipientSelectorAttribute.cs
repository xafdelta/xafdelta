using System;

namespace XafDelta
{
    /// <summary>
    /// Recipient selector attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class RecipientSelectorAttribute : SelectorAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public RecipientSelectorAttribute(string expression) 
            : base(expression)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="selectorType">Type of the selector.</param>
        public RecipientSelectorAttribute(string expression, SelectorType selectorType) 
            : base(expression, selectorType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="selectorType">Type of the selector.</param>
        /// <param name="name">The name.</param>
        public RecipientSelectorAttribute(string expression, SelectorType selectorType, string name)
            : base(expression, selectorType, name)
        {
        }
    }
}
