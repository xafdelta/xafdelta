using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Method call parameters. Contains the list of parameters passed to method on call.
    /// </summary>
    public sealed class MethodCallParams: BaseObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodCallParams"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public MethodCallParams(Session session): base(session)
        {
        }

        /// <summary>
        /// Creates for argumets.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public static MethodCallParams CreateForArgs(IObjectSpace objectSpace, object[] args)
        {
            var i = 0;
            var result = objectSpace.CreateObject<MethodCallParams>();
            foreach (var arg in args)
            {
                var paramValue = MethodParamValue.CreateForArg(objectSpace, arg);
                paramValue.OrdNo = i++;
                paramValue.MethodCallParams = result;
            }
            return result;
        }

        /// <summary>
        /// Gets the method param values.
        /// </summary>
        [Aggregated]
        [Association("MethodCallParamValues")]
        public XPCollection<MethodParamValue> MethodParamValues
        {
            get { return GetCollection<MethodParamValue>("MethodParamValues"); }
        }

        /// <summary>
        /// Gets the param values.
        /// </summary>
        /// <returns></returns>
        public object[] GetParamValues()
        {
            return (from p in MethodParamValues orderby p.OrdNo select p.GetParamValue()).ToArray();
        }
    }
}
