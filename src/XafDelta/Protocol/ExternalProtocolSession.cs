using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using XafDelta.Messaging;

namespace XafDelta.Protocol
{
    /// <summary>
    /// External protocol session is protocol session loaded from different replication node.
    /// </summary>
    [IsLocal]
    public sealed class ExternalProtocolSession : ProtocolSession
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalProtocolSession"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public ExternalProtocolSession(Session session) : base(session)
        {
        }

        /// <summary>
        /// Gets or sets the audit session route - the list of replication nodes passed by session.
        /// </summary>
        /// <value>
        /// The list of replication nodes passed by session.
        /// </value>
        [Size(SizeAttribute.Unlimited)]
        public string Route
        {
            get { return GetPropertyValue<string>("Route"); }
            set { SetPropertyValue("Route", value); }
        }


        /// <summary>
        /// Creates for package session.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="packageSession">The package session.</param>
        /// <returns>New external protocol session</returns>
        public static ExternalProtocolSession CreateForPackageSession(IObjectSpace objectSpace, PackageSession packageSession)
        {
            var result = objectSpace.CreateObject<ExternalProtocolSession>();
            result.SessionId = packageSession.SessionId;
            result.Route = packageSession.Route;
            result.StartedOn = packageSession.StartedOn;
            return result;
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="packageSession">The package session.</param>
        /// <returns>New external protocol session</returns>
        public static ExternalProtocolSession GetSession(IObjectSpace objectSpace, PackageSession packageSession)
        {
            var result =
                objectSpace.FindObject<ExternalProtocolSession>(
                    CriteriaOperator.Parse("SessionId = ?", packageSession.SessionId), true);
            if(result == null)
            {
                result = objectSpace.CreateObject<ExternalProtocolSession>();
                result.SessionId = packageSession.SessionId;
                result.Route = packageSession.Route;
                result.StartedOn = packageSession.StartedOn;
                result.CommitedOn = packageSession.CommitedOn;
                if (packageSession.Parent != null)
                    result.Parent = GetSession(objectSpace, packageSession.Parent);
            }
            return result;
        }
    }

}
