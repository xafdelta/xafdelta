using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using XafDelta.Exceptions;
using XafDelta.Localization;

namespace XafDelta.Messaging
{
    /// <summary>
    /// Package is replication message, directed from one to one or more recipient replication nodes.
    /// Stored in replication storage database.
    /// For internal use only.
    /// </summary>
    [DefaultClassOptions]
    [ImageName("XafDelta")]
    [CreatableItem(false)]
    [IsLocal]
    public sealed class Package : XPObject, IReplicationMessage
    {
        /// <summary>
        /// FieldSeparator
        /// </summary>
        public static readonly string FieldSeparator = "-";
        /// <summary>
        /// PackageFileExtension
        /// </summary>
        public static readonly string PackageFileExtension = ".xad";

        internal static readonly PackageEventType[] NodeEventTypes =
            new[]
                {
                    PackageEventType.Loaded, 
                    PackageEventType.Rejected
                };
        internal static readonly PackageEventType[] TicketEventTypes =
            new[]
                {
                    PackageEventType.Imported,
                    PackageEventType.Loaded, 
                    PackageEventType.Rejected
                };
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Package"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public Package(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Called when saving.
        /// </summary>
        protected override void OnSaving()
        {
            // save data to PackageData property
            CloseUnitOfWork(true);
            base.OnSaving();
        }

        /// <summary>
        /// Persists the current object.
        /// </summary>
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            PackageDateTime = DateTime.UtcNow;
            UserName = SecuritySystem.CurrentUserName;
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
        /// Gets or sets the application name.
        /// </summary>
        /// <value>
        /// The application name.
        /// </value>
        public string ApplicationName
        {
            get { return GetPropertyValue<string>("ApplicationName"); }
            set { SetPropertyValue("ApplicationName", value); }
        }

        /// <summary>
        /// Gets or sets the sender node id.
        /// </summary>
        /// <value>
        /// The sender node id.
        /// </value>
        [Size(255)]
        [RuleRequiredField("", DefaultContexts.Save)]
        public string SenderNodeId
        {
            get { return GetPropertyValue<string>("SenderNodeId"); }
            set { SetPropertyValue("SenderNodeId", value); }
        }

        /// <summary>
        /// Gets or sets the recipient node id. "AllNodes" for broadcast packages.
        /// </summary>
        /// <value>
        /// The recipient node id.
        /// </value>
        [Size(255)]
        [RuleRequiredField("", DefaultContexts.Save)]
        public string RecipientNodeId
        {
            get { return GetPropertyValue<string>("RecipientNodeId"); }
            set { SetPropertyValue("RecipientNodeId", value); }
        }

        /// <summary>
        /// Gets or sets the package id.
        /// </summary>
        /// <value>
        /// The package id.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        public int PackageId
        {
            get { return GetPropertyValue<int>("PackageId"); }
            set { SetPropertyValue("PackageId", value); }
        }

        /// <summary>
        /// Gets or sets the type of the package.
        /// </summary>
        /// <value>
        /// The type of the package.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        public PackageType PackageType
        {
            get { return GetPropertyValue<PackageType>("PackageType"); }
            set { SetPropertyValue("PackageType", value); }
        }

        /// <summary>
        /// Gets or sets the creation date time.
        /// </summary>
        /// <value>
        /// The creation date time.
        /// </value>
        [Custom("DisplayFormat", "{0:o}")]
        public DateTime PackageDateTime
        {
            get { return GetPropertyValue<DateTime>("PackageDateTime"); }
            set { SetPropertyValue("PackageDateTime", value); }
        }
        

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        [RuleRequiredField("", DefaultContexts.Save)]
        public string UserName
        {
            get { return GetPropertyValue<string>("UserName"); }
            set { SetPropertyValue("UserName", value); }
        }

        /// <summary>
        /// Gets or sets the package data.
        /// </summary>
        /// <value>
        /// The package data.
        /// </value>
        [Delayed]
        [ValueConverter(typeof(CompressionConverter))]
        public byte[] PackageData
        {
            get { return GetPropertyValue<byte[]>("PackageData"); }
            set { SetPropertyValue("PackageData", value); }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        [Persistent]
        public string FileName
        {
            get
            {
                return string.Join(FieldSeparator, new[] {ApplicationName, SenderNodeId,
                    RecipientNodeId, PackageId.ToString("X8"), PackageType.ToString()}) + PackageFileExtension;
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
                if(!string.IsNullOrEmpty(RecipientNodeId))
                {
                    var recipientNode = ReplicationNode.FindNode(Session, SenderNodeId);
                    if (recipientNode != null)
                        result = recipientNode.TransportAddress;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the package binary data.
        /// </summary>
        /// <returns>Package binary data</returns>
        public byte[] GetData()
        {
            return ExportToBytes();
        }

        /// <summary>
        /// Gets the loaded date time.
        /// </summary>
        [Custom("DisplayFormat", "{0:o}")]
        public DateTime LoadedDateTime { get { return GetEventDateTime(PackageEventType.Loaded); } }
        

        /// <summary>
        /// Gets a value indicating whether this instance is input package for current node.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is input package; otherwise, <c>false</c>.
        /// </value>
        [VisibleInListView(false), VisibleInDetailView(false)]
        public bool IsInput
        {
            get
            {
                return SenderNodeId != XafDeltaModule.Instance.CurrentNodeId
                       &&
                       (RecipientNodeId == XafDeltaModule.Instance.CurrentNodeId ||
                        RecipientNodeId == ReplicationNode.AllNodes);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is output package for current node.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is output package; otherwise, <c>false</c>.
        /// </value>
        [VisibleInListView(false), VisibleInDetailView(false)]
        public bool IsOutput
        {
            get { return SenderNodeId == XafDeltaModule.Instance.CurrentNodeId; }
        }


        /// <summary>
        /// Gets the state of the external.
        /// </summary>
        /// <value>
        /// The state of the external.
        /// </value>
        public PackageEventType ExternalState
        {
            get
            {
                var result = PackageEventType.Created;
                var lastTicket = (from t in ExternalTickets orderby t.TicketDateTime descending select t).FirstOrDefault();
                if(lastTicket != null)
                    result = lastTicket.PackageEventType;
                return result;
            }
        }

        /// <summary>
        /// Gets the state of the local.
        /// </summary>
        /// <value>
        /// The state of the local.
        /// </value>
        public PackageEventType LocalState
        {
            get
            {
                var result = PackageEventType.Created;
                var logRecord =
                    (from t in LogRecords orderby t.EventDateTime descending select t).FirstOrDefault();
                if (logRecord != null)
                    result = logRecord.PackageEventType;
                return result;
            }
        }

        /// <summary>
        /// Gets the event date time.
        /// </summary>
        /// <param name="packageEventType">Type of the event.</param>
        /// <returns>Event date and time</returns>
        public DateTime GetEventDateTime(PackageEventType packageEventType)
        {
            return GetEventDateTime(new[] {packageEventType});
        }

        /// <summary>
        /// Gets the event date time.
        /// </summary>
        /// <param name="packageEventTypes">The event types.</param>
        /// <returns>Event date and time</returns>
        public DateTime GetEventDateTime(PackageEventType[] packageEventTypes)
        {
            var result = DateTime.MinValue;
            var logRecord = (from c in LogRecords where packageEventTypes.Contains(c.PackageEventType) select c).FirstOrDefault();
            if (logRecord != null)
                result = logRecord.EventDateTime;
            return result;
        }

        /// <summary>
        /// Gets the package tickets.
        /// </summary>
        [Aggregated]
        [Association("PackageTickets")]
        public XPCollection<Ticket> PackageTickets { get { return GetCollection<Ticket>("PackageTickets"); } }

        /// <summary>
        /// Gets the external tickets.
        /// </summary>
        [VisibleInListView(false), VisibleInDetailView(false)]
        public ReadOnlyCollection<Ticket> ExternalTickets
        {
            get
            {
                return new ReadOnlyCollection<Ticket>((from c in PackageTickets 
                       where c.TicketNodeId != XafDeltaModule.Instance.CurrentNodeId select c).ToList());
            }
        }

        private XPCollection<PackageLogRecord> logRecords;
        /// <summary>
        /// Gets the log records.
        /// </summary>
        public ReadOnlyCollection<PackageLogRecord> LogRecords
        {
            get
            {
                if(logRecords == null)
                {
                    logRecords = new XPCollection<PackageLogRecord>(PersistentCriteriaEvaluationBehavior.InTransaction,
                        Session, CriteriaOperator.Parse("FileName = ?", FileName));
                }
                return new ReadOnlyCollection<PackageLogRecord>(logRecords);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is broadcast package.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is broadcast package; otherwise, <c>false</c>.
        /// </value>
        [VisibleInListView(false), VisibleInDetailView(false)]
        public bool IsBroadcast { get { return RecipientNodeId == ReplicationNode.AllNodes; } }

        /// <summary>
        /// Parses the specified fileName and create empty package.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Empty package</returns>
        public static Package Parse(IObjectSpace objectSpace, string fileName)
        {
            if (objectSpace == null) throw new ArgumentNullException("objectSpace");
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException(Localizer.FileNameIsEmpty, "fileName");

            var fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
            while(!string.IsNullOrEmpty(Path.GetExtension(fileNameOnly)))
                fileNameOnly = Path.GetFileNameWithoutExtension(fileNameOnly);

            Debug.Assert(fileNameOnly != null, "fileNameOnly != null");

            var fieldValues = fileNameOnly.Split(new[] {FieldSeparator}, StringSplitOptions.None);
            if (fieldValues.Length != 5) throw new ArgumentException(Localizer.InvalidPackageFileName, "fileName");

            var result = objectSpace.CreateObject<Package>();
            result.ApplicationName = fieldValues[0];
            result.SenderNodeId = fieldValues[1];
            result.RecipientNodeId = fieldValues[2];
            result.PackageId = int.Parse(fieldValues[3], NumberStyles.HexNumber);
            result.PackageType = (PackageType) Enum.Parse(typeof (PackageType), fieldValues[4]);
            return result;
        }

        /// <summary>
        /// Determines whether the specified file name is package file name.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        ///   <c>true</c> if the specified file name is package file name; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsPackageFileName(string fileName)
        {
            var result = true;

            if (Path.GetExtension(fileName) != PackageFileExtension)
                result = false;

            var fileNameOnly = Path.GetFileNameWithoutExtension(fileName);

            Debug.Assert(fileNameOnly != null, "fileNameOnly != null");
            var fieldValues = fileNameOnly.Split(new[] { FieldSeparator }, StringSplitOptions.None);
            if (fieldValues.Length != 5)
                result = false;
            return result;
        }

        #region UnitOfWork

        private string tempFileName;
        private UnitOfWork unitOfWork;

        /// <summary>
        /// Gets the unit of work.
        /// </summary>
        [VisibleInDetailView(false), VisibleInListView(false)]
        internal UnitOfWork UnitOfWork
        {
            get { return unitOfWork ?? (unitOfWork = createUnitOfWork()); }
        }

        /// <summary>
        /// Gets the package file mask.
        /// </summary>
        [VisibleInDetailView(false), VisibleInListView(false)]
        public static string FileMask
        {
            get
            {
                var sep = @"\" + FieldSeparator;
                var result = XafDeltaModule.Instance.ApplicationName + sep
                             + @"[^" + sep + "]+" + sep
                             + @"(" + XafDeltaModule.Instance.CurrentNodeId + @"|" + ReplicationNode.AllNodes + @")" + sep
                             + @"[0-9A-Fa-f]{8}" + sep
                             + @"(" + PackageType.Snapshot + @"|" + PackageType.Protocol + ")"
                             + @"\" + PackageFileExtension;

                return result;
            }
        }

        /// <summary>
        /// Creates the unit of work for package data.
        /// </summary>
        /// <returns>Unit of work</returns>
        private UnitOfWork createUnitOfWork()
        {
            tempFileName = Path.GetTempFileName();
            if (PackageData != null)
                File.WriteAllBytes(tempFileName, PackageData);
            else if (File.Exists(tempFileName))
                File.Delete(tempFileName);

            var connStr = AccessConnectionProvider.GetConnectionString(tempFileName);

            // create unit of work based on temporary ms access database
            IDisposable[] disposables;
            var dataStore = XpoDefault.GetConnectionProvider(connStr, AutoCreateOption.DatabaseAndSchema, 
                out disposables);
            var dataLayer = new SimpleDataLayer(XafTypesInfo.XpoTypeInfoSource.XPDictionary, dataStore);
            var result = new UnitOfWork(dataLayer, disposables);

            string errorText;
            if(!verifyMarker(result, out errorText))
            {
                result.RollbackTransaction();
                result.Dispose();
                throw new InvalidPackageDataException(errorText);
            }
            // mark object changed
            if (PackageData == null)
                OnChanged();
            return result;
        }


        /// <summary>
        /// Verifies the marker.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="errorText">The error text.</param>
        /// <returns><c>true</c> if marker is valid; otherwise <c>false</c></returns>
        private bool verifyMarker(Session session, out string errorText)
        {
            // create new marker
            var marker = PackageMarker.GetInstance(session, this);

            var exceptionText = "";
            var propNames = new[] {"ApplicationName", "SenderNodeId", "RecipientNodeId", "PackageId"};
            propNames.ToList().ForEach(x => exceptionText += buildErrorText(x, marker));
            errorText = exceptionText;
            return string.IsNullOrEmpty(errorText);
        }

        /// <summary>
        /// Builds the error text.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="marker">The marker.</param>
        /// <returns>Error text</returns>
        private string buildErrorText(string propertyName, PackageMarker marker)
        {
            var actual = GetType().GetProperty(propertyName).GetValue(this, null);
            var expected = marker.GetType().GetProperty(propertyName).GetValue(marker, null);
            var result = (expected.Equals(actual) ? ""
                : string.Format(Localizer.InvalidPackageProp, actual, expected, propertyName));
            return result;
        }

        /// <summary>
        /// Closes the unit of work.
        /// </summary>
        /// <param name="saveChanges">if set to <c>true</c> [save changes].</param>
        public void CloseUnitOfWork(bool saveChanges)
        {
            if (unitOfWork == null || !File.Exists(tempFileName)) return;

            if (saveChanges)
            {
                unitOfWork.CommitChanges();
                unitOfWork.Dispose();
                var allBytes = File.ReadAllBytes(tempFileName);
                if (allBytes.Length > 0) PackageData = allBytes;
            }
            else
            {
                unitOfWork.RollbackTransaction();
                unitOfWork.Dispose();
            }

            File.Delete(tempFileName);
            tempFileName = null;
            unitOfWork = null;
        }

        #endregion

        #region Import and Export

        /// <summary>
        /// Loads from file.
        /// </summary>
        /// <param name="objectSpace">The package storage object space.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>Imported package</returns>
        public static Package ImportFromFile(IObjectSpace objectSpace, string fileName)
        {
            var result = Parse(objectSpace, fileName);
            if (File.Exists(fileName))
                result = ImportFromBytes(objectSpace, fileName, File.ReadAllBytes(fileName));
            return result;
        }

        /// <summary>
        /// Imports from bytes.
        /// </summary>
        /// <param name="objectSpace">The package storage object space.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">The data.</param>
        /// <returns>Imported package</returns>
        public static Package ImportFromBytes(IObjectSpace objectSpace, string fileName, byte[] data)
        {
            var result = Parse(objectSpace, fileName);
            var packer = XafDeltaModule.Instance.PackService;
            result.PackageData = packer.UnpackBytes(data, result.SenderNodeId);

            // restore package attributes from marker
            using(result.UnitOfWork)
            {
                var marker = PackageMarker.GetInstance(result.unitOfWork, result);
                if(marker != null)
                {
                    result.PackageDateTime = marker.PackageDateTime;
                    result.UserName = marker.UserName;
                }
                result.CloseUnitOfWork(false);
            }
            return result;
        }

        /// <summary>
        /// Imports from bytes.
        /// </summary>
        /// <param name="objectSpace">The package storage object space.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">The data.</param>
        /// <returns>Imported package</returns>
        public static Package ImportFromMdbBytes(IObjectSpace objectSpace, string fileName, byte[] data)
        {
            var result = Parse(objectSpace, fileName);
            result.PackageData = data;

            // restore package attributes from marker
            using (result.UnitOfWork)
            {
                var marker = PackageMarker.GetInstance(result.unitOfWork, result);
                if (marker != null)
                {
                    result.PackageDateTime = marker.PackageDateTime;
                    result.UserName = marker.UserName;
                }
                result.CloseUnitOfWork(false);
            }
            return result;
        }

        /// <summary>
        /// Save package to file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void ExportToFile(string fileName)
        {
            var packer = XafDeltaModule.Instance.PackService;
            File.WriteAllBytes(fileName, packer.PackBytes(PackageData, RecipientNodeId));
        }

        /// <summary>
        /// Exports to bytes.
        /// </summary>
        /// <returns>Transport message bytes</returns>
        public byte[] ExportToBytes()
        {
            var packer = XafDeltaModule.Instance.PackService;
            return packer.PackBytes(PackageData, RecipientNodeId);
        }


        /// <summary>
        /// Imports the file.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileBytes">The file bytes.</param>
        public static void ImportFile(IObjectSpace objectSpace, string fileName, byte[] fileBytes)
        {
            if (Path.GetExtension(fileName) == PackageFileExtension)
            {
                ImportFromBytes(objectSpace, Path.GetFileName(fileName), fileBytes).
                    CreateLogRecord(PackageEventType.Imported, fileName);
            }
        }

        #endregion

        /// <summary>
        /// Creates the log record.
        /// </summary>
        /// <param name="packageEventType">Type of the event.</param>
        /// <param name="comments">The comments.</param>
        public void CreateLogRecord(PackageEventType packageEventType, string comments)
        {
            if(IsInvalidated) return;
            var obs = ObjectSpace.FindObjectSpaceByObject(this);
            if (obs != null)
            {
                var nodeRecord = obs.CreateObject<PackageLogRecord>();
                nodeRecord.FileName = FileName;
                nodeRecord.PackageEventType = packageEventType;
                nodeRecord.Comments = comments;

                if (XafDeltaModule.Instance.UseTickets && TicketEventTypes.Contains(packageEventType))
                {
                    Ticket.CreateForLogRecord(obs, nodeRecord, this);
                }
            }
        }

        /// <summary>
        /// Creates the log record.
        /// </summary>
        /// <param name="packageEventType">Type of the event.</param>
        public void CreateLogRecord(PackageEventType packageEventType)
        {
            CreateLogRecord(packageEventType, "");
        }
    }


    /// <summary>
    /// Replication package type
    /// </summary>
    public enum PackageType
    {
        /// <summary>
        /// Package contains protocol records
        /// </summary>
        Protocol,
        /// <summary>
        /// Snapshot package
        /// </summary>
        Snapshot
    }
}