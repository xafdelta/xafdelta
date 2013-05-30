using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;

namespace XafDelta.Messaging
{
    /// <summary>
    /// XAF object mapping information. 
    /// Defines link between specified Target object resides in current database and the same object in other replication node.
    /// For internal use only.
    /// </summary>
    [MapInheritance(MapInheritanceType.ParentTable)]
    [RuleCombinationOfPropertiesIsUnique("", DefaultContexts.Save, "AssemblyName,ClassName,ObjectId,NodeId")]
    [IsLocal]
    public sealed class OidMap : ObjectReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OidMap"/> class.
        /// </summary>
        /// <param name="session">The session.</param>
        public OidMap(Session session)
            : base(session)
        {
        }

        /// <summary>
        /// Convert NewObject reference to XPWeakReference after NewObject persist.
        /// </summary>
        internal void FixReference()
        {
            if (NewObject != null && NewObject is IXPObject)
            {
                var xpObj = (IXPObject)NewObject;
                if (!xpObj.Session.IsNewObject(xpObj))
                {
                    xpObj = (IXPObject)Session.GetObjectByKey(xpObj.ClassInfo, xpObj.Session.GetKeyValue(xpObj));
                    Target = new XPWeakReference(Session, xpObj);
                    NewObject = null;
                }
            }
            Save();
        }

        /// <summary>
        /// Gets or sets the target XPWeakReference.
        /// </summary>
        /// <value>
        /// The target XPWeakReference.
        /// </value>
        public XPWeakReference Target
        {
            get { return GetPropertyValue<XPWeakReference>("Target"); }
            set { SetPropertyValue("Target", value); }
        }

        /// <summary>
        /// Gets or sets the replication node id.
        /// </summary>
        /// <value>
        /// The replication node id.
        /// </value>
        [Size(255)]
        [Indexed]
        public string NodeId
        {
            get { return GetPropertyValue<string>("NodeId"); }
            set { SetPropertyValue("NodeId", value); }
        }

        [NonPersistent]
        internal object NewObject { get; set; }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object TargetObject
        {
            get { return Target == null ? NewObject : Target.Target; }
        }

        /// <summary>
        /// Finds the specified object in application database.
        /// </summary>
        /// <param name="objectSpace">The application object space.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="nodeId">The replication node id.</param>
        /// <returns>Object from application database</returns>
        public static object FindApplicationObject(IObjectSpace objectSpace, IObjectReference reference, string nodeId)
        {
            if (objectSpace == null) throw new ArgumentNullException("objectSpace");
            if (reference == null) throw new ArgumentNullException("reference");
            if (nodeId == null) throw new ArgumentNullException("nodeId");

            object result;

            // search existsing OidMap
            var criteria = CriteriaOperator.Parse("NodeId = ? And AssemblyName = ? And ClassName = ? And ObjectId = ?",
                nodeId, reference.AssemblyName, reference.ClassName, reference.ObjectId);

            var mapEntry = objectSpace.FindObject<OidMap>(criteria, true);
            if (mapEntry == null)
            {
                // search using known maps
                result = findApplicationObjectByKnownMaps(objectSpace, reference);
                
                if (result == null && reference.ReplicationKey != null)
                {
                    Type classType = null;
                    var ti = objectSpace.TypesInfo.FindTypeInfo(reference.ClassName);
                    if(ti != null)
                        classType = ti.Type;
                    else
                    {
                        var classInfo = ((ObjectSpace) objectSpace).Session.GetClassInfo(reference.AssemblyName,
                                                                                         reference.ClassName);
                        if(classInfo != null)
                            classType = classInfo.ClassType;
                    }

                    // search using replication key
                    result = findApplicationObjectByReplicationKey(classType, reference, objectSpace);
                }

                if (result != null)
                    CreateOidMap(objectSpace, reference, nodeId, result);
            }
            else
                result = mapEntry.TargetObject;

            return result;
        }

        /// <summary>
        /// Finds the application object by replication key.
        /// </summary>
        /// <param name="classType">Type of the class.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="objectSpace">The object space.</param>
        /// <returns>Application object</returns>
        private static object findApplicationObjectByReplicationKey(Type classType, 
            IObjectReference reference, IObjectSpace objectSpace)
        {
            object result = null;
            var modelClass = XafDeltaModule.XafApp.FindModelClass(classType);
            if (modelClass != null && reference.ReplicationKey != null)
            {
                var replicationKeyMember = ExtensionsHelper.GetReplicationKeyMember(modelClass.TypeInfo);
                var replicationKeyIsCaseInsensitive = modelClass.ReplicationKeyIsCaseInsensitive();
                var replicationKeyIsSpaceInsensitive = modelClass.ReplicationKeyIsSpaceInsensitive();
                if (replicationKeyMember != null)
                {

                    CriteriaOperator opLeft = new OperandProperty(replicationKeyMember.Name);
                    CriteriaOperator opRight = new OperandValue(reference.ReplicationKey);

                    if (replicationKeyIsCaseInsensitive)
                    {
                        opLeft = new FunctionOperator(FunctionOperatorType.Upper, opLeft);
                        opRight = new FunctionOperator(FunctionOperatorType.Upper, opRight);
                    }

                    if (replicationKeyIsSpaceInsensitive)
                    {
                        opLeft = new FunctionOperator(FunctionOperatorType.Replace, opLeft,
                            new OperandValue(" "), new OperandValue(String.Empty));
                        opRight = new FunctionOperator(FunctionOperatorType.Replace, opRight,
                            new OperandValue(" "), new OperandValue(String.Empty));
                    }

                    var keyCriteria = new BinaryOperator(opLeft, opRight, BinaryOperatorType.Equal);
                    if(replicationKeyMember.MemberInfo.IsAliased)
                    {
                        var list = objectSpace.CreateCollection(modelClass.TypeInfo.Type);

                        result = (from c in list.Cast<object>() 
                                  let objKeyValue = replicationKeyMember.MemberInfo.GetValue(c)
                                  where keysMatches(objKeyValue, reference.ReplicationKey, 
                                    replicationKeyIsCaseInsensitive, replicationKeyIsSpaceInsensitive)
                                  select c).FirstOrDefault();
                    }
                    else
                        result = objectSpace.FindObject(classType, keyCriteria, true);
                }
            }
            return result;
        }

        private static bool keysMatches(object objKeyValue, string replicationKey, 
            bool replicationKeyIsCaseInsensitive, bool replicationKeyIsSpaceInsensitive)
        {
            var result = !(objKeyValue == null || replicationKey == null);
            if(result)
            {
                var strObjKey = objKeyValue.ToString();
                if (replicationKeyIsCaseInsensitive)
                    strObjKey = strObjKey.ToLower();
                if(replicationKeyIsSpaceInsensitive)
                    strObjKey = strObjKey.Replace(" ", "");
                if (replicationKeyIsCaseInsensitive)
                    replicationKey = replicationKey.ToLower();
                if (replicationKeyIsSpaceInsensitive)
                    replicationKey = replicationKey.Replace(" ", "");
                result = strObjKey == replicationKey;
            }
            return result;
        }

        /// <summary>
        /// Finds the application object by known maps.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="reference">The reference.</param>
        /// <returns>Application object</returns>
        private static object findApplicationObjectByKnownMaps(IObjectSpace objectSpace, IObjectReference reference)
        {
            object result = null;
            // search using known mappings
            if (reference is IPackageObjectReference)
            {
                var objRef = (IPackageObjectReference) reference;
                if (!string.IsNullOrEmpty(objRef.KnownMapping))
                {
                    var typeInfo = objectSpace.TypesInfo.FindTypeInfo(objRef.ClassName);
                    if (typeInfo != null)
                    {
                        foreach (var mappingString in objRef.KnownMapping.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var mapValues = mappingString.Split('\a');
                            if (mapValues.Length < 2) continue;
                            
                            var refNodeId = mapValues[0];

                            if (refNodeId == XafDeltaModule.Instance.CurrentNodeId)
                                // direct search by TargetId in current database
                                result = objectSpace.GetObjectByKey(typeInfo.Type, 
                                    XPWeakReference.StringToKey(mapValues[1]));
                            else
                            {
                                // search similar mapping in existing OidMaps and return its target
                                var searchCriteria =
                                    CriteriaOperator.Parse("NodeId = ? And AssemblyName = ? And ClassName = ? And ObjectId = ?",
                                                           refNodeId, reference.AssemblyName, reference.ClassName, mapValues[1]);
                                var knownMap = objectSpace.FindObject<OidMap>(searchCriteria, true);
                                if (knownMap != null)
                                    result = knownMap.TargetObject;
                            }

                            if (result != null)
                                break;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates the oid map.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="nodeId">The node id.</param>
        /// <param name="appObject">The target.</param>
        /// <returns>Oid map</returns>
        public static OidMap CreateOidMap(IObjectSpace objectSpace, 
            IObjectReference reference, 
            string nodeId, object appObject)
        {
            if (objectSpace == null) throw new ArgumentNullException("objectSpace");
            if (reference == null) throw new ArgumentNullException("reference");
            if (nodeId == null) throw new ArgumentNullException("nodeId");
            if (appObject == null) throw new ArgumentNullException("appObject");

            var result = objectSpace.CreateObject<OidMap>();
            result.NodeId = nodeId;
            result.ObjectId = reference.ObjectId;
            result.ClassName = reference.ClassName;
            result.AssemblyName = reference.AssemblyName;
            result.AssemblyQualifiedName = reference.AssemblyQualifiedName;
            result.NewObject = appObject;
            result.Save();
            return result;
        }

        /// <summary>
        /// Creates the oid map.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="objectId">The object id.</param>
        /// <param name="nodeId">The node id.</param>
        /// <param name="appObject">The app object.</param>
        /// <returns>Oid map</returns>
        public static OidMap CreateOidMap(IObjectSpace objectSpace, string objectId, string nodeId, IXPObject appObject)
        {
            if (objectSpace == null) throw new ArgumentNullException("objectSpace");
            if (nodeId == null) throw new ArgumentNullException("nodeId");
            if (appObject == null) throw new ArgumentNullException("appObject");

            var result = objectSpace.CreateObject<OidMap>();
            result.NodeId = nodeId;
            result.ObjectId = objectId;
            result.ClassName = appObject.ClassInfo.FullName;
            result.AssemblyName = appObject.ClassInfo.AssemblyName;
            result.AssemblyQualifiedName = appObject.ClassInfo.ClassType.AssemblyQualifiedName;
            result.NewObject = appObject;
            result.Save();
            return result;
        }
        
        /// <summary>
        /// Gets the oid map for app object.
        /// </summary>
        /// <param name="appObject">The app object.</param>
        /// <param name="nodeId">The node id.</param>
        /// <returns>Oid map</returns>
        public static OidMap GetOidMap(object appObject, string nodeId)
        {
            if (appObject == null) throw new ArgumentNullException("appObject");
            if (nodeId == null) throw new ArgumentNullException("nodeId");

            var xpObj = (IXPObject)appObject;

            // search using OidMap
            var criteria = CriteriaOperator.Parse("NodeId = ? And AssemblyName = ? "
                + "And ClassName = ? And Not IsNull(Target) And Target.TargetKey_ = ?",
                nodeId, xpObj.ClassInfo.AssemblyName, xpObj.ClassInfo.FullName,
                XPWeakReference.KeyToString(xpObj.Session.GetKeyValue(xpObj)));
            var result = xpObj.Session.FindObject<OidMap>(PersistentCriteriaEvaluationBehavior.InTransaction, criteria);

            // search in new maps
            if(result == null)
            {
                var newMaps = xpObj.Session.GetObjectsToSave().Cast<object>().Where(x => x is OidMap).Cast<OidMap>();
                foreach (var map in newMaps)
                {
                    if(map.TargetObject == appObject)
                    {
                        result = map;
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the oid maps.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Oid maps</returns>
        public static IEnumerable<OidMap> GetOidMaps(IXPObject source)
        {
            var className = source.ClassInfo.FullName;
            var modelClass = XafDeltaModule.XafApp.FindModelClass(source.GetType());
            if (modelClass != null)
                className = modelClass.Name;

            var criteria = CriteriaOperator.Parse("ClassName = ? And Not IsNull(Target) And Target.TargetKey_ = ?",
                className /*source.ClassInfo.FullName*/,
                XPWeakReference.KeyToString(source.Session.GetKeyValue(source)));
            var result = new XPCollection<OidMap>(PersistentCriteriaEvaluationBehavior.InTransaction,
                source.Session, criteria);
            return result;
        }

       
    }
}

