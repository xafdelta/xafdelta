using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;

namespace XafDelta.Replication
{
    /// <summary>
    /// Snapshot target
    /// </summary>
    [NonPersistent, DomainComponent]
    public class SnapshotTarget : XPObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotTarget"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public SnapshotTarget(Session session)
            : base(session)
        {
            
        }

        /// <summary>
        /// Gets or sets the target node.
        /// </summary>
        /// <value>
        /// The target node.
        /// </value>
        [DataSourceProperty("Candidated")]
        public ReplicationNode TargetNode { get; set; }

        /// <summary>
        /// Gets the candidated.
        /// </summary>
        [VisibleInDetailView(false)]
        public XPCollection<ReplicationNode> Candidated
        {
            get
            {
                return new XPCollection<ReplicationNode>(Session, CriteriaOperator.Parse("NodeId != '@CurrentNodeId'"));
            }
        }
    }
}
