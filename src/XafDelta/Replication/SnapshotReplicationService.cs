using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata.Helpers;
using XafDelta.Exceptions;
using XafDelta.Localization;
using XafDelta.Messaging;
using XafDelta.ReadOnlyParameters;

namespace XafDelta.Replication
{
    /// <summary>
    /// Snapshot replication service. Depricated. Out of order.
    /// </summary>
    internal sealed class SnapshotReplicationService : BaseService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotReplicationService"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public SnapshotReplicationService(XafDeltaModule owner) : base(owner)
        {
        }

        #region Load

        /// <summary>
        /// Loads the snapshot package.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public string LoadSnapshotPackage(LoadPackageContext context)
        {
            var result = string.Empty;

            try
            {
                using (var snapshotObjectSpace = new ObjectSpace(context.Package.UnitOfWork))
                {
                    // create root context
                    var sourceMaps = snapshotObjectSpace.GetObjects<SnapshotOidMap>();
                    var rootContext = new SnapshotLoadContext(context, sourceMaps);

                    context.Worker.ReportProgress(string.Format(Localizer.ObjectsFoundInSnapshot, sourceMaps.Count));

                    // restore all of object from snapshot
                    foreach (var sourceMap in sourceMaps)
                        loadSnapshotObject(rootContext, sourceMap);

                    // fix OidMap references
                    var newMaps = from c in context.ObjectSpace.ModifiedObjects.Cast<object>()
                                  where c is OidMap && ((OidMap)c).NewObject != null
                                  select c as OidMap;
                    context.ObjectSpace.CommitChanges();
                    newMaps.ToList().ForEach(x => x.FixReference());
                    snapshotPostLoad(context.ObjectSpace);
                    restoreLastLoadedNum(context, snapshotObjectSpace);
                    context.ObjectSpace.CommitChanges();
                    snapshotObjectSpace.Rollback();
                }
            }
            catch (Exception exception)
            {
                result = string.Format(Localizer.SnapshotLoadingIsFailed, exception.Message);
                context.Worker.ReportError( result);
            }

            if (string.IsNullOrEmpty(result))
            {
                if (context.Worker.CancellationPending)
                    result = Localizer.LoadingAborted;

                context.Worker.ReportProgress(Color.Blue, Localizer.SnapshotLoadingIs, 
                                                          (context.Worker.CancellationPending ? Localizer.Aborted : Localizer.Finished));
            }

            return result;
        }

        /// <summary>
        /// Restore the last loaded number from marker.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="snapshotObjectSpace">The snapshot object space.</param>
        private static void restoreLastLoadedNum(LoadPackageContext context, IObjectSpace snapshotObjectSpace)
        {
            var marker = PackageMarker.GetInstance(snapshotObjectSpace);
            if (marker == null) return;
            var sender = ReplicationNode.FindNode(context.ObjectSpace, marker.SenderNodeId);
            if(sender != null)
            {
                sender.LastLoadedPackageNumber = marker.LastSavedPackageNumber;
            }
        }

        private void snapshotPostLoad(IObjectSpace objectSpace)
        {
            var schedEvents = objectSpace.GetObjects<Event>(null, true);
            schedEvents.ToList().ForEach(x =>
            {
                x.UpdateResourceIds();
                x.Save();
            });
            var args = new SnapshotPostOperationArgs(objectSpace);
            Owner.OnSnapshotPostLoad(args);
        }

        /// <summary>
        /// Loads the snapshot object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceMap">The source map.</param>
        /// <returns></returns>
        private object loadSnapshotObject(SnapshotLoadContext context, SnapshotOidMap sourceMap)
        {
            // look for existing object in app database
            var result = OidMap.FindApplicationObject(context.ObjectSpace, sourceMap, context.Package.SenderNodeId);

            // if not found then create new one
            if(result == null)
            {
                var typeInfo = context.ObjectSpace.TypesInfo.FindTypeInfo(sourceMap.Target.Target.GetType());
                result = context.ObjectSpace.CreateObject(typeInfo.Type);
            }
            // register mapping
            updateObjectMapping(context, sourceMap, result);

            // restore object's state from snapshot
            restoreSnapshotObjectState(context, (IXPObject)sourceMap.Target.Target, (IXPObject)result);
            return result;
        }

        /// <summary>
        /// Restores the state of the snapshot object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="snapshotObject">The snapshot object.</param>
        /// <param name="appObject">The app object.</param>
        private void restoreSnapshotObjectState(SnapshotLoadContext context, IXPObject snapshotObject, IXPObject appObject)
        {
            if(context.RestoredObjects.Contains(snapshotObject)) return;
            context.RestoredObjects.Add(snapshotObject);

            var snapClassInfo = snapshotObject.ClassInfo;
            var modelClass = XafDeltaModule.XafApp.FindModelClass(snapClassInfo.ClassType);

            // restore non collection properties
            foreach (var snapMemberInfo in snapClassInfo.Members.Where(x => x.IsPersistent 
                && !x.IsReadOnly && !(x is ServiceField) && !x.IsKey))
            {
                // skip non snapshot members
                if(modelClass != null)
                {
                    var modelMember = modelClass.FindMember(snapMemberInfo.Name);
                    if (modelMember != null && modelMember.NonSnapshot()) 
                        continue;
                }

                var appMemberInfo = appObject.ClassInfo.GetMember(snapMemberInfo.Name);
                // skip unknown and readonly properties
                if(appMemberInfo == null || appMemberInfo.IsReadOnly) continue;

                // new value grom snapshot
                var snapValue = snapMemberInfo.GetValue(snapshotObject);
                var memberIsRestored = false;

                // XPO references
                if (snapMemberInfo.ReferenceType != null)
                {
                    // aggregated objects
                    if (snapMemberInfo.IsAggregated && snapValue != null)
                    {
                        // app current aggregate value
                        var aggrObj = appMemberInfo.GetValue(appObject);
                        // if aggregated object in app is not null
                        // then link it to snapshot object and restore state
                        if (aggrObj != null)
                        {
                            updateObjectMapping(context, context.Maps[snapValue], aggrObj);
                            restoreSnapshotObjectState(context, (IXPObject)snapValue, (IXPObject)aggrObj);
                            memberIsRestored = true;
                        }
                    }

                    if (snapValue != null && !memberIsRestored)
                    {
                        var snapOidMap = context.Maps[snapValue];
                        appMemberInfo.SetValue(appObject, loadSnapshotObject(context, snapOidMap));
                        memberIsRestored = true;
                    }
                }

                if (!memberIsRestored)
                    appMemberInfo.SetValue(appObject, snapValue);
            }

            // restore collections state
            foreach (var snapMemberInfo in snapClassInfo.Members.Where(x => !x.Name.EndsWith("__Links") 
                && (x.IsManyToMany || x.IsManyToManyAlias || x.IsCollection || x.IsAssociationList)))
            {
                // skip non snapshot members
                if (modelClass != null)
                {
                    var modelMember = modelClass.FindMember(snapMemberInfo.Name);
                    if (modelMember != null && modelMember.MemberInfo.FindAttribute<NonSnapshotAttribute>() != null)
                        continue;
                }

                var snapCollection = snapMemberInfo.GetValue(snapshotObject) as IList;
                var appMemberInfo = appObject.ClassInfo.GetMember(snapMemberInfo.Name);
                var appCollection = appMemberInfo.GetValue(appObject) as IList;
                if (snapCollection != null && appCollection != null)
                    foreach (var snapElement in snapCollection)
                    {
                        if (context.Maps.ContainsKey(snapElement))
                        {
                            var appElement = loadSnapshotObject(context, context.Maps[snapElement]);
                            if (!appCollection.Contains(appElement))
                                appCollection.Add(appElement);
                        }
                    }
            }
        }

        /// <summary>
        /// Updates the object mapping.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceMap">The source map.</param>
        /// <param name="appObject">The app object.</param>
        private void updateObjectMapping(SnapshotLoadContext context, 
            IPackageObjectReference sourceMap, object appObject)
        {
            if(context.UpdatedMapping.Contains(appObject)) return;
            context.UpdatedMapping.Add(appObject);

            foreach (var mappingStr in sourceMap.KnownMapping.Split(new[]{'\n'}, 
                StringSplitOptions.RemoveEmptyEntries))
            {
                var mapArray = mappingStr.Split('\a');
                var nodeId = mapArray[0];
                var objectId = mapArray[1];
                var oidMap = OidMap.GetOidMap(appObject, nodeId);
                if(oidMap == null)
                    OidMap.CreateOidMap(context.ObjectSpace, objectId, nodeId, (IXPObject)appObject);
            }
        }

        #endregion

        #region Build

        /// <summary>
        /// Builds the snapshot.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="targetNode">The target node.</param>
        /// <returns></returns>
        public bool BuildSnapshot(ActionWorker worker, ReplicationNode targetNode)
        {
            if (targetNode == null) throw new ArgumentNullException("targetNode");
            if (targetNode.Session.IsNewObject(targetNode)) 
                throw new UnsavedTargetNodeException();
            if (targetNode.Disabled)
                throw new DisabledTargetNodeException();

            var result = false;
            worker.ReportProgress(string.Format(Localizer.BuildingSnapshotForNode, targetNode.Name));

            XafDeltaModule.Instance.LoadService.BeginLoad();

            using (var applicationObjectSpace = XafDeltaModule.XafApp.CreateObjectSpace())
            {
                

                targetNode = applicationObjectSpace.GetObjectByKey<ReplicationNode>(targetNode.Oid);
                Owner.DoBeforeBuildPackages(new BeforeBuildPackagesArgs(applicationObjectSpace));
                var package = Owner.MessagingService.CreateOutputPackage(applicationObjectSpace, targetNode,
                                                                            PackageType.Snapshot);
                try
                {
                    var doneObjects = new Dictionary<object, object>();
                    using (var snapshotObjectSpace = new ObjectSpace(package.UnitOfWork))
                    {
                        var mapStack = new Queue<object>();

                        var rootContext = new BuildContext(applicationObjectSpace, doneObjects,
                            snapshotObjectSpace, targetNode, worker, mapStack, 
                            new HashSet<string>(), new List<IModelClass>());

                        foreach (var modelClass in XafDeltaModule.XafApp.Model.BOModel.Where(x => !x.NonSnapshot())
                            .TakeWhile(x => !worker.CancellationPending))
                        {
                            if (!modelClass.TypeInfo.IsAbstract && modelClass.TypeInfo.IsPersistent 
                                && (modelClass.TypeInfo.IsInterface || modelClass.TypeInfo.Implements<IXPObject>()))
                                buildClassSnapshot(new ClassBuildContext(rootContext, modelClass));
                        }
                        if (doneObjects.Keys.Count > 0)
                        {
                            snapshotObjectSpace.CommitChanges();
                            snapshotPostBuild(snapshotObjectSpace);
                            snapshotObjectSpace.CommitChanges();
                            createSnapshotMaps(rootContext);
                            snapshotObjectSpace.CommitChanges();
                            worker.ReportProgress(string.Format(Localizer.TotalObjectsSnapshoted,
                                                                doneObjects.Keys.Count));
                            result = true;
                        }
                        else
                        {
                            snapshotObjectSpace.Rollback();
                            worker.ReportProgress(Color.DarkMagenta, Localizer.NoObjectsFoundForSnapshot);
                        }
                    }

                    package.CloseUnitOfWork(doneObjects.Keys.Count > 0);

                    result &= !worker.CancellationPending;

                    if (result)
                        Owner.OnAfterBuildSnapshot(new AfterBuildSnapshotArgs(targetNode));

                }
                catch (Exception exception)
                {
                    worker.ReportError( Localizer.SnapshotFailed, exception.Message);
                    result = false;
                }

                // on success, commit changes to app database and package storage
                if (result)
                {
                    package.CreateLogRecord(PackageEventType.Created);
                    applicationObjectSpace.CommitChanges();
                }
            }

            XafDeltaModule.Instance.LoadService.EndLoad();

            if (result)
            {
                worker.ReportProgress(Color.Blue, Localizer.SnapshotBuildingIs, 
                    (worker.CancellationPending ? Localizer.Aborted : Localizer.Finished));

            }

            return result;
        }

        private void snapshotPostBuild(ObjectSpace snapshotObjectSpace)
        {
            var schedEvents = snapshotObjectSpace.GetObjects<Event>(null, true);
            schedEvents.ToList().ForEach(x => 
            {
                x.UpdateResourceIds();
                x.Save();
            });
            var args = new SnapshotPostOperationArgs(snapshotObjectSpace);
            Owner.OnSnapshotPostBuild(args);
        }

        private void createSnapshotMaps(BuildContext context)
        {
            var i = 0;
            while(context.MapQueue.Count > 0)
            {
                var sourceObject = context.MapQueue.Dequeue();
                var targetObject = context.DoneObjects[sourceObject];
                SnapshotOidMap.Create(sourceObject, targetObject).OrdNo = i++;
            }
        }

        /// <summary>
        /// Builds the class snapshot.
        /// </summary>
        /// <param name="context">The context.</param>
        private void buildClassSnapshot(ClassBuildContext context)
        {
            // Prepare snapshot read only parameters
            SnapshotNodeParameter.Instance.SnapshotNode = context.TargetNode;
            SnapshotNodeIdParameter.Instance.SnapshotNodeId = context.TargetNode.NodeId;
            try
            {
                //var includeObjects = getSourceObjects(context, SelectorType.Include);
                //var excludeObjects = getSourceObjects(context, SelectorType.Exclude);

                //var sourceObjects = includeObjects.Except(excludeObjects).Where(x => !context.DoneObjects.ContainsKey(x)).ToList();

                var sourceObjects = getSourceObjects(context);

                if(sourceObjects.Count() > 0)
                    context.Worker.ReportProgress(string.Format(Localizer.SourceObjectsFoundForClass, sourceObjects.Count, context.ModelClass.Name));

                // for each selected objects build snapshots
                foreach (var sourceObject in sourceObjects.TakeWhile(x => !context.Worker.CancellationPending))
                    buildObjectSnapshot(context, sourceObject);
            }
            finally
            {
                SnapshotNodeParameter.Instance.SnapshotNode = null;
            }
        }


        private List<object> getSourceObjects(ClassBuildContext context)
        {
            return getSourceObjects(context, null);
        }

        private List<object> getSourceObjects(ClassBuildContext context, IModelClass modelClass)
        {
            if (modelClass == null)
                modelClass = context.ModelClass;

            IEnumerable<object> result = new List<object>();
            var targetType = modelClass.TypeInfo.Type;
            var allObjects = (from c in context.ObjectSpace.GetObjects(targetType).Cast<object>() select c).ToList();
            if (Owner.SelectorMode == SelectorMode.BlackList)
                result = allObjects;
            foreach (var selector in modelClass.AllSnapshotSelectors())
            {
                IEnumerable<object> selectResult;

                var expression = selector.Expression;
                if (string.IsNullOrEmpty(expression))
                    selectResult = allObjects;
                else
                    selectResult = context.ObjectSpace.GetObjects(targetType, CriteriaOperator.Parse(expression)).Cast<object>();

                if (selector.SelectorType == SelectorType.Include)
                    result = result.Union(selectResult).Distinct();
                else
                    result = result.Except(selectResult);
            }
            result = result.Where(x => !context.DoneObjects.ContainsKey(x)).Distinct();
            result = from c in result where !(c is ISnapshotable) 
                         || ((ISnapshotable)c).ShouldBeIncludedInSnapshot(context.TargetNode) select c;

            var resultList = result.ToList();

            // register class done
            context.SelectedTypes.Add(modelClass);

            foreach (var resultObject in resultList)
            {
                // store selected objects hashes
                if (XafDeltaModule.XafApp.FindModelClass(resultObject.GetType()) == modelClass)
                {
                    var seekKey = resultObject.GetType().AssemblyQualifiedName + 
                        context.ObjectSpace.GetKeyValue(resultObject);

                    context.SelectedSourceObjects.Add(seekKey);
                }
            }

            return resultList;
        }

        /// <summary>
        /// Builds object snapshot.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceObject">The source object.</param>
        /// <returns></returns>
        private object buildObjectSnapshot(ClassBuildContext context, object sourceObject)
        {
            object result;

            if (!availableForSnapshot(sourceObject, context)) return null;

            if (!context.DoneObjects.TryGetValue(sourceObject, out result))
            {
                var packageUow = ((ObjectSpace)context.PackageObjectSpace).Session;
                var classInfo = packageUow.GetClassInfo(sourceObject.GetType());
                var modelClass = XafDeltaModule.XafApp.FindModelClass(sourceObject.GetType());
                result = context.ObjectSpace.CreateObject(modelClass.TypeInfo.Type);
                // result = classInfo.CreateObject(packageUow);

                context.DoneObjects.Add(sourceObject, result);

                storeObjectState(context, sourceObject, result);
                context.MapQueue.Enqueue(sourceObject);
            }
            return result;
        }

        private bool availableForSnapshot(object sourceObject, ClassBuildContext context)
        {
            var result = context.DoneObjects.ContainsKey(sourceObject);
            if(!result)
            {
                var modelClass = XafDeltaModule.XafApp.FindModelClass(sourceObject.GetType());
                if(modelClass != null)
                {
                    if (!context.SelectedTypes.Contains(modelClass))
                        getSourceObjects(context, modelClass);
                    var seekKey = sourceObject.GetType().AssemblyQualifiedName +
                                  context.ObjectSpace.GetKeyValue(sourceObject);
                    result = context.SelectedSourceObjects.Contains(seekKey);
                }
            }
            return result;
        }

        /// <summary>
        /// Clones the state of the source object into target one.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceObject">The source object.</param>
        /// <param name="targetObject">The target object.</param>
        private void storeObjectState(ClassBuildContext context, object sourceObject, object targetObject)
        {
            var classInfo = ((IXPObject) sourceObject).ClassInfo;
            var modelClass = XafDeltaModule.XafApp.FindModelClass(classInfo.ClassType);

            foreach (var memberInfo in classInfo.Members.Where(x => !(x is ServiceField) && x.IsPersistent
                && !x.IsReadOnly && !x.IsKey))
            {
                if(modelClass != null)
                {
                    var modelMember = modelClass.AllMembers[memberInfo.Name];
                    if(modelMember != null && modelMember.NonSnapshot()) 
                        continue;
                }

                var sourceValue = memberInfo.GetValue(sourceObject);
                var memberIsCloned = false;

                // XP object references
                if (memberInfo.ReferenceType != null)
                {
                    // aggregated objects
                    if(memberInfo.IsAggregated && sourceValue != null)
                    {
                        var aggrObj = memberInfo.GetValue(targetObject);
                        // if aggregated object in target is not null
                        // then link it to source value and clone state
                        if (aggrObj != null)
                        {
                            if (!context.DoneObjects.ContainsValue(aggrObj))
                            {
                                if(!context.DoneObjects.ContainsKey(sourceValue))
                                    context.DoneObjects.Add(sourceValue, aggrObj);
                                storeObjectState(context, sourceValue, aggrObj);
                            }
                            memberIsCloned = true;
                        }
                    }

                    if(sourceValue != null && !memberIsCloned)
                    {
                        memberInfo.SetValue(targetObject, buildObjectSnapshot(context, sourceValue));
                        memberIsCloned = true;
                    }
                }

                if (!memberIsCloned)
                    memberInfo.SetValue(targetObject, sourceValue);
            }

            // clone collections
            if (modelClass != null)
            {
                foreach (var modelMember in modelClass.AllMembers.Where(x => x.MemberInfo.IsAssociation 
                    && x.MemberInfo.IsList))
                {
                    if (modelMember == null || modelMember.NonSnapshot())
                        continue;

                    var sourceCollection = modelMember.MemberInfo.GetValue(sourceObject) as IList;
                    var targetMember = ((IXPObject) targetObject).ClassInfo.GetMember(modelMember.Name);
                    
                    var targetCollection = targetMember.GetValue(targetObject) as IList;
                    if (sourceCollection != null && targetCollection != null)
                    {
                        var sourceObjectList = new List<object>();
                        sourceObjectList.AddRange(sourceCollection.Cast<object>());
                        foreach (var sourceElement in sourceObjectList)
                        {
                            var targeElement = buildObjectSnapshot(context, sourceElement);
                            if (targeElement != null && !targetCollection.Contains(targeElement))
                                targetCollection.Add(targeElement);
                        }
                    }
                }
            }
        }

        #endregion

        #region Execution context

        private class ClassBuildContext : BuildContext
        {
            public IModelClass ModelClass { get; private set; }

            public ClassBuildContext(BuildContext rootContext, IModelClass modelClass)
                : base(rootContext.ObjectSpace, rootContext.DoneObjects,
                    rootContext.PackageObjectSpace, rootContext.TargetNode,
                    rootContext.Worker, rootContext.MapQueue, rootContext.SelectedSourceObjects, 
                rootContext.SelectedTypes)
            {
                ModelClass = modelClass;
            }
        }

        private class BuildContext
        {
            public IObjectSpace ObjectSpace { get; private set; }
            public Dictionary<object, object> DoneObjects { get; private set; }
            public IObjectSpace PackageObjectSpace { get; private set; }
            public ReplicationNode TargetNode { get; private set; }
            public ActionWorker Worker { get; private set; }
            public Queue<object> MapQueue { get; private set; }
            public HashSet<string> SelectedSourceObjects { get; private set; }
            public List<IModelClass> SelectedTypes { get; private set; }



            public BuildContext(IObjectSpace objectSpace, Dictionary<object, object> doneObjects,
                IObjectSpace packageObjectSpace, ReplicationNode targetNode,
                ActionWorker worker, Queue<object> mapQueue, HashSet<string> selectedSourceObjects, 
                List<IModelClass> selectedTypes)
            {
                ObjectSpace = objectSpace;
                DoneObjects = doneObjects;
                PackageObjectSpace = packageObjectSpace;
                TargetNode = targetNode;
                Worker = worker;
                MapQueue = mapQueue;
                SelectedSourceObjects = selectedSourceObjects;
                SelectedTypes = selectedTypes;
            }
        }

        public class SnapshotLoadContext : LoadPackageContext
        {
            public SnapshotLoadContext(LoadPackageContext context, IEnumerable<SnapshotOidMap> sourceMaps)
                : base(context.Package, context.Worker, context.ObjectSpace, context.CurrentNodeId)
            {
                Maps = new Dictionary<object, SnapshotOidMap>();
                RestoredObjects = new List<object>();
                UpdatedMapping = new List<object>();
                var sourceList = sourceMaps.ToList();

                // fixReplicaDictionary(context, sourceList);
                sourceList.ForEach(x => Maps.Add(x.Target.Target, x));
            }

            public Dictionary<object, SnapshotOidMap> Maps { get; private set; }
            public List<object> RestoredObjects { get; private set; }
            public List<object> UpdatedMapping { get; private set; }
        }

        #endregion
    }


    /// <summary>
    /// Argument for <see cref="XafDeltaModule.AfterBuildSnapshot"/> event.
    /// </summary>
    public class AfterBuildSnapshotArgs : EventArgs
    {
        /// <summary>
        /// Gets the snapshot target node.
        /// </summary>
        public ReplicationNode TargetNode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AfterBuildSnapshotArgs"/> class.
        /// </summary>
        /// <param name="targetNode">The target node.</param>
        public AfterBuildSnapshotArgs(ReplicationNode targetNode)
        {
            TargetNode = targetNode;
        }
    }

    /// <summary>
    /// Argument for <see cref="XafDeltaModule.SnapshotPostBuild"/> and <see cref="XafDeltaModule.SnapshotPostLoad"/> events.
    /// </summary>
    public class SnapshotPostOperationArgs : EventArgs
    {
        /// <summary>
        /// Gets the snapshot object space.
        /// </summary>
        public IObjectSpace SnapshotObjectSpace { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotPostOperationArgs"/> class.
        /// </summary>
        /// <param name="snapshotObjectSpace">The snapshot object space.</param>
        public SnapshotPostOperationArgs(IObjectSpace snapshotObjectSpace)
        {
            SnapshotObjectSpace = snapshotObjectSpace;
        }
    }

}
