using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Object reference interface. 
    /// Contains XAF object identity, including it's AssemblyName, ClassName, ObjectId, ReplicationKey
    /// </summary>
    public interface IObjectReference
    {
        /// <summary>
        /// Gets or sets the name of the assembly.
        /// </summary>
        /// <value>
        /// The name of the assembly.
        /// </value>
        [Size(100)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        string AssemblyName { get;  }

        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        /// <value>
        /// The name of the class.
        /// </value>
        [Size(100)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        string ClassName { get;  }

        /// <summary>
        /// Gets or sets the assembly qualified name.
        /// </summary>
        /// <value>
        /// The assembly qualified name.
        /// </value>
        [Size(255)]
        string AssemblyQualifiedName { get;  }

        /// <summary>
        /// Gets or sets the object id(key).
        /// </summary>
        /// <value>
        /// The object id.
        /// </value>
        [Size(50)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        string ObjectId { get;  }

        /// <summary>
        /// Gets or sets the replication key.
        /// Replication key is a string, uniquely identifies object in replication net.
        /// </summary>
        /// <value>
        /// The replication key.
        /// </value>
        [Size(255)]
        [RuleRequiredField("", DefaultContexts.Save)]
        [Indexed]
        string ReplicationKey { get;  }
    }
}