using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.Metadata.Helpers;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Tracked object data
    /// </summary>
    internal sealed class TrackedObjectData : IDisposable
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="TrackedObjectData"/> class from being created.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="trackedObjectBag">The tracked object bag.</param>
        private TrackedObjectData(object targetObject, TrackedObjectBag trackedObjectBag)
        {
            if (targetObject == null) throw new ArgumentNullException("targetObject");

            OldValues = new Dictionary<string, object>();
            Collections = new Dictionary<XPBaseCollection, string>();
            TargetObject = targetObject;
            TrackedObjectBag = trackedObjectBag;
            subscribeToCollectionChanges();
        }

        /// <summary>
        /// Creates the tracked data.
        /// </summary>
        /// <param name="targetObject">The target object.</param>
        /// <param name="trackedObjectBag">The tracked object bag.</param>
        /// <returns>Object tracked data</returns>
        public static TrackedObjectData CreateTrackedData(object targetObject, TrackedObjectBag trackedObjectBag)
        {
            TrackedObjectData result = null;
            if(targetObject != null && !(targetObject is IntermediateObject) && targetObject is ISessionProvider)
                result = new TrackedObjectData(targetObject, trackedObjectBag);
            return result;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object TargetObject { get; private set; }
        /// <summary>
        /// Gets or sets the tracked object bag.
        /// </summary>
        /// <value>
        /// The tracked object bag.
        /// </value>
        public TrackedObjectBag TrackedObjectBag { get; set; }
        /// <summary>
        /// Gets the old values.
        /// </summary>
        public Dictionary<string, object> OldValues { get; private set; }
        /// <summary>
        /// Gets the collections subscripted to.
        /// </summary>
        public Dictionary<XPBaseCollection, string> Collections { get; private set; }

        /// <summary>
        /// Subscribes to collection changes.
        /// </summary>
        private void subscribeToCollectionChanges()
        {
            var session = ((ISessionProvider)TargetObject).Session;
            var classInfo = session.GetClassInfo(TargetObject);
            foreach (var member in classInfo.Members)
                if (member.IsAssociationList)
                {
                    var collectionObj = member.GetValue(TargetObject);
                    if (collectionObj is XPBaseCollection)
                    {
                        var collection = (XPBaseCollection) collectionObj;
                        collection.CollectionChanged += collection_CollectionChanged;
                        Collections.Add(collection, member.Name);
                    }
                }
        }
        /// <summary>
        /// Handles the CollectionChanged event of the collection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Xpo.XPCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void collection_CollectionChanged(object sender, XPCollectionChangedEventArgs e)
        {
            if (TrackedObjectBag.SessionData.Collector.ProtocolService.LoadService.IsLoading) return;

            ProtocolEventType protocolEventType;
            switch (e.CollectionChangedType)
            {
                case XPCollectionChangedType.AfterAdd:
                    protocolEventType = ProtocolEventType.AddedToCollection;
                    break;
                case XPCollectionChangedType.AfterRemove:
                    protocolEventType = ProtocolEventType.RemovedFromCollection;
                    break;
                default:
                    return;
            }
            var protEvent = new ProtocolEvent
                                {
                                    Target = TargetObject,
                                    OldValue = e.ChangedObject,
                                    PropertyName = Collections[(XPBaseCollection)sender],
                                    ProtocolEventType = protocolEventType,
                                    ReplicationKey = ExtensionsHelper.GetReplicationKey(TargetObject)
                                };
            var session = ((ISessionProvider)TargetObject).Session;
            TrackedObjectBag.SessionData.Collector.RegisterProtocolEvent(session, protEvent);
        }

        /// <summary>
        /// Unsubscribe from collection changes.
        /// </summary>
        private void unSubscribeFromCollectionChanges()
        {
            while (Collections.Count > 0)
            {
                var pair = Collections.ElementAt(0);
                var collection = pair.Key;
                collection.CollectionChanged -= collection_CollectionChanged;
                Collections.Remove(collection);
            }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            unSubscribeFromCollectionChanges();
            OldValues.Clear();
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            loadOldValues();
        }

        private bool isInLoad;
        /// <summary>
        /// Loads the old values.
        /// </summary>
        private void loadOldValues()
        {
            if(isInLoad) return;
            isInLoad = true;
            try
            {
                OldValues.Clear();
                var session = ((ISessionProvider)TargetObject).Session;
                var classInfo = session.GetClassInfo(TargetObject);
                foreach (var member in classInfo.Members)
                    if (!(member is ServiceField) && !member.IsCollection
                        && !TrackedObjectBag.SessionData.Collector.IsNotForProtocol(classInfo.ClassType)
                        && !TrackedObjectBag.SessionData.Collector.IsNotForProtocol(classInfo.ClassType, member.Name)
                        && !member.Owner.ClassType.IsAssignableFrom(typeof(XPBaseObject)))
                            OldValues.Add(member.Name, member.GetValue(TargetObject));
            }
            finally
            {
                isInLoad = false;
            }
        }

        #region IDisposable

        private bool isDisposed;
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            Close();
            isDisposed = true;
        }

        #endregion
    }
}