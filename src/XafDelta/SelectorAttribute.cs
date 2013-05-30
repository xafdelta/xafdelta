using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XafDelta
{
    /// <summary>
    /// Base attribute for selectors
    /// </summary>
    public abstract class SelectorAttribute : Attribute
    {
        /// <summary>
        /// Gets the criteria expression.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Gets the type of the recipient selector.
        /// </summary>
        /// <value>
        /// The type of the recipient selector.
        /// </value>
        public SelectorType SelectorType { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        protected SelectorAttribute(string expression)
            : this(expression, SelectorType.Include)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientSelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="selectorType">Type of the recipient selector.</param>
        /// <param name="name">The name.</param>
        protected SelectorAttribute(string expression, SelectorType selectorType, string name)
        {
            Expression = expression;
            SelectorType = selectorType;
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorAttribute"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="selectorType">Type of the selector.</param>
        protected SelectorAttribute(string expression, SelectorType selectorType)
            : this(expression, SelectorType.Include, null)
        {
            Expression = expression;
            SelectorType = selectorType;
        }
    }
}
