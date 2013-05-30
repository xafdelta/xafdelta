using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using XafDelta.Protocol;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Package session. Protocol session data container in package.
    /// For internal use only.
    /// </summary>
    [IsLocal]
    public class PackageSession : XPObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageSession"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public PackageSession(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Creates PackageSession for protocol session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="protocolSession">The protocol session.</param>
        /// <param name="nodeId"></param>
        /// <returns>Package session</returns>
        public static PackageSession CreateForProtocolSession(Session session, ProtocolSession protocolSession, string nodeId)
        {
            var result = session.FindObject<PackageSession>(PersistentCriteriaEvaluationBehavior.InTransaction, 
                CriteriaOperator.Parse("SessionId = ?", protocolSession.SessionId));

            if (result == null)
            {

                result = new PackageSession(session)
                             {
                                 SessionId = protocolSession.SessionId,
                                 StartedOn = protocolSession.StartedOn,
                                 CommitedOn = protocolSession.CommitedOn
                             };

                if (protocolSession is ExternalProtocolSession)
                    result.Route = ((ExternalProtocolSession) protocolSession).Route;

                result.Route += '\n' + nodeId;

                if (protocolSession.Parent != null)
                    result.Parent = CreateForProtocolSession(session, protocolSession.Parent, nodeId);
            }

            return result;
        }

        /// <summary>
        /// Persists the current object.
        /// </summary>
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            Route = "";
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public PackageSession Parent
        {
            get { return GetPropertyValue<PackageSession>("Parent"); }
            set { SetPropertyValue("Parent", value); }
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
        /// Gets or sets the route.
        /// </summary>
        /// <value>
        /// The route.
        /// </value>
        [Size(SizeAttribute.Unlimited)]
        public string Route
        {
            get { return GetPropertyValue<string>("Route"); }
            set { SetPropertyValue("Route", value); }
        }

        /// <summary>
        /// Gets or sets the UTC offset.
        /// </summary>
        /// <value>
        /// The UTC offset.
        /// </value>
        public TimeSpan UtcOffset
        {
            get { return GetPropertyValue<TimeSpan>("UtcOffset"); }
            set { SetPropertyValue("UtcOffset", value); }
        }

        /// <summary>
        /// Gets or sets session start date and time.
        /// </summary>
        /// <value>
        /// Session start date and time.
        /// </value>
        [Custom("DisplayFormat", "{0:o}")]
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
        [Custom("DisplayFormat", "{0:o}")]
        public DateTime CommitedOn
        {
            get { return GetPropertyValue<DateTime>("CommitedOn"); }
            set { SetPropertyValue("CommitedOn", value); }
        }
       

        /// <summary>
        /// Gets all children sessions.
        /// </summary>
        public IEnumerable<PackageSession> AllChildren
        {
            get
            {
                var result = new List<PackageSession>();
                var children = (from c in new XPCollection<PackageSession>(PersistentCriteriaEvaluationBehavior.InTransaction,
                    Session, CriteriaOperator.Parse("Not IsNull(Parent) And Parent.SessionId = ?", SessionId)) select c).ToList();
                foreach (var child in children)
                    result.AddRange(child.AllChildren);
                result.Add(this);
                return result;
            }
        }

        /// <summary>
        /// Gets the package records.
        /// </summary>
        [Aggregated]
        [Association("PackageSessionRecords")]
        public XPCollection<PackageRecord> PackageRecords { get { return GetCollection<PackageRecord>("PackageRecords"); } }
    }

}
