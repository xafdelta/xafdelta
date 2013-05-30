using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Method parameter value
    /// </summary>
    public sealed class MethodParamValue : BaseObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodParamValue"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public MethodParamValue(Session session): base(session)
        {
        }

        /// <summary>
        /// Creates for argument.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="arg">The arg.</param>
        /// <returns></returns>
        public static MethodParamValue CreateForArg(IObjectSpace objectSpace, object arg)
        {
            var result = objectSpace.CreateObject<MethodParamValue>();
            result.ScalarValue = null;
            if (arg != null)
            {
                result.ScalarValue = arg.ToString();
                result.AssemblyQualifiedName = arg.GetType().AssemblyQualifiedName;
            }
            return result;
        }

        /// <summary>
        /// Gets or sets the ordinal number of parameter in list.
        /// </summary>
        /// <value>
        /// The ordinal number.
        /// </value>
        public int OrdNo
        {
            get { return GetPropertyValue<int>("OrdNo"); }
            set { SetPropertyValue("OrdNo", value); }
        }

        /// <summary>
        /// Gets or sets the method call params.
        /// </summary>
        /// <value>
        /// The method call params.
        /// </value>
        [Association("MethodCallParamValues")]
        public MethodCallParams MethodCallParams
        {
            get { return GetPropertyValue<MethodCallParams>("MethodCallParams"); }
            set { SetPropertyValue("MethodCallParams", value); }
        }

        /// <summary>
        /// Gets or sets the scalar value.
        /// </summary>
        /// <value>
        /// The scalar value.
        /// </value>
        [Size(SizeAttribute.Unlimited)]
        public string ScalarValue
        {
            get { return GetPropertyValue<string>("ScalarValue"); }
            set { SetPropertyValue("ScalarValue", value); }
        }

        /// <summary>
        /// Gets or sets the name of the assembly qualified.
        /// </summary>
        /// <value>
        /// The name of the assembly qualified.
        /// </value>
        [Size(255)]
        public string AssemblyQualifiedName
        {
            get { return GetPropertyValue<string>("AssemblyQualifiedName"); }
            set { SetPropertyValue("AssemblyQualifiedName", value); }
        }

        /// <summary>
        /// Gets the param value.
        /// </summary>
        /// <returns></returns>
        public object GetParamValue()
        {
            object result = null;
            if(ScalarValue != null)
            {
                if (AssemblyQualifiedName != null)
                {
                    var classType = Type.GetType(AssemblyQualifiedName);
                    if(classType != null)
                        result = Convert.ChangeType(ScalarValue, classType);
                }
            }
            return result;
        }
    }
}
