using System;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;

namespace XafDelta.ReadOnlyParameters
{
    /// <summary>
    /// Snapshot node parameter
    /// </summary>
    public class SnapshotNodeParameter : ReadOnlyParameter
    {
        /// <summary>
        /// Gets or sets the singleton.
        /// </summary>
        /// <value>
        /// The singleton.
        /// </value>
        public static SnapshotNodeParameter Instance { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotNodeParameter"/> class.
        /// </summary>
        public SnapshotNodeParameter()
            : base("SnapshotNode", typeof(ReplicationNode))
        {
            Instance = Instance ?? this;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public override object CurrentValue { get { return Instance.SnapshotNode; } }

        /// <summary>
        /// Gets or sets the snapshot node.
        /// </summary>
        /// <value>
        /// The snapshot node.
        /// </value>
        public ReplicationNode SnapshotNode { get; set; }
    }


    /* 11.2.7 */

    /// <summary>
    /// Snapshot node operator
    /// </summary>
    public class SnapshotNodeOperator : ICustomFunctionOperator
    {
        private static bool registered;

        static SnapshotNodeOperator()
        {
            if (!registered)
            {
                CriteriaOperator.RegisterCustomFunction(new SnapshotNodeOperator());
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
            return typeof(ReplicationNode);
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
            return SnapshotNodeParameter.Instance.SnapshotNode;
        }

        /// <summary>
        /// When implemented by a custom function, specifies its name.
        /// </summary>
        /// <value>
        /// A <a href="#" onclick="dxHelpRedirect('T:System.String')">String</a> used to identify a custom function.
        /// </value>
        public string Name
        {
            get { return "SnapshotNode"; }
        }
    }
}