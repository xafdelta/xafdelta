using System.Data;
using DevExpress.ExpressApp;

namespace XafDelta
{
    /// <summary>
    /// Data store provider factory interface.
    /// For internal use only.
    /// </summary>
    internal interface IDataStoreProviderFactory
    {
        /// <summary>
        /// Creates the data store provider.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="connectionString"></param>
        /// <returns>Data store provider</returns>
        IXpoDataStoreProvider CreateDataStoreProvider(IDbConnection connection, string connectionString);
    }
}
