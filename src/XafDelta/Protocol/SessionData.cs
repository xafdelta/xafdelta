using System;
using System.Linq;
using System.Threading;
using DevExpress.ExpressApp.DC.Xpo;
using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo.Metadata.Helpers;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Session protocoling processor
    /// </summary>
    internal sealed class SessionData : IDisposable
    {
        private SessionEvents sessionEvents  { get { return SessionEvents.GetEvents(this); } }
        private readonly TrackedObjectBag trackedObjects;
        private bool hasCommitedNestedSessions;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionData"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="collector">The collector.</param>
        public SessionData(Session session, Collector collector)
        {
            if (session == null) throw new ArgumentNullException("session");
            Session = session;
            Collector = collector;
            SessionId = Guid.NewGuid();
            StartedOn = DateTime.UtcNow;
            trackedObjects = new TrackedObjectBag(this);
            subscribeToSessionEvents(session);
        }

        /// <summary>
        /// Gets the collector which owns this object.
        /// </summary>
        public Collector Collector { get; private set; }

        /// <summary>
        /// Gets the session id.
        /// </summary>
        public Guid SessionId { get; private set; }

        /// <summary>
        /// Gets the session.
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public SessionData Parent { get; set; }

        /// <summary>
        /// Gets the started on date and time.
        /// </summary>
        public DateTime StartedOn { get; private set; }

        /// <summary>
        /// Subscribes to session events.
        /// </summary>
        /// <param name="session">The session.</param>
        private void subscribeToSessionEvents(Session session)
        {
            session.ObjectChanged += session_ObjectChanged;
            session.ObjectDeleting += session_ObjectDeleting;
            session.ObjectDeleted += session_ObjectDeleted;
            session.AfterCommitTransaction += session_AfterCommitTransaction;
            session.AfterRollbackTransaction += session_AfterRollbackTransaction;
            session.ObjectLoaded += session_ObjectLoaded;
        }

        /// <summary>
        /// Uns the subscribe from session events.
        /// </summary>
        /// <param name="session">The session.</param>
        private void unSubscribeFromSessionEvents(Session session)
        {
            session.ObjectChanged -= session_ObjectChanged;
            session.ObjectDeleted -= session_ObjectDeleted;
            session.AfterCommitTransaction -= session_AfterCommitTransaction;
            session.AfterRollbackTransaction -= session_AfterRollbackTransaction;
            session.ObjectLoaded -= session_ObjectLoaded;
        }

        /// <summary>
        /// Handles the AfterRollbackTransaction event of the session control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Xpo.SessionManipulationEventArgs"/> instance containing the event data.</param>
        private void session_AfterRollbackTransaction(object sender, SessionManipulationEventArgs e)
        {
            // clear lists
            trackedObjects.Clear();
            sessionEvents.Rollback();
        }

        /// <summary>
        /// Handles the AfterCommitTransaction event of the session control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Xpo.SessionManipulationEventArgs"/> instance containing the event data.</param>
        private void session_AfterCommitTransaction(object sender, SessionManipulationEventArgs e)
        {
            // save changes
            saveProtocolData();
            trackedObjects.Refresh();
        }

        /// <summary>
        /// Registers the protocol event.
        /// </summary>
        /// <param name="protocolEvent">The protocol event.</param>
        public void RegisterProtocolEvent(ProtocolEvent protocolEvent)
        {
            // prevent event registration at loading and inside registered method
            if (Collector.ProtocolService.IsSessionInMethod(Session)
                || Collector.ProtocolService.LoadService.IsLoading) return;

            if(protocolEvent.Target != null)
            {
                if(Collector.IsNotForProtocol(protocolEvent.Target.GetType()))
                    return;

                if (!string.IsNullOrEmpty(protocolEvent.PropertyName) && 
                    Collector.IsNotForProtocol(protocolEvent.Target.GetType(), protocolEvent.PropertyName))
                        return;
            }

            sessionEvents.ProtocolEvents.Add(protocolEvent);
        }

        /// <summary>
        /// Registers the object creation.
        /// </summary>
        /// <param name="newObject">The new object.</param>
        /// <param name="createDateTime">The create date time.</param>
        /// <param name="eventId">The event id.</param>
        internal void RegisterObjectCreation(object newObject, DateTime createDateTime, long eventId)
        {
            if (newObject == null) throw new ArgumentNullException("newObject");

            if (!isMaintenanceObject(newObject) && !Collector.IsNotForProtocol(newObject.GetType()))
            {
                var newEvent = new ProtocolEvent
                                   {
                                       Target = newObject,
                                       ProtocolEventType = ProtocolEventType.ObjectCreated,
                                       EventDateTime = createDateTime,
                                       EventId = eventId
                                   };



                RegisterProtocolEvent(newEvent);

                // load initial values for brand new object
                trackedObjects.Refresh(newObject);
            }
        }

        private long saving;
        /// <summary>
        /// Saves the protocol data for current session.
        /// </summary>
        private void saveProtocolData()
        {
            if (Interlocked.Read(ref saving) == 0)
            {
                Interlocked.Increment(ref saving);
                try
                {
                    // determine wherether saving is needed
                    if (Collector.ProtocolService.LoadService.IsLoading) return;

                    var needCommitRecord = (sessionEvents.ProtocolEvents.Count != 0) || hasCommitedNestedSessions;
                    if (!needCommitRecord) return;

                    // if current session is nested then mark parent session as modified
                    if (Parent != null)
                        Parent.hasCommitedNestedSessions = true;

                    RegisterProtocolEvent(new ProtocolEvent { ProtocolEventType = ProtocolEventType.CommitSession });
                    sessionEvents.CommitedOn = DateTime.UtcNow;

                    if (Parent != null)
                        sessionEvents.CastReferencesToParent();
                    else
                        sessionEvents.PersistEvents();

                    ((UnitOfWork) Session).CommitChanges();
                }
                finally
                {
                    Interlocked.Decrement(ref saving);
                }
            }
        }

        #region Changes tracking

        /// <summary>
        /// Handles the ObjectLoaded event of the session control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Xpo.ObjectManipulationEventArgs"/> instance containing the event data.</param>
        private void session_ObjectLoaded(object sender, ObjectManipulationEventArgs e)
        {
            if (isMaintenanceObject(e.Object)) return;
            trackedObjects.Refresh(e.Object);
        }

        /// <summary>
        /// Handles the ObjectDeleting event of the session control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Xpo.ObjectManipulationEventArgs"/> instance containing the event data.</param>
        private void session_ObjectDeleting(object sender, ObjectManipulationEventArgs e)
        {
            if (isDcManyToManyLinkObject(e.Object))
            {

                var propMember = Session.GetClassInfo(e.Object).ObjectProperties.Cast<XPMemberInfo>()
                    .Where(x => x.Name.EndsWith(SpecificWords.LinkedPostfix)).FirstOrDefault();

                if (propMember != null)
                    registerDcManyToManyChange(e.Object, propMember,
                        propMember.GetValue(e.Object), ProtocolEventType.RemovedFromCollection, false);
            }
        }

        /// <summary>
        /// Handles the ObjectDeleted event of the session control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Xpo.ObjectManipulationEventArgs"/> instance containing the event data.</param>
        private void session_ObjectDeleted(object sender, ObjectManipulationEventArgs e)
        {
            if (isMaintenanceObject(e.Object)) return;

            if (!isDcManyToManyLinkObject(e.Object))
            {
                RegisterProtocolEvent(new ProtocolEvent
                                          {
                                              Target = e.Object,
                                              ProtocolEventType = ProtocolEventType.ObjectDeleted,
                                              ReplicationKey = ExtensionsHelper.GetReplicationKey(e.Object)
                                          });
            }

            trackedObjects.Remove(e.Object);
        }

        /// <summary>
        /// Handles the ObjectChanged event of the session control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DevExpress.Xpo.ObjectChangeEventArgs"/> instance containing the event data.</param>
        private void session_ObjectChanged(object sender, ObjectChangeEventArgs e)
        {
            if(e.Object == null) return;
            if (e.Session.IsObjectMarkedDeleted(e.Object)) return;
            if (isMaintenanceObject(e.Object)) return;
            if (isMaintenanceProperty(e.Object, e.PropertyName)) return;

            switch (e.Reason)
            {
                case ObjectChangeReason.BeginEdit:
                case ObjectChangeReason.CancelEdit:
                case ObjectChangeReason.EndEdit:
                case ObjectChangeReason.Reset:
                    // refresh old values for object
                    trackedObjects.Refresh(e.Object);
                    break;
                case ObjectChangeReason.PropertyChanged:
                    if (!string.IsNullOrEmpty(e.PropertyName))
                    {
                        var session = e.Session;

                        var propertyName = e.PropertyName;
                        var propMember = session.GetClassInfo(e.Object).FindMember(propertyName);
                        if (propMember == null || propMember.IsCollection) return;

                        // get new and old values
                        var newValue = e.NewValue;
                        if (e.OldValue == e.NewValue && e.NewValue == null)
                            newValue = propMember.GetValue(e.Object);

                        var oldValue = e.OldValue;
                        if (!session.IsNewObject(e.Object))
                            oldValue = trackedObjects.GetPropertyOldValue(e.Object, propertyName);

                        // DC many to many collections
                        if (isDCManyToManyLink(propMember))
                        {
                            if (e.OldValue != null)
                                registerDcManyToManyChange(e.Object, propMember, e.OldValue,
                                                           ProtocolEventType.RemovedFromCollection, false);
                            if (e.NewValue != null)
                                registerDcManyToManyChange(e.Object, propMember, e.NewValue,
                                                           ProtocolEventType.AddedToCollection, false);

                            break;
                        }


                        var eventType = session.IsNewObject(e.Object) 
                            && sessionEvents.ProtocolEvents.Find(x => x.Target == e.Object 
                                && x.ProtocolEventType == ProtocolEventType.ObjectCreated) == null
                                            ? ProtocolEventType.InitialValueAssigned
                                            : ProtocolEventType.ObjectChanged;

                        // for InitialValueAssigned events replication key is null
                        var replicationKeyValue = eventType == ProtocolEventType.InitialValueAssigned
                            ? null : trackedObjects.GetReplicationKeyOldValue(e.Object);

                        RegisterProtocolEvent(new ProtocolEvent
                                                  {
                                                      Target = e.Object,
                                                      ProtocolEventType = eventType,
                                                      PropertyName = propertyName,
                                                      NewValue = newValue,
                                                      OldValue = oldValue,
                                                      ReplicationKey = replicationKeyValue
                                                  });

                        // assume new value = old value
                        trackedObjects.SetPropertyOldValue(e.Object, propertyName, newValue);

                        // refresh replication key old value
                        var replKeyMember = ExtensionsHelper.GetReplicationKeyMember(e.Object);
                        if (replKeyMember != null)
                        {
                            var newReplicationKeyValue = ExtensionsHelper.GetReplicationKey(e.Object);
                            if(newReplicationKeyValue != replicationKeyValue)
                                trackedObjects.SetPropertyOldValue(e.Object, 
                                    replKeyMember.Name, newReplicationKeyValue);
                        }
                    }
                    break;
            }
        }

        private void registerDcManyToManyChange(object linkObject, XPMemberInfo propMember, object element, ProtocolEventType protocolEventType, bool recurse)
        {
            var oppositeProp =
                (from p in propMember.Owner.ObjectProperties.Cast<XPMemberInfo>()
                 where p.Name.EndsWith(SpecificWords.LinkedPostfix) && p != propMember
                 select p).FirstOrDefault();

            if(oppositeProp != null)
            {
                var targetObject = oppositeProp.GetValue(linkObject);
                if(targetObject != null)
                {
                    var targetModelClass = XafDeltaModule.XafApp.FindModelClass(targetObject.GetType());
                    if (targetModelClass != null)
                    {
                        var nameArray = propMember.Name.Split('_').ToList();
                        if(nameArray.Count > 2)
                        {
                            nameArray.RemoveAt(0);
                            nameArray.RemoveAt(nameArray.Count-1);
                            var targetListName = string.Join("_", nameArray.ToArray());

                            var protEvent = new ProtocolEvent
                            {
                                Target = targetObject,
                                OldValue = element,
                                PropertyName = targetListName,
                                ProtocolEventType = protocolEventType,
                                ReplicationKey = ExtensionsHelper.GetReplicationKey(targetObject)
                            };

                            var session = ((ISessionProvider)targetObject).Session;

                            Collector.RegisterProtocolEvent(session, protEvent);

                            if (!recurse)
                                registerDcManyToManyChange(linkObject, oppositeProp, targetObject, protocolEventType, true);
                        }
                    }
                }
            }
        }

        private bool isDCManyToManyLink(XPMemberInfo propMember)
        {
            var result = false;
            if(propMember != null)
            {
                result = propMember.Name.EndsWith(SpecificWords.LinkedPostfix)
                         && propMember.Owner.FullName.EndsWith(SpecificWords.LinkedPostfix)
                         && propMember.Owner.ObjectProperties.Cast<XPMemberInfo>().Count() == 2
                         && string.IsNullOrEmpty(propMember.Owner.ClassType.Assembly.Location);
            }
            return result;
        }

        private bool isDcManyToManyLinkObject(object source)
        {
            var result = false;
            if (source != null)
            {
                var classInfo = Session.GetClassInfo(source);
                result = classInfo.FullName.EndsWith(SpecificWords.LinkedPostfix)
                         && classInfo.ObjectProperties.Cast<XPMemberInfo>().Count() == 2
                         && string.IsNullOrEmpty(classInfo.ClassType.Assembly.Location);
            }
            return result;
        }

        /// <summary>
        /// Determines whether the specified property is maintenance.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>
        ///   <c>true</c> if property is maintenance; otherwise, <c>false</c>.
        /// </returns>
        private bool isMaintenanceProperty(object target, string propertyName)
        {
            var classInfo = Session.GetClassInfo(target);
            var propInfo = classInfo.FindMember(propertyName);
            return propInfo == null || (propInfo is ServiceField) 
                || propInfo.IsReadOnly 
                || propInfo.Owner.ClassType.IsAssignableFrom(typeof(XPBaseObject)) 
                || Collector.IsNotForProtocol(classInfo.ClassType)
                || Collector.IsNotForProtocol(classInfo.ClassType, propertyName);
        }

        /// <summary>
        /// Determines whether the specified object is maintenance.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>
        ///   <c>true</c> if object is maintenance; otherwise, <c>false</c>.
        /// </returns>
        private bool isMaintenanceObject(object source)
        {
            var classInfo = Session.GetClassInfo(source);
            var result = classInfo == null;
            if (!result)
            {
                result = !classInfo.IsPersistent
                         || classInfo.BaseClass.ClassType == typeof (XPBaseObject)
                         || Collector.IsNotForProtocol(classInfo.ClassType)
                         || (source is IntermediateObject);

            }
            return result;
        }

        #endregion

        #region IDisposable

        private bool isDisposed;
        public void Dispose()
        {
            if (isDisposed) return;
            trackedObjects.Dispose();
            unSubscribeFromSessionEvents(Session);
            SessionEvents.Remove(this);
            isDisposed = true;
        }

        #endregion
    }
}