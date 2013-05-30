using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Package log record linked with specified node. Stores in node's database.
    /// </summary>
    [CreatableItem(false), IsLocal]
    public sealed class PackageLogRecord : BaseObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageLogRecord"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public PackageLogRecord(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Persists the current object.
        /// </summary>
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            EventDateTime = DateTime.UtcNow;
            UserName = SecuritySystem.CurrentUserName;
        }

        /// <summary>
        /// Gets or sets the name of package file.
        /// </summary>
        /// <value>
        /// The name of package file.
        /// </value>
        [Size(255)]
        public string FileName
        {
            get { return GetPropertyValue<string>("FileName"); }
            set { SetPropertyValue("FileName", value); }
        }

        /// <summary>
        /// Gets the event date time.
        /// </summary>
        [Custom("DisplayFormat", "{0:o}")]
        public DateTime EventDateTime
        {
            get { return GetPropertyValue<DateTime>("EventDateTime"); }
            set { SetPropertyValue("EventDateTime", value); }
        }
       
        /// <summary>
        /// Gets the event moment.
        /// </summary>
        public string EventMoment
        {
            get { return EventDateTime.ToString("G"); }
        }

        /// <summary>
        /// Gets the type of the event.
        /// </summary>
        /// <value>
        /// The type of the event.
        /// </value>
        public PackageEventType PackageEventType
        {
            get { return GetPropertyValue<PackageEventType>("PackageEventType"); }
            set { SetPropertyValue("PackageEventType", value); }
        }

        /// <summary>
        /// Gets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName
        {
            get { return GetPropertyValue<string>("UserName"); }
            set { SetPropertyValue("UserName", value); }
        }

        /// <summary>
        /// Gets the comments.
        /// </summary>
        [Size(SizeAttribute.Unlimited)]
        public string Comments
        {
            get { return GetPropertyValue<string>("Comments"); }
            set { SetPropertyValue("Comments", value); }
        }
    }


    /// <summary>
    /// Replication package event type
    /// </summary>
    public enum PackageEventType
    {
        /// <summary>
        /// Package exported
        /// </summary>
        Exported,
        /// <summary>
        /// Package imported
        /// </summary>
        Imported,
        /// <summary>
        /// Package loaded
        /// </summary>
        Loaded,
        /// <summary>
        /// Package rejected
        /// </summary>
        Rejected,
        /// <summary>
        /// Package created
        /// </summary>
        Created,
        /// <summary>
        /// Package sent
        /// </summary>
        Sent,
        /// <summary>
        /// Package loading started
        /// </summary>
        Loading,
        /// <summary>
        /// Package loading Failed
        /// </summary>
        Failed,
        /// <summary>
        /// Session Already Loaded
        /// </summary>
        SessionAlreadyLoaded
    }
}
