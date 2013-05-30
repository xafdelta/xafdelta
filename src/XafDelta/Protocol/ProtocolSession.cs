using System;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;

namespace XafDelta.Protocol
{
    /// <summary>
    /// Protocol session. Contains commited session's data.
    /// </summary>
    [ImageName("ProtocolSession")]
    [IsLocal]
    public class ProtocolSession : XPObject
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolSession"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public ProtocolSession(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Persists the current object.
        /// </summary>
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            CommitedOn = DateTime.UtcNow;
            SessionId = Guid.NewGuid();
        }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        /// <value>
        /// The session id.
        /// </value>
        public Guid SessionId
        {
            get { return GetPropertyValue<Guid>("SessionId"); }
            set { SetPropertyValue("SessionId", value); }
        }

        /// <summary>
        /// Gets or sets the parent protocol session.
        /// </summary>
        /// <value>
        /// The parent protocol session.
        /// </value>
        public ProtocolSession Parent
        {
            get { return GetPropertyValue<ProtocolSession>("Parent"); }
            set { SetPropertyValue("Parent", value); }
        }

        /// <summary>
        /// Gets or sets session start date and time.
        /// </summary>
        /// <value>
        /// Session start date and time.
        /// </value>
        [Custom("DisplayFormat", "{0:dd/MM/yy hh:mm:ss zzz}")]
        public DateTime StartedOn
        {
            get { return GetPropertyValue<DateTime>("StartedOn"); }
            set { SetPropertyValue("StartedOn", value); }
        }

        /// <summary>
        /// Gets or sets session commit date and time.
        /// </summary>
        /// <value>
        /// Session commit date and time.
        /// </value>
        [Custom("DisplayFormat", "{0:G zzz}")]
        public DateTime CommitedOn
        {
            get { return GetPropertyValue<DateTime>("CommitedOn"); }
            set { SetPropertyValue("CommitedOn", value); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether session is included in packages.
        /// </summary>
        /// <value>
        ///   <c>true</c> if session is saved; otherwise, <c>false</c>.
        /// </value>
        public bool SessionIsSaved
        {
            get { return GetPropertyValue<bool>("SessionIsSaved"); }
            set { SetPropertyValue("SessionIsSaved", value); }
        }

        /// <summary>
        /// Gets the protocol records owned by current protocol session.
        /// </summary>
        [Aggregated]
        [Association("ProtocolRecords")]
        public XPCollection<ProtocolRecord> ProtocolRecords { get { return GetCollection<ProtocolRecord>("ProtocolRecords"); } }

        /// <summary>
        /// Gets a value indicating whether this session is external (loaded from another node).
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is external; otherwise, <c>false</c>.
        /// </value>
        [VisibleInListView(false), VisibleInDetailView(false)]
        public bool IsExternal { get { return this is ExternalProtocolSession; } }
    }
}