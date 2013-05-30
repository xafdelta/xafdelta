using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Metadata;

namespace XafDelta.Storage
{
    /// <summary>
    /// Xpo data store proxy
    /// (based on http://www.devexpress.com/Support/Center/KB/p/K18123.aspx sample)
    /// </summary>
    internal sealed class XpoDataStoreProxy : IDataStore {
        private SimpleDataLayer appDataLayer;
        private IDataStore appDataStore;
        private SimpleDataLayer secondStorageDataLayer;
        private IDataStore secondStorageDataStore;
        private string secondStorageConnStr;
        private ReflectionDictionary secondStorageDictionary;
        private readonly List<List<DBTable>> delayedPackageUpdates = new List<List<DBTable>>();
        private static readonly List<string> secondStorageTableNames = new List<string>();

        /// <summary>
        /// Initializes the proxy.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="appConnectionString">The app connection string.</param>
        /// <param name="secondStorageConnectionString">The replication storage connection string.</param>
        /// <param name="secondStorageTypes">The second storage types.</param>
        public void Initialize(XPDictionary dictionary, string appConnectionString,
            string secondStorageConnectionString, IEnumerable<Type> secondStorageTypes)
        {
            var secondStorageClasses = secondStorageTypes.Select(currentType => dictionary.GetClassInfo(currentType))
                .Where(classInfo => classInfo != null).ToList();
            Initialize(dictionary, appConnectionString, secondStorageConnectionString, secondStorageClasses);            
        }

        /// <summary>
        /// Initializes the proxy.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="appConnectionString">The app connection string.</param>
        /// <param name="secondStorageConnectionString">The replication storage connection string.</param>
        /// <param name="secondStorageClasses">The second storage classes.</param>
        public void Initialize(XPDictionary dictionary, string appConnectionString,
            string secondStorageConnectionString, IEnumerable<XPClassInfo> secondStorageClasses) 
        {
            var appDictionary = new ReflectionDictionary();
            secondStorageDictionary = new ReflectionDictionary();

            secondStorageClasses.ToList().ForEach(x => secondStorageTableNames.Add(x.TableName));

            foreach(XPClassInfo ci in dictionary.Classes) 
            {
                if (secondStorageTableNames.Contains(ci.TableName)) 
                    secondStorageDictionary.QueryClassInfo(ci.ClassType);

                appDictionary.QueryClassInfo(ci.ClassType);
            }

            appDataStore = XpoDefault.GetConnectionProvider(appConnectionString, AutoCreateOption.DatabaseAndSchema);
            appDataLayer = new SimpleDataLayer(appDictionary, appDataStore);

            secondStorageConnStr = secondStorageConnectionString;
        }

        /// <summary>
        /// Opens the second storage.
        /// </summary>
        private void openSecondStorage()
        {
            if (!string.IsNullOrEmpty(secondStorageConnStr) 
                && (secondStorageDataStore == null || secondStorageDataStore == appDataStore))
            {
                try
                {
                    secondStorageDataStore = XpoDefault.GetConnectionProvider(secondStorageConnStr, AutoCreateOption.DatabaseAndSchema);
                    secondStorageDataLayer = new SimpleDataLayer(secondStorageDictionary, secondStorageDataStore);
                }
                catch (Exception exception)
                {
                    var args = new GetPlatformArgs();
                    XafDeltaModule.Instance.OnGetPlatform(args);
                    if(args.XafDeltaPlatform != null)
                        args.XafDeltaPlatform.OnShowError(new ErrorEventArgs(exception));

                    secondStorageDataStore = appDataStore;
                    secondStorageDataLayer = appDataLayer;
                }

                foreach (var packageUpdate in delayedPackageUpdates)
                    secondStorageDataStore.UpdateSchema(false, packageUpdate.ToArray());

                delayedPackageUpdates.Clear();
            }
            return;
        }

        /// <summary>
        /// When implemented by a class, returns which operations are performed when a data store is accessed for the first time.
        /// </summary>
        /// <value>
        /// An <see cref="T:DevExpress.Xpo.DB.AutoCreateOption"/> value that specifies which operations are performed when a data store is accessed for the first time.
        /// </value>
        public AutoCreateOption AutoCreateOption 
        {
            get { return AutoCreateOption.DatabaseAndSchema; }
        }

        /// <summary>
        /// Updates data in a data store using the specified modification statements.
        /// </summary>
        /// <param name="dmlStatements">An array of data modification statements.</param>
        /// <returns>
        /// The result of the data modifications.
        /// </returns>
        public ModificationResult ModifyData(params ModificationStatement[] dmlStatements) {
            var appChanges = new List<ModificationStatement>(dmlStatements.Length);
            var ssChanges = new List<ModificationStatement>(dmlStatements.Length);

            foreach (var stm in dmlStatements)
            {
                if (isSecondStorageTable(stm.TableName))
                    ssChanges.Add(stm);
                else
                    appChanges.Add(stm);
            }

            var resultSet = new List<ParameterValue>();
            if (appChanges.Count > 0)
                resultSet.AddRange(appDataLayer.ModifyData(appChanges.ToArray()).Identities);

            if (ssChanges.Count > 0)
            {
                openSecondStorage();
                resultSet.AddRange(secondStorageDataLayer.ModifyData(ssChanges.ToArray()).Identities);
            }

            return new ModificationResult(resultSet);
        }

        /// <summary>
        /// Determines whether the specified table should resides in second storage.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns>
        ///   <c>true</c> if the specified table is second storage table ; otherwise, <c>false</c>.
        /// </returns>
        private bool isSecondStorageTable(string tableName)
        {
            return secondStorageTableNames.Contains(tableName) && !string.IsNullOrEmpty(secondStorageConnStr);
        }

        /// <summary>
        /// When implemented by a class, fetches data from a data store using the specified query statements.
        /// </summary>
        /// <param name="selects">An array of statements to obtain data from the data store.</param>
        /// <returns>
        /// Data retrieved from the data store.
        /// </returns>
        public SelectedData SelectData(params SelectStatement[] selects) {
            var appSelects = new List<SelectStatement>(selects.Length);
            var ssSelects = new List<SelectStatement>(selects.Length);
            foreach (var stm in selects)
            {
                if (isSecondStorageTable(stm.TableName))
                    ssSelects.Add(stm);
                else
                    appSelects.Add(stm);
            }

            var resultSet = new List<SelectStatementResult>();
            if (appSelects.Count > 0)
                resultSet.AddRange(appDataLayer.SelectData(appSelects.ToArray()).ResultSet);

            if (ssSelects.Count > 0)
            {
                openSecondStorage();
                resultSet.AddRange(secondStorageDataLayer.SelectData(ssSelects.ToArray()).ResultSet);
            }

            return new SelectedData(resultSet.ToArray());
        }

        /// <summary>
        /// When implemented by a class, updates the storage schema according to the specified class descriptions.
        /// </summary>
        /// <param name="dontCreateIfFirstTableNotExist"><b>true</b> if the schema should not be created if the table that corresponds to the first item in the <i>tables</i> array doesn't exist in the data store.</param>
        /// <param name="tables">An array of tables whose structure should be saved in the data store.</param>
        /// <returns>
        /// An <see cref="T:DevExpress.Xpo.DB.UpdateSchemaResult"/> value that specifies the result of the update operation.
        /// </returns>
        public UpdateSchemaResult UpdateSchema(bool dontCreateIfFirstTableNotExist, params DBTable[] tables) 
        {
            var appTables = new List<DBTable>();
            var ssTables = new List<DBTable>();

            foreach(var table in tables) 
            {
                if (isSecondStorageTable(table.Name)) 
                    ssTables.Add(table);
                else 
                    appTables.Add(table);
            }

            appDataStore.UpdateSchema(false, appTables.ToArray());

            if (ssTables.Count > 0 && !string.IsNullOrEmpty(secondStorageConnStr))
            {
                if (secondStorageDataStore == null)
                    delayedPackageUpdates.Add(ssTables);
                else
                    secondStorageDataStore.UpdateSchema(false, ssTables.ToArray());
            }

            return UpdateSchemaResult.SchemaExists;
        }
    }
}