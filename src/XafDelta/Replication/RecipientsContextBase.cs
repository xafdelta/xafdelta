using DevExpress.Xpo;
using XafDelta.Protocol;

namespace XafDelta.Replication
{
    /// <summary>
    /// Base selection context for recipient selector.
    /// For internal use only.
    /// </summary>
    [NonPersistent]
    [IsLocal]
    public abstract class RecipientsContextBase : XPCustomObject 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientsContextBase"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        protected RecipientsContextBase(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Gets or sets the event data.
        /// </summary>
        /// <value>
        /// ProtocolRecord object contains audit event data.
        /// </value>
        public ProtocolRecord ProtocolRecord
        {
            get { return GetPropertyValue<ProtocolRecord>("ProtocolRecord"); }
            set { SetPropertyValue("ProtocolRecord", value); }
        }

        /// <summary>
        /// Gets or sets the target node.
        /// </summary>
        /// <value>
        /// The replication node - candidate to be recipient for <see cref="ProtocolRecord"/>.
        /// </value>
        public ReplicationNode RecipientNode
        {
            get { return GetPropertyValue<ReplicationNode>("RecipientNode"); }
            set { SetPropertyValue("RecipientNode", value); }
        }

        /// <summary>
        /// Gets or sets the sender node.
        /// </summary>
        /// <value>
        /// The sender replication node (i.e. current node).
        /// </value>
        public ReplicationNode SenderNode
        {
            get { return GetPropertyValue<ReplicationNode>("SenderNode"); }
            set { SetPropertyValue("SenderNode", value); }
        }

        /// <summary>
        /// Gets or sets the type of the replication.
        /// </summary>
        /// <value>
        /// The type of the replication.
        /// </value>
        public RoutingType RoutingType
        {
            get { return GetPropertyValue<RoutingType>("RoutingType"); }
            set { SetPropertyValue("RoutingType", value); }
        }
    }
}