using DevExpress.ExpressApp;
using XafDelta.Messaging;

namespace XafDelta.Replication
{
    /// <summary>
    /// Load package context
    /// </summary>
    public class LoadPackageContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadPackageContext"/> class.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="worker">The worker.</param>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="currentNodeId">The current node id.</param>
        public LoadPackageContext(Package package, ActionWorker worker, 
            IObjectSpace objectSpace, string currentNodeId)
        {
            CurrentNodeId = currentNodeId;
            Package = package;
            Worker = worker;
            ObjectSpace = objectSpace;
        }

        /// <summary>
        /// Gets the package.
        /// </summary>
        public Package Package { get; private set; }

        /// <summary>
        /// Gets the worker.
        /// </summary>
        public ActionWorker Worker { get; private set; }

        /// <summary>
        /// Gets the object space.
        /// </summary>
        public IObjectSpace ObjectSpace { get; private set; }

        /// <summary>
        /// Gets the current node id.
        /// </summary>
        public string CurrentNodeId { get; private set; }
    }
}