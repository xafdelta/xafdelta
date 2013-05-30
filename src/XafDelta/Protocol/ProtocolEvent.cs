using System;
using System.Threading;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Protocol event. In memory object used to store events data until persist it.
    /// For internal use only.
    /// </summary>
    internal sealed class ProtocolEvent
    {
        private static long lastEventId = long.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolEvent"/> class.
        /// </summary>
        public ProtocolEvent()
        {
            EventId = GetNextId();
            if (Interlocked.Read(ref lastEventId) == long.MaxValue)
                Interlocked.Exchange(ref lastEventId, long.MinValue);
            EventDateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the next id.
        /// </summary>
        /// <returns></returns>
        public static long GetNextId()
        {
            var result = Interlocked.Read(ref lastEventId);
            Interlocked.Increment(ref lastEventId);
            return result;
        }

        public object Target { get; set; }
        public string PropertyName { get; set; }
        public long EventId { get; set; }
        public DateTime EventDateTime { get; set; }
        public string Description { get; set; }
        public ProtocolEventType ProtocolEventType { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public string ReplicationKey { get; set; }
        public bool IsCommited { get; set; }

        public bool IsNotForProtocol
        {
            get
            {
                var result = false;
                if(Target != null)
                {
                    var modelClass = XafDeltaModule.XafApp.FindModelClass(Target.GetType());
                    if(modelClass != null)
                    {
                        var attr = (Attribute) modelClass.TypeInfo.FindAttribute<NotForProtocolAttribute>()
                                   ?? (Attribute) modelClass.TypeInfo.FindAttribute<IsLocalAttribute>();
                        result = attr != null;
                        if(!result && !string.IsNullOrEmpty(PropertyName))
                        {
                            var member = modelClass.AllMembers[PropertyName];
                            if (member != null)
                            {
                                attr = (Attribute)member.MemberInfo.FindAttribute<NotForProtocolAttribute>()
                                   ?? (Attribute)member.MemberInfo.FindAttribute<IsLocalAttribute>();
                                result = attr != null;
                            }
                        }
                    }
                }
                return result;
            }
        }
    }

    /// <summary>
    /// Protocol event type
    /// </summary>
    internal enum ProtocolEventType
    {
        /// <summary>
        /// New object created
        /// </summary>
        ObjectCreated = 1,
        /// <summary>
        /// Initial value assigned to property
        /// </summary>
        InitialValueAssigned = 2,
        /// <summary>
        /// Property value is changed
        /// </summary>
        ObjectChanged = 3,
        /// <summary>
        /// Object deleted
        /// </summary>
        ObjectDeleted = 4,
        /// <summary>
        /// Object added to collection
        /// </summary>
        AddedToCollection = 5,
        /// <summary>
        /// Object removed from collection
        /// </summary>
        RemovedFromCollection = 6,
        /// <summary>
        /// Collection object modified
        /// </summary>
        CollectionObjectChanged = 7,
        /// <summary>
        /// Aggregated object modified
        /// </summary>
        AggregatedObjectChanged = 8,
        /// <summary>
        /// Custom event
        /// </summary>
        CustomData = 9,
        /// <summary>
        /// Session commited
        /// </summary>
        CommitSession = 10,
        /// <summary>
        /// Specified method call
        /// </summary>
        MethodCall = 11
    }
}