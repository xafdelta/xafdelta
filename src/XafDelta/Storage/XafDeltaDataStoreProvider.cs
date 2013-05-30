using System;
using DevExpress.ExpressApp;
using DevExpress.Xpo.Metadata;

namespace XafDelta.Storage
{
    /// <summary>
    /// Xpo data store provider 
    /// (based on http://www.devexpress.com/Support/Center/KB/p/K18123.aspx sample)
    /// </summary>
    internal sealed class XafDeltaDataStoreProvider : IXpoDataStoreProvider
    {
        private readonly XpoDataStoreProxy proxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="XafDeltaDataStoreProvider"/> class.
        /// </summary>
        public XafDeltaDataStoreProvider()
        {
            proxy = new XpoDataStoreProxy();
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString
        {
            get { return ""; }
        }

        /// <summary>
        /// Creates the updating store.
        /// </summary>
        /// <param name="disposableObjects">The disposable objects.</param>
        /// <returns></returns>
        public DevExpress.Xpo.DB.IDataStore CreateUpdatingStore(out IDisposable[] disposableObjects)
        {
            disposableObjects = null;
            return proxy;
        }

        /// <summary>
        /// Creates the working store.
        /// </summary>
        /// <param name="disposableObjects">The disposable objects.</param>
        /// <returns></returns>
        public DevExpress.Xpo.DB.IDataStore CreateWorkingStore(out IDisposable[] disposableObjects)
        {
            disposableObjects = null;
            return proxy;
        }

        /// <summary>
        /// Gets the XP dictionary.
        /// </summary>
        public XPDictionary XPDictionary
        {
            get { return null; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initializes the specified dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="appConnectionString">The app connection string.</param>
        /// <param name="secondStorageConnectionString">The second storagea connection string.</param>
        /// <param name="secondStorageTypes">The second storage types.</param>
        public void Initialize(XPDictionary dictionary, string appConnectionString, 
            string secondStorageConnectionString, Type[] secondStorageTypes)
        {
            proxy.Initialize(dictionary, appConnectionString, secondStorageConnectionString, secondStorageTypes);
            IsInitialized = true;
        }
    }
}