using System;
using System.Data;
using DevExpress.ExpressApp;
using DevExpress.Xpo.Metadata;

namespace XafDelta.Storage
{
    /// <summary>
    /// Storage service - two database application support.
    /// </summary>
    internal sealed class StorageService : IDataStoreProviderFactory
    {
        private static StorageService instance;
        /// <summary>
        /// Gets the service singleton.
        /// </summary>
        public static StorageService Instance
        {
            get { return instance ?? (instance = new StorageService()); }
        }

        /// <summary>
        /// Initialize service.
        /// </summary>
        /// <param name="application">The application.</param>
        /// <param name="secondStorageConnectionString">The second storage connection string.</param>
        /// <param name="secondStorageTypes">The second storage types.</param>
        public void Initialize(XafApplication application, 
            string secondStorageConnectionString, 
            params Type[] secondStorageTypes)
        {
            application.CustomCheckCompatibility += application_CustomCheckCompatibility;
            application.Disposed += application_Disposed;
            ssConnectionString = secondStorageConnectionString;
            ssTypes = secondStorageTypes;
        }

        /// <summary>
        /// Handles the Disposed event of the application. Unsubscribe from application events.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void application_Disposed(object sender, EventArgs e)
        {
            var application = (XafApplication)sender;
            application.CustomCheckCompatibility -= application_CustomCheckCompatibility;
            application.Disposed -= application_Disposed;
        }

        /// <summary>
        /// Custom DataStoreProvider for multi-database support
        /// </summary>
        private XafDeltaDataStoreProvider provider;
        private string ssConnectionString;
        private Type[] ssTypes;

        /// <summary>
        /// Applications the custom check compatibility. Create and initialize data store provider.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DevExpress.ExpressApp.CustomCheckCompatibilityEventArgs"/> instance containing the event data.</param>
        private void application_CustomCheckCompatibility(object sender, CustomCheckCompatibilityEventArgs e)
        {
            if (provider == null || provider.IsInitialized)
                return;

            var application = (XafApplication)sender;
            var providerType = application.ObjectSpaceProvider.GetType();
            var dict = (XPDictionary)providerType.GetProperty("XPDictionary").GetValue(application.ObjectSpaceProvider, null);
            var connStr = application.Connection == null ? application.ConnectionString : application.Connection.ConnectionString;
            provider.Initialize(dict, connStr, ssConnectionString, ssTypes);
        }

        #region IDataStoreProviderFactory

        /// <summary>
        /// Creates the data store provider.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public IXpoDataStoreProvider CreateDataStoreProvider(IDbConnection connection, string connectionString)
        {
            IXpoDataStoreProvider dataStoreProvider = null;
            var multiDatabase = !string.IsNullOrEmpty(ssConnectionString);
            if (multiDatabase)
            {
                provider = new XafDeltaDataStoreProvider();
                dataStoreProvider = provider;
            }
            return dataStoreProvider;
        }

        #endregion
    }
}
