using System;
using System.IO;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using XafDelta.Exceptions;

namespace XafDelta
{
    /// <summary>
    /// Replication node
    /// </summary>
    [DefaultClassOptions]
    public class ReplicationNode : HCategory
    {
        /// <summary>
        /// Recipient Node Id for broadcast replicas
        /// </summary>
        public static readonly string AllNodes = "AllNodes";

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicationNode"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public ReplicationNode(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Persists the current object.
        /// </summary>
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            NodeId = "Node" + DateTime.UtcNow.Ticks.ToString("X");
            Name = NodeId;
        }

        /// <summary>
        /// Called when deleting.
        /// </summary>
        protected override void OnDeleting()
        {
            if (Equals(CurrentNodeHolder.GetInstance(Session).CurrentNode))
                throw new CurrentNodeDeletionException();
            base.OnDeleting();
        }

        /// <summary>
        /// Called when changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected override void OnChanged(string propertyName, object oldValue, object newValue)
        {
            if(!IsLoading)
            {
                if (propertyName == "NodeId")
                {
                    if(newValue.ToString() == AllNodes)
                        throw new InvalidReplicationNodeIdException();
                    var invalidChars =
                        newValue.ToString().ToCharArray().Intersect(Path.GetInvalidFileNameChars()).ToArray();
                    if(invalidChars.Length > 0)
                        throw new InvalidCharInNodeIdException(invalidChars);
                    XafDeltaModule.Instance.ClearCurrentNodeIdCache();
                }
            }
            base.OnChanged(propertyName, oldValue, newValue);
        }

        /// <summary>
        /// Gets the parent node.
        /// </summary>
        [VisibleInDetailView(false), VisibleInListView(false)]
        public ReplicationNode ParentNode { get { return (ReplicationNode) Parent; } }

        /// <summary>
        /// Gets or sets the node id.
        /// </summary>
        /// <value>
        /// Replication node unique Id.
        /// </value>
        [ReplicationKey]
        public string NodeId
        {
            get { return GetPropertyValue<string>("NodeId"); }
            set { SetPropertyValue("NodeId", value); }
        }

        /// <summary>
        /// Gets or sets the transport address.
        /// </summary>
        /// <value>
        /// Replication node transport address.
        /// </value>
        public string TransportAddress
        {
            get { return GetPropertyValue<string>("TransportAddress"); }
            set { SetPropertyValue("TransportAddress", value); }
        }

        /// <summary>
        /// Gets or sets the last saved package number.
        /// </summary>
        /// <value>
        /// The last saved to node package number.
        /// </value>
        [IsLocal]
        public int LastSavedPackageNumber
        {
            get { return GetPropertyValue<int>("LastSavedPackageNumber"); }
            set { SetPropertyValue("LastSavedPackageNumber", value); }
        }

        /// <summary>
        /// Gets or sets the last loaded package number.
        /// </summary>
        /// <value>
        /// The last loaded package number.
        /// </value>
        [IsLocal]
        public int LastLoadedPackageNumber
        {
            get { return GetPropertyValue<int>("LastLoadedPackageNumber"); }
            set { SetPropertyValue("LastLoadedPackageNumber", value); }
        }

        /// <summary>
        /// Gets or sets the last saved Snapshot number.
        /// </summary>
        /// <value>
        /// The last saved to node Snapshot number.
        /// </value>
        [IsLocal]
        public int LastSavedSnapshotNumber
        {
            get { return GetPropertyValue<int>("LastSavedSnapshotNumber"); }
            set { SetPropertyValue("LastSavedSnapshotNumber", value); }
        }

        /// <summary>
        /// Gets or sets the last loaded Snapshot number.
        /// </summary>
        /// <value>
        /// The last loaded Snapshot number.
        /// </value>
        [IsLocal]
        public int LastLoadedSnapshotNumber
        {
            get { return GetPropertyValue<int>("LastLoadedSnapshotNumber"); }
            set { SetPropertyValue("LastLoadedSnapshotNumber", value); }
        }

        /// <summary>
        /// Gets or sets the snapshot date time.
        /// </summary>
        /// <value>
        /// The last snapshot date time.
        /// </value>
        [IsLocal]
        [Custom("DisplayFormat", "{0:G}")]
        public DateTime SnapshotDateTime
        {
            get { return GetPropertyValue<DateTime>("SnapshotDateTime"); }
            set { SetPropertyValue("SnapshotDateTime", value); }
        }
       
        /// <summary>
        /// Gets or sets the Init date time.
        /// </summary>
        /// <value>
        /// The last Init date time.
        /// </value>
        [IsLocal]
        [Custom("DisplayFormat", "{0:G}")]
        public DateTime InitDateTime
        {
            get { return GetPropertyValue<DateTime>("InitDateTime"); }
            set { SetPropertyValue("InitDateTime", value); }
        }
        

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ReplicationNode"/> is disabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disabled; otherwise, <c>false</c>.
        /// </value>
        [IsLocal]
        public bool Disabled
        {
            get { return GetPropertyValue<bool>("Disabled"); }
            set { SetPropertyValue("Disabled", value); }
        }

        /// <summary>
        /// Gets the current node.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>Current replication node</returns>
        public static ReplicationNode GetCurrentNode(Session session)
        {
            return CurrentNodeHolder.GetCurrentNode(session);
        }

        /// <summary>
        /// Gets the current node.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>Current replication node</returns>
        public static ReplicationNode GetCurrentNode(IObjectSpace objectSpace)
        {
            return CurrentNodeHolder.GetCurrentNode(objectSpace);
        }

        /// <summary>
        /// Gets the current node id.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>Current replication node Id</returns>
        public static string GetCurrentNodeId(IObjectSpace objectSpace)
        {
            return GetCurrentNode(objectSpace).NodeId;
        }

        /// <summary>
        /// Finds the node by NodeId.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="nodeId">The node id.</param>
        /// <returns>Replication node having specified <paramref name="nodeId"/></returns>
        public static ReplicationNode FindNode(Session session, string nodeId)
        {
            return session.FindObject<ReplicationNode>(CriteriaOperator.Parse("NodeId = ?", nodeId));
        }

        /// <summary>
        /// Finds the node by NodeId.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="nodeId">The node id.</param>
        /// <returns>Replication node having specified <paramref name="nodeId"/></returns>
        public static ReplicationNode FindNode(IObjectSpace objectSpace, string nodeId)
        {
            return objectSpace.FindObject<ReplicationNode>(
                CriteriaOperator.Parse("NodeId = ?", nodeId));
        }
    }
}