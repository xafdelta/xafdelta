using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace XafDelta
{
    /// <summary>
    /// Contains reference to current replication node. Singleton (only one instance in database is allowed).
    /// Created by XafDelta on database initialization or update.
    /// For internal use only.
    /// </summary>
    [IsLocal]
    public class CurrentNodeHolder : BaseObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentNodeHolder"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public CurrentNodeHolder(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Called when object was changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected override void OnChanged(string propertyName, object oldValue, object newValue)
        {
            base.OnChanged(propertyName, oldValue, newValue);
            if (IsLoading) return;
            // invalidate current node Id cache
            if(propertyName == "CurrentNode")
                XafDeltaModule.Instance.ClearCurrentNodeIdCache();
        }

        /// <summary>
        /// Gets or sets the current node.
        /// </summary>
        /// <value>
        /// The current node.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        public ReplicationNode CurrentNode
        {
            get { return GetPropertyValue<ReplicationNode>("CurrentNode"); }
            set { SetPropertyValue("CurrentNode", value); }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>Singleton</returns>
        public static CurrentNodeHolder GetInstance(Session session)
        {
            return session.FindObject<CurrentNodeHolder>(PersistentCriteriaEvaluationBehavior.InTransaction, null);
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>Singleton</returns>
        public static CurrentNodeHolder GetInstance(IObjectSpace objectSpace)
        {
            return GetInstance(((ObjectSpace) objectSpace).Session);
        }

        /// <summary>
        /// Gets the current replication node.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>Current replication node</returns>
        public static ReplicationNode GetCurrentNode(Session session)
        {
            return GetInstance(session).CurrentNode;
        }

        /// <summary>
        /// Gets the current replication node.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>Current replication node</returns>
        public static ReplicationNode GetCurrentNode(IObjectSpace objectSpace)
        {
            return GetInstance(objectSpace).CurrentNode;
        }
    }
}