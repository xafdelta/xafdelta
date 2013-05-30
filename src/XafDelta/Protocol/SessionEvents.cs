using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Session events
    /// </summary>
    internal class SessionEvents
    {
        /// <summary>
        /// SessionEvents global list. Shared through all xaf application instances. Must be static.
        /// </summary>
        private static readonly List<SessionEvents> tree = new List<SessionEvents>();

        /// <summary>
        /// Prevents a default instance of the <see cref="SessionEvents"/> class from being created.
        /// </summary>
        private SessionEvents()
        {
            ProtocolEvents = new List<ProtocolEvent>();
        }

        /// <summary>
        /// Gets the SessionEvents for sessionData.
        /// </summary>
        /// <param name="sessionData">The session data.</param>
        /// <returns></returns>
        public static SessionEvents GetEvents(SessionData sessionData)
        {
            SessionEvents result = null;
            if (sessionData != null)
            {
                result = tree.Where(x => x.SessionData == sessionData).FirstOrDefault();
                if (result == null)
                {
                    result = new SessionEvents
                                 {
                                     SessionData = sessionData, 
                                     StartedOn = sessionData.StartedOn, 
                                     SessionId = sessionData.SessionId
                                 };
                    tree.Add(result);
                    if (sessionData.Parent != null)
                        result.Parent = GetEvents(sessionData.Parent);
                }
            }
            return result;
        }

        /// <summary>
        /// Removes the specified session data.
        /// </summary>
        /// <param name="sessionData">The session data.</param>
        public static void Remove(SessionData sessionData)
        {
            if(sessionData == null) return;
            var item = tree.Where(x => x.SessionData == sessionData).FirstOrDefault();
            if(item != null)
            {
                foreach (var child in item.AllChildren)
                    Remove(child.SessionData);

                if (sessionData.Parent == null)
                {
                    foreach (var child in item.AllChildren)
                        tree.Remove(child);
                    tree.Remove(item);
                }
                else
                {
                    item.SessionData = null;
                    if (item.ProtocolEvents.Count == 0)
                        tree.Remove(item);
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public SessionEvents Parent { get; set; }

        /// <summary>
        /// Gets the children.
        /// </summary>
        public IEnumerable<SessionEvents> Children
        {
            get { return from c in tree where c.Parent == this select c; }
        }

        /// <summary>
        /// Gets all children.
        /// </summary>
        public List<SessionEvents> AllChildren
        {
            get
            {
                var result = new List<SessionEvents>();
                result.AddRange(Children);
                result.AddRange(from c in Children from a in c.AllChildren select a);
                return result;
            }
        }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        /// <value>
        /// The session id.
        /// </value>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Gets or sets the session data.
        /// </summary>
        /// <value>
        /// The session data.
        /// </value>
        public SessionData SessionData { get; set; }

        /// <summary>
        /// Gets or sets the started on.
        /// </summary>
        /// <value>
        /// The started on.
        /// </value>
        public DateTime StartedOn { get; set; }

        /// <summary>
        /// Gets or sets the commited on.
        /// </summary>
        /// <value>
        /// The commited on.
        /// </value>
        public DateTime CommitedOn { get; set; }

        /// <summary>
        /// Gets the protocol events.
        /// </summary>
        public List<ProtocolEvent> ProtocolEvents { get; private set; }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            ProtocolEvents.Clear();
            Children.ToList().ForEach(x => x.Clear());
        }

        public void Rollback()
        {
            ProtocolEvents.RemoveAll(x => !x.IsCommited);
            foreach (var child in Children)
                child.Rollback();
        }

        /// <summary>
        /// Casts the references to parent.
        /// </summary>
        public void CastReferencesToParent()
        {
            castReferencesToParent(SessionData.Session);
        }

        /// <summary>
        /// Casts the references to parent in context of the session.
        /// </summary>
        /// <param name="session">The session.</param>
        private void castReferencesToParent(Session session)
        {
            if (session is NestedUnitOfWork)
            {
                var nestedUnitOfWork = session as NestedUnitOfWork;

                // cast references to specified session
                foreach (var protocolEvent in ProtocolEvents)
                {
                    protocolEvent.Target = castReferenceToParent(nestedUnitOfWork, protocolEvent.Target);
                    protocolEvent.OldValue = castReferenceToParent(nestedUnitOfWork, protocolEvent.OldValue);
                    protocolEvent.NewValue = castReferenceToParent(nestedUnitOfWork, protocolEvent.NewValue);
                }

                // make cast for all children
                foreach (var child in Children)
                    child.castReferencesToParent(session);

                // mark events commited
                (from c in Children from e in c.ProtocolEvents select e).ToList().ForEach(x => x.IsCommited = true);
            }
        }

        /// <summary>
        /// Casts the reference to parent.
        /// </summary>
        /// <param name="nestedUnitOfWork">The nested unit of work.</param>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        private object castReferenceToParent(NestedUnitOfWork nestedUnitOfWork, object source)
        {
            var result = source;
            if(source != null && source is IXPObject)
            {
                if ((source as IXPObject).Session != nestedUnitOfWork.Parent)
                    result = nestedUnitOfWork.GetParentObject(source);
            }
            return result;
        }

        /// <summary>
        /// Persists the events.
        /// </summary>
        public void PersistEvents()
        {
            persistSessionEvents(SessionData.Session);
            foreach (var child in AllChildren.Where(x => x.ProtocolEvents.Count > 0))
                child.persistSessionEvents(SessionData.Session);
            Clear();
        }

        /// <summary>
        /// Persists the session events.
        /// </summary>
        /// <param name="persistSession">The session.</param>
        private void persistSessionEvents(Session persistSession)
        {
            if(ProtocolEvents.Count == 0 && AllChildren.Count == 0) return;

            // create or find protocol session for current session data
            var protocolSession = getProtocolSession(persistSession);
            protocolSession.StartedOn = StartedOn;
            protocolSession.CommitedOn = CommitedOn;

            double timeOffset = 0;

            // for each protocol event create protocol record
            foreach (var protocolEvent in ProtocolEvents.OrderBy(x => x.EventId))
            {
                if (protocolEvent.IsNotForProtocol) continue;

                var protocolRecord = new ProtocolRecord(persistSession);

                if (protocolEvent.Target != null)
                    protocolRecord.AuditedObject = new AuditedObjectWeakReference(persistSession, protocolEvent.Target);

                protocolRecord.ProtocolSession = protocolSession;
                protocolRecord.OperationType = protocolEvent.ProtocolEventType.ToString();
                protocolRecord.UserName = SecuritySystem.CurrentUserName;
                protocolRecord.Description = protocolEvent.Description;
                protocolRecord.ModifiedOn = protocolEvent.EventDateTime.AddMilliseconds(timeOffset += 3);
                protocolRecord.PropertyName = protocolEvent.PropertyName;

                if (protocolEvent.NewValue != null)
                {
                    protocolRecord.NewValue = ValueTransform.ObjectToString(protocolEvent.NewValue);
                    

                    if (protocolEvent.NewValue is IXPObject)
                        protocolRecord.NewObject = new XPWeakReference(persistSession, protocolEvent.NewValue);
                }

                if (protocolEvent.OldValue != null)
                {
                    protocolRecord.OldValue = ValueTransform.ObjectToString(protocolEvent.OldValue);

                    if (protocolEvent.OldValue is IXPObject)
                        protocolRecord.OldObject = new XPWeakReference(persistSession, protocolEvent.OldValue);
                }

                protocolRecord.NewBlobValue = getNewBlobValueProperty(protocolEvent);

                // for ObjectCreated events replication key to current replication key value; otherwise 
                // use event's replication key value
                protocolRecord.ReplicationKey = protocolEvent.ProtocolEventType == ProtocolEventType.ObjectCreated
                                                    ? ExtensionsHelper.GetReplicationKey(protocolEvent.Target)
                                                    : protocolEvent.ReplicationKey;
            }
        }

        /// <summary>
        /// Gets the protocol session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        private ProtocolSession getProtocolSession(Session session)
        {
            var result = session.FindObject<ProtocolSession>(PersistentCriteriaEvaluationBehavior.InTransaction,
                                                             CriteriaOperator.Parse("SessionId = ?", SessionId));
            if(result == null)
            {
                result = new ProtocolSession(session) { SessionId = SessionId, StartedOn = StartedOn, CommitedOn = CommitedOn };
                if (Parent != null)
                    result.Parent = Parent.getProtocolSession(session);
            }
            return result;
        }

        /// <summary>
        /// Gets the new BLOB value property.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        private byte[] getNewBlobValueProperty(ProtocolEvent item)
        {
            if (item == null) return null;
            // check for operation type: NewBlobValue stored for InitialValueAssigned && ObjectChanged operation
            if (!(new[] { ProtocolEventType.InitialValueAssigned, ProtocolEventType.ObjectChanged }).Contains(item.ProtocolEventType))
                return null;
            // convert item.NewValue to blob if needed
            var blobValue = ValueTransform.ConvertToBlob(item.NewValue);
            return blobValue;
        }
    }
}