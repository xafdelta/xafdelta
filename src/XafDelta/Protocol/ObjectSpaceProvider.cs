using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Utils;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Helpers;
using DevExpress.Xpo.Metadata;

namespace XafDelta.Protocol
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ParameterTypeCanBeEnumerable.Local

    /// <summary>
    /// Object space provider. Based on DevExpress.ExpressApp.ObjectSpaceProvider source code.
    /// Uses XafDelta.Protocol.Native.ObjectSpace as object space type.
    /// </summary>
    public class ObjectSpaceProvider : IObjectSpaceProvider, IDisposableExt
    {
        private readonly DisposeIndicator disposeIndicator = new DisposeIndicator();
        private IXpoDataStoreProvider dataStoreProvider;
        private Session defaultSession;
        private IDataLayer workingDataLayer;
        private IDataStore workingDataStore;
        private IDisposable[] workingDataStoreDisposableObjects;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSpaceProvider"/> class.
        /// </summary>
        /// <param name="dataStoreProvider">The data store provider.</param>
        public ObjectSpaceProvider(IXpoDataStoreProvider dataStoreProvider)
        {
            XpoDefault.DefaultCaseSensitive = false;
            XpoDefault.IdentityMapBehavior = IdentityMapBehavior.Strong;
            this.dataStoreProvider = dataStoreProvider;
            if (Session.DefaultSession != null)
            {
                defaultSession = Session.DefaultSession;
                defaultSession.BeforeConnect += DefaultSession_BeforeConnect;
            }
        }

        /// <summary>
        /// Gets the XP dictionary.
        /// </summary>
        public XPDictionary XPDictionary
        {
            get { return XafTypesInfo.XpoTypeInfoSource.XPDictionary; }
        }

        /// <summary>
        /// Gets the working data layer.
        /// </summary>
        public IDataLayer WorkingDataLayer
        {
            get { return workingDataLayer; }
        }

        #region IDisposableExt Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (defaultSession != null)
            {
                defaultSession.BeforeConnect -= DefaultSession_BeforeConnect;
                defaultSession = null;
            }
            ReleaseWorkingResources();
            disposeIndicator.Dispose();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed
        {
            get { return disposeIndicator.IsDisposed; }
        }

        #endregion

        #region IObjectSpaceProvider Members

        /// <summary>
        /// Instantiates an Object Space.
        /// </summary>
        /// <returns>
        /// An <see cref="T:DevExpress.ExpressApp.IObjectSpace"/> object representing the instantiated Object Space.
        /// </returns>
        public IObjectSpace CreateObjectSpace()
        {
            Guard.NotDisposed(this);
            if (workingDataStore == null)
            {
                workingDataStoreDisposableObjects = null;
                workingDataStore = dataStoreProvider.CreateWorkingStore(out workingDataStoreDisposableObjects);
            }
            if (workingDataLayer == null)
            {
                workingDataLayer = CreateWorkingDataLayer(workingDataStore);
            }
            var unitOfWork = new UnitOfWork(workingDataLayer);
            return CreateObjectSpaceCore(unitOfWork, TypesInfo);
        }

        /// <summary>
        /// Instantiates an Object Space that can be used to update the database.
        /// </summary>
        /// <param name="allowUpdateSchema"><b>true</b>, to allow schema updates; otherwise, <b>false</b>.</param>
        /// <returns>
        /// An <see cref="T:DevExpress.ExpressApp.IObjectSpace"/> object representing the instantiated Object Space that can be used to update the database.
        /// </returns>
        public IObjectSpace CreateUpdatingObjectSpace(Boolean allowUpdateSchema)
        {
            Guard.NotDisposed(this);
            IDisposable[] disposableObjects;
            IDataStore dataStore;

            if (allowUpdateSchema || System.Diagnostics.Debugger.IsAttached /* BPOST */) 
            {
                dataStore = dataStoreProvider.CreateUpdatingStore(out disposableObjects);
            }
            else
            {
                //dataStore = dataStoreProvider.CreateUpdatingStore(out disposableObjects);
                dataStore = dataStoreProvider.CreateWorkingStore(out disposableObjects);
            }
            UnitOfWork unitOfWork = CreateUnitOfWork(dataStore, disposableObjects);
            return new ObjectSpace(unitOfWork, TypesInfo);
        }

        /// <summary>
        /// Specifies the connection string used by the Object Space Provider's data layer.
        /// </summary>
        /// <value>
        /// A connection string used by the Object Space Provider's data layer.
        /// </value>
        public string ConnectionString
        {
            get
            {
                if (workingDataLayer != null && workingDataLayer.Connection != null)
                {
                    return workingDataLayer.Connection.ConnectionString;
                }
                return RemovePassword(dataStoreProvider.ConnectionString);
            }
            set { SetDataStoreProvider(new ConnectionStringDataStoreProvider(value)); }
        }

        /// <summary>
        /// Supplies metadata on types used in an <b>XAF</b> application.
        /// </summary>
        /// <value>
        /// An <see cref="T:DevExpress.ExpressApp.DC.ITypesInfo"/> object which supplies metadata on types used in an <b>XAF</b> application.
        /// </value>
        public ITypesInfo TypesInfo
        {
            get { return XafTypesInfo.Instance; }
        }

        #endregion

        private void DefaultSession_BeforeConnect(object sender, SessionManipulationEventArgs e)
        {
            throw new Exception(
                "This error occurs because you tried to use the Session.DefaultSession " +
                "(please refer to XPO documentation to learn more) in XAF. " +
                "It is not allowed by default. Instead, use the XafApplication.ConnectionString property " +
                "to configure the connection to the database according to your needs. " +
                "Most likely, this problem is caused by the fact that you instantiated your persistent object, " +
                "XPCollection, etc. without the Session parameter and the default session was used by default. " +
                "Use a non-default session object to instantiate your objects and collections.");
        }

        private string RemovePassword(string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }
            var parser = new ConnectionStringParser(connectionString);
            parser.RemovePartByName("Password");
            parser.RemovePartByName("Jet OLEDB:Database Password");
            return parser.GetConnectionString();
        }

        private Session CreateSession(IDataStore dataStore, IDisposable[] disposableObjects)
        {
            var disposableObjectsList = new List<IDisposable>();
            if (disposableObjects != null)
            {
                disposableObjectsList.AddRange(disposableObjects);
            }
            var dataLayer = new SimpleDataLayer(XPDictionary, dataStore);
            disposableObjectsList.Add(dataLayer);
            return new Session(dataLayer, disposableObjectsList.ToArray());
        }

        private UnitOfWork CreateUnitOfWork(IDataStore dataStore, IDisposable[] disposableObjects)
        {
            var disposableObjectsList = new List<IDisposable>();
            if (disposableObjects != null)
            {
                disposableObjectsList.AddRange(disposableObjects);
            }
            var dataLayer = new SimpleDataLayer(XPDictionary, dataStore);
            disposableObjectsList.Add(dataLayer);
            return new UnitOfWork(dataLayer, disposableObjectsList.ToArray());
        }

        private void ReleaseWorkingResources()
        {
            if (workingDataStoreDisposableObjects != null)
            {
                foreach (IDisposable disposableObject in workingDataStoreDisposableObjects)
                {
                    try
                    {
                        disposableObject.Dispose();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
                workingDataStoreDisposableObjects = null;
            }
            if (workingDataLayer != null)
            {
                workingDataLayer.Dispose();
                workingDataLayer = null;
            }
// ReSharper disable RedundantCheckBeforeAssignment
            if (workingDataStore != null)
// ReSharper restore RedundantCheckBeforeAssignment
            {
                workingDataStore = null;
            }
        }

        private void AsyncServerModeSourceResolveSession(ResolveSessionEventArgs args)
        {
            IDisposable[] disposableObjects;
            IDataStore dataStore = dataStoreProvider.CreateWorkingStore(out disposableObjects);
            args.Session = CreateUnitOfWork(dataStore, disposableObjects);
        }

        private void AsyncServerModeSourceDismissSession(ResolveSessionEventArgs args)
        {
            var toDispose = args.Session as IDisposable;
            if (toDispose != null)
            {
                toDispose.Dispose();
            }
        }

        /// <summary>
        /// Creates the working data layer.
        /// </summary>
        /// <param name="workingDataStoreParam">The working data store param.</param>
        /// <returns>Data layer</returns>
        protected virtual IDataLayer CreateWorkingDataLayer(IDataStore workingDataStoreParam)
        {
            return new SimpleDataLayer(XPDictionary, workingDataStoreParam);
        }

        /// <summary>
        /// Creates the object space core.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="typesInfo">The types info.</param>
        /// <returns>Object space</returns>
        protected virtual IObjectSpace CreateObjectSpaceCore(UnitOfWork unitOfWork, ITypesInfo typesInfo)
        {
            var objectSpace = new ObjectSpace(unitOfWork, typesInfo)
                                  {
                                      AsyncServerModeSourceResolveSession = AsyncServerModeSourceResolveSession,
                                      AsyncServerModeSourceDismissSession = AsyncServerModeSourceDismissSession
                                  };
            return objectSpace;
        }

        /// <summary>
        /// Creates the updating read only session.
        /// </summary>
        /// <returns>Session</returns>
        public Session CreateUpdatingReadOnlySession()
        {
            Guard.NotDisposed(this);
            IDisposable[] disposableObjects;
            IDataStore dataStore = dataStoreProvider.CreateWorkingStore(out disposableObjects);
            return CreateSession(dataStore, disposableObjects);
        }

        /// <summary>
        /// Creates the updating session.
        /// </summary>
        /// <returns>Session</returns>
        public Session CreateUpdatingSession()
        {
            Guard.NotDisposed(this);
            IDisposable[] disposableObjects;
            IDataStore dataStore = dataStoreProvider.CreateUpdatingStore(out disposableObjects);
            return CreateSession(dataStore, disposableObjects);
        }

        /// <summary>
        /// Sets the data store provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        public void SetDataStoreProvider(IXpoDataStoreProvider provider)
        {
            ReleaseWorkingResources();
            dataStoreProvider = provider;
        }
    }

    /// <summary>
    /// Audit object space provider thread safe
    /// </summary>
    public class ObjectSpaceProviderThreadSafe : ObjectSpaceProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSpaceProviderThreadSafe"/> class.
        /// </summary>
        /// <param name="dataStoreProvider">The data store provider.</param>
        public ObjectSpaceProviderThreadSafe(IXpoDataStoreProvider dataStoreProvider)
            : base(dataStoreProvider)
        {
        }

        /// <summary>
        /// Creates the working data layer.
        /// </summary>
        /// <param name="workingDataStore">The working data store.</param>
        /// <returns>Data layer</returns>
        protected override IDataLayer CreateWorkingDataLayer(IDataStore workingDataStore)
        {
            return new ThreadSafeDataLayer(XPDictionary, workingDataStore);
        }
    }

    // ReSharper restore ParameterTypeCanBeEnumerable.Local
    // ReSharper restore InconsistentNaming
}