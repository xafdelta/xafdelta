using System;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;

namespace XafDelta.ReadOnlyParameters
{
    /// <summary>
    /// Read only parameter represents current replication node Id
    /// </summary>
   public class CurrentNodeIdParameter : ReadOnlyParameter 
   {
       /// <summary>
       /// Initializes a new instance of the <see cref="CurrentNodeIdParameter"/> class.
       /// </summary>
       public CurrentNodeIdParameter() : base("CurrentNodeId", typeof(string)) { }

       /// <summary>
       /// Gets the current value.
       /// </summary>
       public override object CurrentValue {get { return XafDeltaModule.Instance.CurrentNodeId; } }
   }


   /* 11.2.7 */

   /// <summary>
   /// Current node operator
   /// </summary>
   public class CurrentNodeIdOperator : ICustomFunctionOperator
   {
       private static bool registered;
       static CurrentNodeIdOperator()
       {
           if (!registered)
           {
               CriteriaOperator.RegisterCustomFunction(new CurrentNodeIdOperator());
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
           return  XafDeltaModule.Instance.CurrentNodeId;
       }

       /// <summary>
       /// When implemented by a custom function, specifies its name.
       /// </summary>
       /// <value>
       /// A <a href="#" onclick="dxHelpRedirect('T:System.String')">String</a> used to identify a custom function.
       /// </value>
       public string Name
       {
           get { return "CurrentNodeId"; }
       }
   }

}
