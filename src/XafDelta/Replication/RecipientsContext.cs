using DevExpress.Xpo;

namespace XafDelta.Replication
{
    /// <summary>
    /// Selection context for recipients selection.
    /// For internal use only.
    /// </summary>
    /// <typeparam name="T">Persistent object type</typeparam>
    [NonPersistent]
    [IsLocal]
    public sealed class RecipientsContext<T> : RecipientsContextBase where T: class 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecipientsContext&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public RecipientsContext(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Gets the event object.
        /// </summary>
        public T TargetObject
        {
            get { return ProtocolRecord.AuditedObject == null ? null : (T)ProtocolRecord.AuditedObject.Target; }
        }
    }
}