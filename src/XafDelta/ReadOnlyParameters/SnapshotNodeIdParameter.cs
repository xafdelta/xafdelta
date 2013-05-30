using System;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;

namespace XafDelta.ReadOnlyParameters
{
    /// <summary>
    /// Snapshot node id parameter
    /// </summary>
    public class SnapshotNodeIdParameter : ReadOnlyParameter
    {
        /// <summary>
        /// Gets or sets the singleton.
        /// </summary>
        /// <value>
        /// The singleton.
        /// </value>
        public static SnapshotNodeIdParameter Instance { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentNodeIdParameter"/> class.
        /// </summary>
        public SnapshotNodeIdParameter() : base("SnapshotNodeId", typeof(string))
        {
            Instance = Instance ?? this;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public override object CurrentValue { get { return Instance.SnapshotNodeId; } }

        /// <summary>
        /// Gets or sets the snapshot node id.
        /// </summary>
        /// <value>
        /// The snapshot node id.
        /// </value>
        public string SnapshotNodeId { get; set; }
    }


    /* 11.2.7 */

    /// <summary>
    /// Snapshot node id operator
    /// </summary>
    public class SnapshotNodeIdOperator : ICustomFunctionOperator
    {
        private static bool registered;

        static SnapshotNodeIdOperator()
        {
            if (!registered)
            {
                CriteriaOperator.RegisterCustomFunction(new SnapshotNodeIdOperator());
                registered = true;
            }
        }

        /// <summary>
        /// Registers this operator.
        /// </summary>
        public static void Register() { }


        /// <summary>
        /// When implemented by a custom function, determines its return value type based on the type of function operands (parameters).
        /// </summary>
        /// <param name="operands">An array of function operator (parameter) types.</param>
        /// <returns>
        /// A <a href="#" onclick="dxHelpRedirect('T:System.Type')">Type</a> object specifying the return value type of a custom function.
        /// </returns>
        public Type ResultType(params Type[] operands)
        {
            return typeof(string);
        }

        /// <summary>
        /// When implemented by a custom function, evaluates it on the client.
        /// </summary>
        /// <param name="operands">An array of objects specifying function operands (parameters).</param>
        /// <returns>
        /// An <a href="#" onclick="dxHelpRedirect('T:System.Object')">Object</a> specifying a custom function's return value, calculated based on the <i>operands</i>.
        /// </returns>
        public object Evaluate(params object[] operands)
        {
            return SnapshotNodeIdParameter.Instance.SnapshotNodeId;
        }

        /// <summary>
        /// When implemented by a custom function, specifies its name.
        /// </summary>
        /// <value>
        /// A <a href="#" onclick="dxHelpRedirect('T:System.String')">String</a> used to identify a custom function.
        /// </value>
        public string Name
        {
            get { return "SnapshotNodeId"; }
        }
    }

}
