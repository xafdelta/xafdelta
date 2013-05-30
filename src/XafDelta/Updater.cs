using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using XafDelta.Properties;

namespace XafDelta
{
    /// <summary>
    /// Updater
    /// </summary>
    public class Updater : ModuleUpdater
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Updater"/> class.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="currentDBVersion">The current DB version.</param>
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion) { }

        /// <summary>
        /// Performs a database update after the database schema is updated.
        /// </summary>
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            var holder = CurrentNodeHolder.GetInstance(ObjectSpace) ?? ObjectSpace.CreateObject<CurrentNodeHolder>();

            if (holder.CurrentNode == null)
            {
                // create new replication node
                holder.CurrentNode = ObjectSpace.CreateObject<ReplicationNode>();

                var settingsNodeId = Settings.Default.CurrentReplicationNodeId;
                if (!string.IsNullOrEmpty(settingsNodeId))
                    holder.CurrentNode.NodeId = settingsNodeId;

                // try to get the name for a new replication node using OnInitCurrentNodeId event
                var args = new InitCurrentNodeIdArgs(holder.CurrentNode.NodeId);
                XafDeltaModule.OnInitCurrentNodeId(args);
                if (!string.IsNullOrEmpty(args.NodeId))
                    holder.CurrentNode.NodeId = args.NodeId;

                holder.CurrentNode.Name = args.NodeId;
            }

            ObjectSpace.CommitChanges();
        }
    }

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.InitCurrentNodeId"/> event. 
    /// You can specify current replication node <see cref="NodeId"/> (based on database name, app settings etc.) in event handler.
    /// </summary>
    public class InitCurrentNodeIdArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the current node id. You can specify current replication node <see cref="NodeId"/> 
        /// (based on database name, app settings etc.) in event handler.
        /// </summary>
        /// <value>
        /// The node id.
        /// </value>
        public string NodeId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitCurrentNodeIdArgs"/> class.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        public InitCurrentNodeIdArgs(string nodeId)
        {
            NodeId = nodeId;
        }
    }
}
