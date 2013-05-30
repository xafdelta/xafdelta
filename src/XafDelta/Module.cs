using System;
using System.ComponentModel;
using System.Drawing;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Utils;
using XafDelta.Localization;
using XafDelta.Messaging;
using XafDelta.Protocol;
using XafDelta.ReadOnlyParameters;
using XafDelta.Replication;
using XafDelta.Storage;

namespace XafDelta
{
    /// <summary>
    /// XafDeltaModule
    /// </summary>
    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [ToolboxItem(true)]
    [ToolboxTabName(XafAssemblyInfo.DXTabXafModules)]
    [ToolboxBitmap(typeof(XafDeltaModule), "Resources.Delta.ico")]
    [Description("Supplies asynchronous replication system. Allows register and propagate changes between XAF databases.")]
    public sealed partial class XafDeltaModule : ModuleBase
    {
        /// <summary>
        /// Initializes the <see cref="XafDeltaModule"/> class.
        /// </summary>
        static XafDeltaModule()
        {
            // register read only parameters
            ParametersFactory.RegisterParameter(new CurrentNodeIdParameter());
            ParametersFactory.RegisterParameter(new ApplicationNameParameter());
            ParametersFactory.RegisterParameter(new SnapshotNodeIdParameter());
            ParametersFactory.RegisterParameter(new SnapshotNodeParameter());

            CurrentNodeIdOperator.Register();
            ApplicationNameOperator.Register();
            SnapshotNodeIdOperator.Register();
            SnapshotNodeOperator.Register();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XafDeltaModule"/> class.
        /// </summary>
        public XafDeltaModule()
        {
            ModelDifferenceResourceName = "XafDelta.Model.DesignedDiffs";
            InitializeComponent();
            createServices();
            Instance = this;
            ResourcesExportedToModel.Add(typeof(Localizer));
        }

        /// <summary>
        /// Creates internal services.
        /// </summary>
        private void createServices()
        {
            LoadService = new LoadService(this);
            ProtocolService = new ProtocolService(this, LoadService);
            MessagingService = new MessagingService(this);
            ProtocolReplicationService = new ProtocolReplicationService(this);
            SnapshotService = new SnapshotService(this);
            PackService = new PackService(this);
        }

        /// <summary>
        /// Collects the object space changes.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        public void CollectObjectSpace(IObjectSpace objectSpace)
        {
            ProtocolService.Collector.CollectObjectSpace(objectSpace);
        }

        /// <summary>
        /// Gets the protocol service.
        /// </summary>
        internal ProtocolService ProtocolService { get; private set; }
        /// <summary>
        /// Gets the load service.
        /// </summary>
        internal LoadService LoadService { get; private set; }
        /// <summary>
        /// Gets the messaging service.
        /// </summary>
        internal MessagingService MessagingService { get; private set; }
        /// <summary>
        /// Gets the protocol replication service.
        /// </summary>
        internal ProtocolReplicationService ProtocolReplicationService { get; private set; }
        /// <summary>
        /// Gets the snapshot replication service.
        /// </summary>
        internal SnapshotService SnapshotService { get; private set; }
        /// <summary>
        /// Gets the pack service.
        /// </summary>
        internal PackService PackService { get; private set; }

        /// <summary>
        /// Gets the XafDeltaModule singleton.
        /// </summary>
        public static XafDeltaModule Instance { get; private set; }

        private string cachedCurrentNodeId;
        /// <summary>
        /// Gets the current replication node id.
        /// </summary>
        [Browsable(false)]
        public string CurrentNodeId
        {
            get
            {
                var result = cachedCurrentNodeId;
                if (result == null && Instance != null && !Instance.DesignMode)
                {
                    using (var objectSpace = XafApp.CreateObjectSpace())
                    {
                        result = ReplicationNode.GetCurrentNodeId(objectSpace);
                        cachedCurrentNodeId = result;
                        objectSpace.CommitChanges();
                    }
                }
                return result;
            }
        }
        /// <summary>
        /// Clears the current node id cache.
        /// </summary>
        public void ClearCurrentNodeIdCache()
        {
            cachedCurrentNodeId = null;
        }

        /// <summary>
        /// Extends the Application Model.
        /// </summary>
        /// <param name="extenders">A <b>ModelInterfaceExtenders</b> object representing a collection of Application Model interface extenders.</param>
        public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders)
        {
            base.ExtendModelInterfaces(extenders);
            ExtensionsHelper.ExtendModelInterfaces(extenders);
        }

        /// <summary>
        /// Sets up a module after it has been added to the <see cref="P:DevExpress.ExpressApp.XafApplication.Modules"/> collection.
        /// </summary>
        /// <param name="application">An <see cref="T:DevExpress.ExpressApp.XafApplication"/> object that provides methods and properties to manage the current application. This parameter value is set for the <see cref="P:DevExpress.ExpressApp.ModuleBase.Application"/> property.</param>
        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            // initialize services
            StorageService.Instance.Initialize(application, ReplicaStorageConnectionString, typeof(Package), typeof(Ticket));
            ProtocolService.Initialize(application, StorageService.Instance, LoadService);
            MessagingService.Initialize(application);
        }

        /// <summary>
        /// Gets the XAF application instance.
        /// </summary>
        public static XafApplication XafApp
        {
            get 
            { 
                XafApplication result = null;
                if (Instance != null) result = Instance.Application;
                if(result == null)
                {
                    var args = new CustomGetApplicationArgs();
                    OnCustomGetApplication(args);
                    result = args.Application;
                }
                return result;
            }
        }

        #region Options

        /// <summary>
        /// Gets or sets the replica storage XPO connection string. 
        /// If null or empty value then replicas stored in application's database.
        /// For additiona flexibility you can bind this property to application settings value.
        /// </summary>
        /// <value>
        /// The replica storage XPO connection string.
        /// </value>
        public string ReplicaStorageConnectionString { get; set; }

        private IModelReplicationNode modelReplicationNode
        {
            get
            {
                IModelReplicationNode result = null;
                if (XafApp != null && XafApp.Model != null)
                    /* 11.2.7 */
                    result = (IModelReplicationNode)XafApp.Model.GetNode("Replication");
                return result;
            }
        }

        /// <summary>
        /// Gets the type of the replication.
        /// </summary>
        /// <value>
        /// The type of the replication.
        /// </value>
        [Browsable(false)]
        public RoutingType RoutingType
        {
            get
            {
                return modelReplicationNode == null ? RoutingType.BroadcastRouting : modelReplicationNode.RoutingType;
            }
        }

        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        /// <value>
        /// The application identifier.
        /// </value>
        [Browsable(false)]
        public string ApplicationName
        {
            get { return XafApp.ApplicationName; }
        }

        /// <summary>
        /// Gets the type of the crypto algorithm for the transport message encryption. 
        /// </summary>
        /// <value>
        /// The type of the crypto algorithm for the transport message encryption.
        /// </value>
        [Browsable(false)]
        public CryptoAlgorithmType CryptoAlgorithmType
        {
            get { return modelReplicationNode == null ? CryptoAlgorithmType.RijndaelManaged : modelReplicationNode.CryptoAlgorithmType; }
        }

        /// <summary>
        /// Gets the password for encrypt/decrypt.
        /// </summary>
        /// <value>
        /// The password for encrypt/decrypt.
        /// </value>
        [Browsable(false)]
        public string Password
        {
            get { return modelReplicationNode == null ? "" : modelReplicationNode.Password; }
        }

        /// <summary>
        /// Gets a value indicating whether anonymous packages is granted to load.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if anonymous packages is granted to load; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool AnonymousPackagesAllowed
        {
            get { return modelReplicationNode != null && modelReplicationNode.AnonymousPackagesAllowed; }
        }

        /// <summary>
        /// Gets a value indicating whether external protocol records replication is enabled.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if external protocol records replication is enabled; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool ReplicateExternalData
        {
            get { return modelReplicationNode != null && modelReplicationNode.ReplicateExternalData; }
        }

        /// <summary>
        /// Gets a value indicating what XafDelta can use tickets.
        /// </summary>
        /// <value>
        ///   <c>true</c> if use tickets; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool UseTickets { get { return modelReplicationNode != null && modelReplicationNode.UseTickets; } }

        /// <summary>
        /// Gets a value indicating whether XafDelta should mark messages as sent after export.
        /// Recommended for 'manual' message delivery.
        /// </summary>
        /// <value>
        ///   <c>true</c> if export is send; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool ExportIsSend { get { return modelReplicationNode != null && modelReplicationNode.MarkExportedAsSent; } }

        /// <summary>
        /// Gets a value indicating whether should create external protocol records 
        /// in application database while package loading. 
        /// Set this propery in model to <c>false</c> to decrease protocol size.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if create external protocol records; otherwise, <c>false</c>.
        /// </value>
        [Browsable(false)]
        public bool CreateExternalProtocolRecords { get { return modelReplicationNode != null && modelReplicationNode.CreateExternalProtocolRecords; } }

        /// <summary>
        /// Gets the selector mode.
        /// </summary>
        public SelectorMode SelectorMode
        {
            get {  return modelReplicationNode != null ? modelReplicationNode.SelectorMode : SelectorMode.WhiteList; } 
        }

        #endregion

        #region Module events

        /// <summary>
        /// Occurs before pack message.
        /// </summary>
        [Description("Occurs before pack message.")]
        public event EventHandler BeforePack;
        internal void DoBeforePack(EventArgs e)
        {
            var handler = BeforePack;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after pack message.
        /// </summary>
        [Description("Occurs after pack message.")]
        public event EventHandler AfterPack;
        internal void DoAfterPack(EventArgs e)
        {
            var handler = AfterPack;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before unpack message.
        /// </summary>
        [Description(" Occurs before unpack message.")]
        public event EventHandler BeforeUnpack;
        internal void DoBeforeUnpack(EventArgs e)
        {
            var handler = BeforeUnpack;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after unpack message.
        /// </summary>
        [Description("Occurs after unpack message")]
        public event EventHandler AfterUnpack;
        internal void DoAfterUnpack(EventArgs e)
        {
            var handler = AfterUnpack;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before (or instead of) message encryption. Use this event to implement custom encryption 
        /// or setup standard algorithm with custom parameters. See also <see cref="CryptoEventArgs"/>, <see cref="BeforeDecrypt"/>.
        /// </summary>
        /// <seealso cref="CryptoEventArgs"/>
        /// <seealso cref="BeforeDecrypt"/>
        /// <example>
        /// 
        /// This sample shows how to use <see cref="DoBeforeEncrypt"/> event to specify custom encryption key.
        /// <code>
        /// private void xafDeltaModule1_BeforeEncrypt(object sender, XafDelta.Messaging.CryptoEventArgs e)
        ///{
        ///    e.Algoritm.Key = getCustomKey(e); 
        ///}
        ///
        ///private byte[] getCustomKey(CryptoEventArgs args)
        ///{
        ///    var hashArray = (new SHA512Managed()).ComputeHash(Encoding.UTF8.GetBytes("Secret word"));
        ///    return hashArray.Take(args.Algoritm.KeySize / 8).ToArray();
        ///}
        /// </code>
        /// 
        /// This sample shows how to use <see cref="DoBeforeEncrypt"/> event for implement simple XOR custom encryption.
        /// <code>
        /// private void xafDeltaModule1_BeforeEncrypt(object sender, XafDelta.Messaging.CryptoEventArgs e)
        /// {
        ///     var sourceBytes = e.SourceStream.AllBytes();
        ///     var encryptedBytes = sourceBytes.ToList().ConvertAll&lt;byte&gt;(customXor).ToArray();
        ///     using (var memStream = new MemoryStream(encryptedBytes))
        ///     {
        ///          memStream.CopyTo(e.DestStream);
        ///     }
        ///     e.Done = true;
        /// }
        /// 
        /// private byte customXor(byte input)
        /// {
        ///     var result = input ^ 0xAA;
        ///     return (byte)result;
        /// }
        /// </code>
        /// </example>
        [Description("Occurs before encrypt message")]
        public event EventHandler<CryptoEventArgs> BeforeEncrypt;
        internal void DoBeforeEncrypt(CryptoEventArgs e)
        {
            var handler = BeforeEncrypt;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after message encryption. See also <see cref="CryptoEventArgs"/>.
        /// </summary>
        /// <seealso cref="CryptoEventArgs"/>
        [Description("Occurs after encrypt message")]
        public event EventHandler<CryptoEventArgs> AfterEncrypt;
        internal void DoAfterEncrypt(CryptoEventArgs e)
        {
            var handler = AfterEncrypt;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before (or instead of) message compression. 
        /// Use this event for implement custom compression. See also <see cref="StreamEventArgs"/>, <see cref="BeforeDecompress"/>.
        /// </summary>
        /// <seealso cref="StreamEventArgs"/>
        /// <seealso cref="BeforeDecompress"/>
        /// <example>
        /// 
        /// This sample shows how to use <see cref="DoBeforeCompress"/> event to disable compression.
        /// <code>
        /// private void xafDeltaModule1_BeforeCompress(object sender, StreamEventArgs e)
        /// {
        ///     e.SourceStream.CopyTo(e.DestStream);
        ///     e.Done = true;
        /// }
        /// </code>  
        /// </example>
        [Description("Occurs before compress message")]
        public event EventHandler<StreamEventArgs> BeforeCompress;
        internal void DoBeforeCompress(StreamEventArgs e)
        {
            var handler = BeforeCompress;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after compress message.
        /// </summary>
        [Description("Occurs after compress message")]
        public event EventHandler<StreamEventArgs> AfterCompress;
        internal void DoAfterCompress(StreamEventArgs e)
        {
            var handler = AfterCompress;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before (or instead of) message decryption. Use this event for implement custom decryption 
        /// or setup standard algorithm with custom parameters. See also <see cref="CryptoEventArgs"/>, <see cref="AfterEncrypt"/>.
        /// </summary>
        /// <seealso cref="CryptoEventArgs"/>
        /// <seealso cref="AfterEncrypt"/>
        /// <example>
        /// 
        /// This sample shows how to use <see cref="BeforeDecrypt"/> event to define custom decryption key.
        /// <code>
        /// private void xafDeltaModule1_BeforeDecrypt(object sender, XafDelta.Messaging.CryptoEventArgs e)
        ///{
        ///    e.Algoritm.Key = getCustomKey(e); 
        ///}
        ///
        ///private byte[] getCustomKey(CryptoEventArgs args)
        ///{
        ///    var hashArray = (new SHA512Managed()).ComputeHash(Encoding.UTF8.GetBytes("Secret word"));
        ///    return hashArray.Take(args.Algoritm.KeySize / 8).ToArray();
        ///}
        /// </code>
        /// 
        /// This sample shows how to use <see cref="BeforeDecrypt"/> event for implement simple XOR custom decryption.
        /// <code>
        /// private void xafDeltaModule1_BeforeDecrypt(object sender, XafDelta.Messaging.CryptoEventArgs e)
        /// {
        ///     var sourceBytes = e.SourceStream.AllBytes();
        ///     var decryptedBytes = sourceBytes.ToList().ConvertAll&lt;byte&gt;(customXor).ToArray();
        ///     using (var memStream = new MemoryStream(decryptedBytes))
        ///     {
        ///          memStream.CopyTo(e.DestStream);
        ///     }
        ///     e.Done = true;
        /// }
        /// 
        /// private byte customXor(byte input)
        /// {
        ///     var result = input ^ 0xAA;
        ///     return (byte)result;
        /// }
        /// </code>
        /// </example>
        [Description("Occurs before decrypt message")]
        public event EventHandler<CryptoEventArgs> BeforeDecrypt;
        internal void DoBeforeDecrypt(CryptoEventArgs e)
        {
            var handler = BeforeDecrypt;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after decrypt message. See also <see cref="CryptoEventArgs"/>
        /// </summary>
        /// <seealso cref="CryptoEventArgs"/>
        [Description("Occurs after decrypt message")]
        public event EventHandler<CryptoEventArgs> AfterDecrypt;
        internal void DoAfterDecrypt(CryptoEventArgs e)
        {
            var handler = AfterDecrypt;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before (or instead of) message decompression. 
        /// Use this event for implement custom decompression. See also <see cref="StreamEventArgs"/>, <see cref="BeforeCompress"/>.
        /// </summary>        
        /// <example>
        /// This sample shows how to use <see cref="DoBeforeCompress"/> event to disable decompression.
        /// <code>
        /// private void xafDeltaModule1_BeforeDecompress(object sender, StreamEventArgs e)
        /// {
        ///     e.SourceStream.CopyTo(e.DestStream);
        ///     e.Done = true;
        /// }
        /// </code>  
        /// </example>
        [Description("Occurs before decompress message")]
        public event EventHandler<StreamEventArgs> BeforeDecompress;
        internal void DoBeforeDecompress(StreamEventArgs e)
        {
            var handler = BeforeDecompress;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after decompress message.
        /// </summary>
        [Description("Occurs after decompress message")]
        public event EventHandler<StreamEventArgs> AfterDecompress;
        internal void DoAfterDecompress(StreamEventArgs e)
        {
            var handler = AfterDecompress;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Get recipients for protocol record. Use this event to implement complex custom logic 
        /// for selecting recipients for protocol record . See also <see cref="GetRecipientsEventArgs"/>.
        /// </summary>
        /// <seealso cref="GetRecipientsEventArgs"/>
        /// <example>
        /// 
        /// This sample shows how to use <see cref="GetRecipients"/> event to select recipients with custom logic.
        /// <code>
        /// private void xafDeltaModule1_GetRecipients(object sender, XafDelta.Replication.Protocol.GetRecipientsEventArgs e)
        /// {
        ///     var currentNode = ReplicationNode.GetCurrentNode(e.ObjectSpace);
        ///     // for "_POS" nodes send all changes to parent node
        ///     if (currentNode != null &amp;&amp; currentNode.NodeId.EndsWith("_POS"))
        ///     {
        ///         e.Recipients.Add(currentNode.ParentNode);
        ///     }
        /// } 
        /// </code>
        /// </example>
        [Description("Get recipients for protocol record")]
        public event EventHandler<GetRecipientsEventArgs> GetRecipients;
        internal void OnGetRecipients(GetRecipientsEventArgs e)
        {
            var handler = GetRecipients;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before build packages. See also <see cref="BeforeBuildPackagesArgs"/>.
        /// </summary>
        /// <seealso cref="BeforeBuildPackagesArgs"/>
        [Description("Occurs before build packages.")]
        public event EventHandler<BeforeBuildPackagesArgs> BeforeBuildPackages;
        internal void DoBeforeBuildPackages(BeforeBuildPackagesArgs e)
        {
            var handler = BeforeBuildPackages;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after replicas packages. See also <see cref="AfterBuildPackagesArgs"/>.
        /// </summary>
        /// <seealso cref="AfterBuildPackagesArgs"/>
        [Description("Occurs after packages builded.")]
        public event EventHandler<AfterBuildPackagesArgs> AfterBuildPackages;
        internal void DoAfterBuildPackages(AfterBuildPackagesArgs e)
        {
            var handler = AfterBuildPackages;
            if (handler != null) handler(this, e);
        }
        
        /// <summary>
        /// Occurs before load packages. See also <see cref="LoadEventArgs"/>.
        /// </summary>
        /// <seealso cref="LoadEventArgs"/>
        [Description("Occurs before load packages.")]
        public event EventHandler<LoadEventArgs> BeforeLoad;
        internal void DoBeforeLoad(LoadEventArgs e)
        {
            var handler = BeforeLoad;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after load packages. See also <see cref="LoadEventArgs"/>.
        /// </summary>
        /// <seealso cref="LoadEventArgs"/>
        [Description("Occurs after load packages.")]
        public event EventHandler<LoadEventArgs> AfterLoad;
        internal void DoAfterLoad(LoadEventArgs e)
        {
            var handler = AfterLoad;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs after package is loaded into application database. 
        /// See also <see cref="LoadPackageEventArgs"/>, <see cref="BeforeLoadPackage"/>.
        /// </summary>
        /// <seealso cref="LoadPackageEventArgs"/>
        /// <seealso cref="BeforeLoadPackage"/>
        [Description("Occurs after load package.")]
        public event EventHandler<LoadPackageEventArgs> AfterLoadPackage;
        internal void DoAfterLoadPackage(LoadPackageEventArgs e)
        {
            var handler = AfterLoadPackage;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before package load into application database. 
        /// See also <see cref="LoadPackageEventArgs"/>, <see cref="AfterLoadPackage"/>.
        /// </summary>
        /// <seealso cref="LoadPackageEventArgs"/>
        /// <seealso cref="AfterLoadPackage"/>
        [Description("Occurs before load package.")]
        public event EventHandler<LoadPackageEventArgs> BeforeLoadPackage;
        internal void DoBeforeLoadPackage(LoadPackageEventArgs e)
        {
            var handler = BeforeLoadPackage;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs when invalid package detected before load. See also <see cref="InvalidPackageArgs"/>, <see cref="DoPackageLoadingError"/>.
        /// </summary> 
        /// <seealso cref="InvalidPackageArgs"/> 
        /// <seealso cref="DoPackageLoadingError"/>
        [Description("Occurs when invalid package detected while load.")]
        public event EventHandler<InvalidPackageArgs> InvalidPackage;
        internal void DoInvalidPackage(InvalidPackageArgs e)
        {
            var handler = InvalidPackage;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs when package loading error happened. See also <see cref="PackageLoadingErrorArgs"/>, <see cref="InvalidPackage"/>.
        /// </summary>
        /// <seealso cref="PackageLoadingErrorArgs"/>
        /// <seealso cref="InvalidPackage"/>
        [Description("Occurs when package loading error happened.")]
        public event EventHandler<PackageLoadingErrorArgs> PackageLoadingError;
        internal void DoPackageLoadingError(PackageLoadingErrorArgs e)
        {
            var handler = PackageLoadingError;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before load every package session. 
        /// Use this event to implement custom session loading. 
        /// See also <see cref="LoadSessionArgs"/>, <see cref="AfterLoadSession"/>, <see cref="DoBeforeLoadRecord"/>.
        /// </summary>
        /// <seealso cref="LoadSessionArgs"/>
        /// <seealso cref="AfterLoadSession"/>
        /// <seealso cref="DoBeforeLoadRecord"/>
        [Description("Occurs before load package session")]
        public event EventHandler<LoadSessionArgs> BeforeLoadSession;
        internal bool DoBeforeLoadSession(LoadSessionArgs e)
        {
            var handler = BeforeLoadSession;
            if (handler != null) handler(this, e);
            return e.Done;
        }

        /// <summary>
        /// Occurs after session was loaded. See also <see cref="LoadSessionArgs"/>, <see cref="BeforeLoadSession"/>.
        /// </summary>
        /// <seealso cref="LoadSessionArgs"/>
        /// <seealso cref="BeforeLoadSession"/>
        [Description("Occurs after load package session.")]
        public event EventHandler<LoadSessionArgs> AfterLoadSession;
        internal void DoAfterLoadSession(LoadSessionArgs e)
        {
            var handler = AfterLoadSession;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before load package record into application database. 
        /// Use this event to implement custom package record processing on load. 
        /// See also <see cref="LoadRecordArgs"/>, <see cref="BeforeLoadSession"/>, <see cref="AfterLoadRecord"/>.
        /// </summary>
        /// <seealso cref="LoadRecordArgs"/>
        /// <seealso cref="AfterLoadRecord"/>
        /// <seealso cref="BeforeLoadSession"/>
        [Description("Occurs before load package record.")]
        public event EventHandler<LoadRecordArgs> BeforeLoadRecord;
        internal bool DoBeforeLoadRecord(LoadRecordArgs e)
        {
            var handler = BeforeLoadRecord;
            if (handler != null) handler(this, e);
            return e.Done;
        }

        /// <summary>
        /// Occurs after package record was loaded. 
        /// See also <see cref="LoadRecordArgs"/>, <see cref="BeforeLoadRecord"/>, <see cref="DoBeforeLoadSession"/>.
        /// </summary>
        /// <seealso cref="LoadRecordArgs"/>
        /// <seealso cref="BeforeLoadRecord"/>
        /// <seealso cref="DoBeforeLoadSession"/>
        [Description("Occurs after load package record.")]
        public event EventHandler<LoadRecordArgs> AfterLoadRecord;
        internal void DoAfterLoadRecord(LoadRecordArgs e)
        {
            var handler = AfterLoadRecord;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs on resolve replication collision. 
        /// Use this event to resolve replication collisions in event handler code. 
        /// See also <see cref="ResolveReplicationCollisionArgs"/>.
        /// </summary>
        /// <seealso cref="ResolveReplicationCollisionArgs"/>
        [Description("Occurs on resolve replication collision.")]
        public event EventHandler<ResolveReplicationCollisionArgs> ResolveReplicationCollision;
        internal void DoResolveReplicationCollision(ResolveReplicationCollisionArgs e)
        {
            var handler = ResolveReplicationCollision;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs when current node Id value required. 
        /// This event raises at moment of current node creation (on database update or creation).
        /// Use this event to provide current node Id, based on database name, app settings etc. 
        /// See also <see cref="InitCurrentNodeIdArgs"/>
        /// </summary>
        /// <seealso cref="InitCurrentNodeIdArgs"/>
        /// <example>
        /// 
        /// This sample shows how to use <see cref="InitCurrentNodeId"/> event. 
        /// <code>
        /// private void xafDeltaModule1_InitCurrentNodeId(object sender, InitCurrentNodeIdArgs e)
        /// {
        ///     e.NodeId = Properties.Settings.Default.CurrentNodId;
        /// }
        /// </code>
        /// </example>
        [Description("Occurs when init current node id.")]
        public static event EventHandler<InitCurrentNodeIdArgs> InitCurrentNodeId;
        /// <summary>
        /// Called when init current node.
        /// </summary>
        /// <param name="e">The e.</param>
        public static void OnInitCurrentNodeId(InitCurrentNodeIdArgs e)
        {
            var handler = InitCurrentNodeId;
            if (handler != null) handler(null, e);
        }

        /// <summary>
        /// Occurs after snapshot building. 
        /// </summary>
        [Description("Occurs after build snapshot.")]
        public event EventHandler<AfterBuildSnapshotArgs> AfterBuildSnapshot;
        internal void OnAfterBuildSnapshot(AfterBuildSnapshotArgs e)
        {
            var handler = AfterBuildSnapshot;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Snapshot post-processing. Occurs after snapshot build, but before snapshot package closed. 
        /// Use this event for correction of snapshot package content. See also <see cref="SnapshotPostOperationArgs"/>
        /// </summary>
        /// <seealso cref="SnapshotPostOperationArgs"/>
        [Description("Snapshot post-processing.")]
        public event EventHandler<SnapshotPostOperationArgs> SnapshotPostBuild;
        /// <summary>
        /// Called when [snapshot post build].
        /// </summary>
        /// <param name="e">The e.</param>
        internal void OnSnapshotPostBuild(SnapshotPostOperationArgs e)
        {
            var handler = SnapshotPostBuild;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Snapshot post-load.
        /// </summary>
        [Description("Snapshot post-load.")]
        public event EventHandler<SnapshotPostOperationArgs> SnapshotPostLoad;
        /// <summary>
        /// Called when [snapshot post load].
        /// </summary>
        /// <param name="e">The e.</param>
        internal void OnSnapshotPostLoad(SnapshotPostOperationArgs e)
        {
            var handler = SnapshotPostLoad;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs when <see cref="XafDeltaModule"/> obtains specified member is non replicable. 
        /// This event enabling you to specify in handler whether selected member is not replicable. 
        /// </summary>
        [Description("Get member is not replicable.")]
        public event EventHandler<GetMemberIsNotReplicableArgs> GetMemberIsNotReplicable;
        /// <summary>
        /// Called when get member is not replicable.
        /// </summary>
        /// <param name="e">The e.</param>
        internal void OnGetMemberIsNotReplicable(GetMemberIsNotReplicableArgs e)
        {
            var handler = GetMemberIsNotReplicable;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Get current XafApplication instance. For internal use only.
        /// See also <see cref="CustomGetApplicationArgs"/>
        /// </summary>
        /// <seealso cref="CustomGetApplicationArgs"/>
        public static event EventHandler<CustomGetApplicationArgs> CustomGetApplication;
        /// <summary>
        /// Called when custom get application.
        /// </summary>
        /// <param name="e">The e.</param>
        internal static void OnCustomGetApplication(CustomGetApplicationArgs e)
        {
            var handler = CustomGetApplication;
            if (handler != null) handler(null, e);
        }

        /// <summary>
        /// Get current <see cref="IXafDeltaPlatform"/> for application. For internal use only.
        /// See also <see cref="GetPlatformArgs"/>.
        /// </summary>
        /// <seealso cref="GetPlatformArgs"/>
        public event EventHandler<GetPlatformArgs> GetPlatform;
        /// <summary>
        /// Called when get platform.
        /// </summary>
        /// <param name="e">The e.</param>
        public void OnGetPlatform(GetPlatformArgs e)
        {
            var handler = GetPlatform;
            if (handler != null) handler(this, e);
        }

        #endregion
    }

    #region Event args

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.GetPlatform"/> event
    /// </summary>
    public class GetPlatformArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the <see cref="IXafDeltaPlatform"/> platform for current application. 
        /// For internal use only.
        /// </summary>
        /// <value>
        /// The XafDelta platform.
        /// </value>
        public IXafDeltaPlatform XafDeltaPlatform { get; set; }
    }

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.CustomGetApplication"/> event
    /// </summary>
    public class CustomGetApplicationArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the XAF application instance.
        /// </summary>
        /// <value>
        /// The XAF application instance.
        /// </value>
        public XafApplication Application { get; set; }
    }

    #endregion
}
