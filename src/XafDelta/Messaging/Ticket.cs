using System;
using System.IO;
using System.Xml;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Ticket is notification about package state changes. 
    /// Tickets transmitted from package recipient (ticket event invocator) to package sender database.
    /// Stored in replication storage database.
    /// For internal use only.
    /// </summary>
    [DefaultClassOptions]
    [ImageName("Ticket")]
    [CreatableItem(false)]
    [IsLocal]
    public sealed class Ticket : XPObject, IReplicationMessage
    {
        /// <summary>
        /// Ticket file extension used for export and import.
        /// </summary>
        public static readonly string TicketFileExtension = ".xdt";

        /// <summary>
        /// Initializes a new instance of the <see cref="Ticket"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public Ticket(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Persists the current object.
        /// </summary>
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            TicketDateTime = DateTime.UtcNow;
            UserName = SecuritySystem.CurrentUserName;
            TicketNodeId = XafDeltaModule.Instance.CurrentNodeId;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return FileName;
        }

        /// <summary>
        /// Gets or sets the package.
        /// </summary>
        /// <value>
        /// The package.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        [Association("PackageTickets")]
        public Package Package
        {
            get { return GetPropertyValue<Package>("Package"); }
            set { SetPropertyValue("Package", value); }
        }

        /// <summary>
        /// Gets or sets the ticket node id.
        /// </summary>
        /// <value>
        /// The ticket node id.
        /// </value>
        [Size(255)]
        public string TicketNodeId
        {
            get { return GetPropertyValue<string>("TicketNodeId"); }
            set { SetPropertyValue("TicketNodeId", value); }
        }

        /// <summary>
        /// Gets or sets the type of the ticket.
        /// </summary>
        /// <value>
        /// The type of the ticket.
        /// </value>
        public PackageEventType PackageEventType
        {
            get { return GetPropertyValue<PackageEventType>("PackageEventType"); }
            set { SetPropertyValue("PackageEventType", value); }
        }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        [Size(255)]
        public string UserName
        {
            get { return GetPropertyValue<string>("UserName"); }
            set { SetPropertyValue("UserName", value); }
        }

        /// <summary>
        /// Gets or sets the ticket date time.
        /// </summary>
        /// <value>
        /// The ticket date time.
        /// </value>
        [Custom("DisplayFormat", "{0:o}")]
        public DateTime TicketDateTime
        {
            get { return GetPropertyValue<DateTime>("TicketDateTime"); }
            set { SetPropertyValue("TicketDateTime", value); }
        }
       

        /// <summary>
        /// Gets or sets the ticket processing (import or send) date time.
        /// </summary>
        /// <value>
        /// The processing date time.
        /// </value>
        [Custom("DisplayFormat", "{0:o}")]
        public DateTime ProcessingDateTime
        {
            get { return GetPropertyValue<DateTime>("ProcessingDateTime"); }
            set { SetPropertyValue("ProcessingDateTime", value); }
        }
        

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>
        /// The comments.
        /// </value>
        [Size(SizeAttribute.Unlimited)]
        public string Comments
        {
            get { return GetPropertyValue<string>("Comments"); }
            set { SetPropertyValue("Comments", value); }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        public string FileName
        {
            get 
            { 
                var result = "";
                if (Package != null)
                    result = Package.FileName + TicketFileExtension;
                return result;
            }
        }

        /// <summary>
        /// Gets the recipient address for upload.
        /// </summary>
        public string RecipientAddress
        {
            get 
            {
                var result = "";
                if (Package != null && !string.IsNullOrEmpty(Package.SenderNodeId))
                {
                    var recipientNode = ReplicationNode.FindNode(Session, Package.SenderNodeId);
                    if (recipientNode != null)
                        result = recipientNode.TransportAddress;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the ticket binary data.
        /// </summary>
        /// <returns>Ticket binary data</returns>
        public byte[] GetData()
        {
            return ExportTicket();
        }

        /// <summary>
        /// Gets the file mask.
        /// </summary>
        public static string FileMask
        {
            get { return Package.FileMask + @"\" + TicketFileExtension; }
        }

        #region Export and Import

        /// <summary>
        /// Exports the ticket.
        /// </summary>
        /// <returns>Exported binary data</returns>
        public byte[] ExportTicket()
        {
            byte[] result = null;
            if (Package != null)
            {
                using (var ms = new MemoryStream())
                using (var xml = XmlWriter.Create(ms))
                {
                    xml.WriteStartDocument();
                    try
                    {
                        xml.WriteStartElement("Ticket");

                        xml.WriteElementString("TicketDateTime", TicketDateTime.ToUniversalTime().ToString("R"));
                        xml.WriteElementString("TicketNodeId", TicketNodeId);
                        xml.WriteElementString("EventType", PackageEventType.ToString());
                        xml.WriteElementString("PackageFileName", Package.FileName);
                        xml.WriteElementString("Comments", Comments);
                        xml.WriteElementString("UserName", UserName);

                        xml.WriteEndElement();

                        // if export is send then assume export moment is ProcessingDateTime
                        if (XafDeltaModule.Instance.ExportIsSend && ProcessingDateTime == DateTime.MinValue)
                            ProcessingDateTime = DateTime.UtcNow;
                    }
                    finally
                    {
                        xml.WriteEndDocument();
                        xml.Close();
                        ms.Close();
                        result = ms.ToArray();
                        result = XafDeltaModule.Instance.PackService.PackBytes(result, "");
                    }
                }
            }
            return result;
        }

        
       

        /// <summary>
        /// Imports the ticket.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="source">The source.</param>
        /// <returns>Imported ticket</returns>
        public static Ticket ImportTicket(IObjectSpace objectSpace, byte[] source)
        {
            Ticket result = null;
            if (source != null && source.Length > 0)
            {
                source = XafDeltaModule.Instance.PackService.UnpackBytes(source, "");
                using (var ms = new MemoryStream(source))
                using (var xml = XmlReader.Create(ms))
                {
                    xml.MoveToContent();
                    try
                    {
                        xml.ReadStartElement();

                        var ticketDateTime = DateTime.ParseExact(xml.ReadElementString("TicketDateTime"), "R", null).ToLocalTime();

                        var ticketNodeId = xml.ReadElementString("TicketNodeId");
                        var eventType =
                            (PackageEventType) Enum.Parse(typeof (PackageEventType), xml.ReadElementString("EventType"));
                        var packageFileName = xml.ReadElementString("PackageFileName");
                        var comments = xml.ReadElementString("Comments");
                        var userName = xml.ReadElementString("UserName");

                        xml.ReadEndElement();

                        var package =
                            objectSpace.FindObject<Package>(CriteriaOperator.Parse("FileName = ?", packageFileName));
                        if (package != null)
                        {
                            result = objectSpace.CreateObject<Ticket>();
                            result.Package = package;
                            result.Comments = comments;
                            result.TicketDateTime = ticketDateTime;
                            result.TicketNodeId = ticketNodeId;
                            result.PackageEventType = eventType;
                            result.UserName = userName;
                            result.ProcessingDateTime = DateTime.UtcNow;
                        }
                    }
                    finally
                    {
                        xml.Close();
                        ms.Close();
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Imports the file.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileBytes">The file bytes.</param>
        public static void ImportFile(IObjectSpace objectSpace, string fileName, byte[] fileBytes)
        {
            if (Path.GetExtension(fileName) == TicketFileExtension)
            {
                ImportTicket(objectSpace, fileBytes);
            }
        }

       

        #endregion

        /// <summary>
        /// Creates ticket for log record.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="logRecord">The log record.</param>
        /// <param name="package">The package.</param>
        /// <returns>New ticket</returns>
        public static Ticket CreateForLogRecord(IObjectSpace objectSpace, 
            PackageLogRecord logRecord, Package package)
        {
            var result = objectSpace.CreateObject<Ticket>();
            result.PackageEventType = logRecord.PackageEventType;
            result.Package = package;
            result.Comments = logRecord.Comments;
            return result;
        }
    }
}
