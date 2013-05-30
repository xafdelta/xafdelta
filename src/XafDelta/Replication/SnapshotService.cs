using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using DevExpress.Xpo.Metadata.Helpers;
using XafDelta.Exceptions;
using XafDelta.Localization;
using XafDelta.Messaging;
using ObjectSpace = XafDelta.Protocol.ObjectSpace;

namespace XafDelta.Replication
{
    internal class SnapshotService : BaseService
    {
        public SnapshotService(XafDeltaModule owner) : base(owner)
        {
        }

        #region Build

        public bool BuildSnapshot(ActionWorker worker, ReplicationNode targetNode)
        {
            if (targetNode == null) throw new ArgumentNullException("targetNode");
            if (targetNode.Session.IsNewObject(targetNode)) throw new UnsavedTargetNodeException();
            if (targetNode.Disabled) throw new DisabledTargetNodeException();

            var result = false;
            worker.ReportProgress(string.Format(Localizer.BuildingSnapshotForNode, targetNode.Name));

            XafDeltaModule.Instance.LoadService.BeginLoad();

            using (var appObjectSpace = XafDeltaModule.XafApp.CreateObjectSpace())
            {
                targetNode = appObjectSpace.GetObjectByKey<ReplicationNode>(targetNode.Oid);
                Owner.DoBeforeBuildPackages(new BeforeBuildPackagesArgs(appObjectSpace));
                var package = Owner.MessagingService.CreateOutputPackage(appObjectSpace, targetNode,
                                                                         PackageType.Snapshot);
                try
                {
                    var doneObjectsCount = 0;
                    using (var snapObjectSpace = new ObjectSpace(package.UnitOfWork))
                    {
                        var rootContext = new BuildContext(appObjectSpace, snapObjectSpace, targetNode, worker);

                        foreach (var modelClass in XafDeltaModule.XafApp.Model.BOModel.Where(x => !x.NonSnapshot())
                            .TakeWhile(x => !worker.CancellationPending))
                        {
                            if (!modelClass.TypeInfo.IsAbstract && modelClass.TypeInfo.IsPersistent
                                && (modelClass.TypeInfo.IsInterface || modelClass.TypeInfo.Implements<IXPObject>()))
                                doneObjectsCount += buildClassSnapshot(new ClassBuildContext(rootContext, modelClass));
                        }
                        if (doneObjectsCount > 0 && !worker.CancellationPending)
                        {
                            worker.ReportProgress(Localizer.CommitChanges);
                            snapObjectSpace.CommitChanges();
                            //snapshotPostBuild(snapObjectSpace);
                            snapObjectSpace.CommitChanges();
                            createSnapshotMaps(rootContext);
                            worker.ReportProgress(Localizer.CommitChanges);
                            snapObjectSpace.CommitChanges();
                            worker.ReportProgress(string.Format(Localizer.TotalObjectsSnapshoted,
                                                                doneObjectsCount));
                            result = true;
                        }
                        else
                        {
                            snapObjectSpace.Rollback();
                            if (worker.CancellationPending)
                                worker.ReportProgress(Color.DarkMagenta, Localizer.Aborted);
                            else
                                worker.ReportProgress(Color.DarkMagenta, Localizer.NoObjectsFoundForSnapshot);
                        }
                    }

                    package.CloseUnitOfWork(doneObjectsCount > 0 && !worker.CancellationPending);

                    result &= !worker.CancellationPending;

                    if (result)
                        Owner.OnAfterBuildSnapshot(new AfterBuildSnapshotArgs(targetNode));

                }
                catch (Exception exception)
                {
                    worker.ReportError(Localizer.SnapshotFailed, exception.Message);
                    result = false;
                }

                // on success, commit changes to app database and package storage
                if (result)
                {
                    package.CreateLogRecord(PackageEventType.Created);
                    appObjectSpace.CommitChanges();
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

        /// <summary>
        /// Creates the snapshot maps.
        /// </summary>
        /// <param name="context">The context.</param>
        private void createSnapshotMaps(BuildContext context)
        {
            context.Worker.ReportProgress(Localizer.BuildObjectMaps);
            var i = 0;
            while (context.DoneObjects.Count > 0)
            {
                context.Worker.ReportPercent((double) i/(double) context.DoneObjects.Count);
                var sourceObject = context.DoneObjects.Dequeue();
                var snapObject = context.Map[sourceObject];
                SnapshotOidMap.Create(sourceObject, snapObject).OrdNo = i++;
            }
        }

        /// <summary>
        /// Builds the class snapshot.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private int buildClassSnapshot(ClassBuildContext context)
        {
            var snapSourceList = getSnapShourceList(context);
            var result = snapSourceList.Count;
            if (result > 0)
                context.Worker.ReportProgress(string.Format(Localizer.SourceObjectsFoundForClass,
                                                            result, context.ModelClass.Name));
            int i = 0;
            foreach (var sourceObject in snapSourceList.TakeWhile(x => !context.Worker.CancellationPending))
            {
                context.Worker.ReportPercent((double) i++/(double) result);
                buildObjectSnapshot(context, sourceObject);
            }

            return result;
        }

        /// <summary>
        /// Builds the object snapshot.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceObject">The source object.</param>
        /// <returns></returns>
        private object buildObjectSnapshot(ClassBuildContext context, object sourceObject)
        {
            if (!isSourceObject(context, sourceObject) || context.Worker.CancellationPending) return null;
            object snapObject;

            if (!context.Map.TryGetValue(sourceObject, out snapObject))
            {
                var modelClass = XafDeltaModule.XafApp.FindModelClass(sourceObject.GetType());
                snapObject = context.SnapObjectSpace.CreateObject(modelClass.TypeInfo.Type);
                storeObjectState(context, sourceObject, snapObject);
                context.DoneObjects.Enqueue(sourceObject);
            }
            return snapObject;
        }

        /// <summary>
        /// Determines whether source object is source for snapshot.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceObject">The source object.</param>
        /// <returns>
        ///   <c>true</c> if the source object is source for snapshort; otherwise, <c>false</c>.
        /// </returns>
        private bool isSourceObject(ClassBuildContext context, object sourceObject)
        {
            var modelClass = XafDeltaModule.XafApp.FindModelClass(sourceObject.GetType());
            var sourceContext = new ClassBuildContext(context, modelClass);
            var result = !modelClass.NonSnapshot() && getSnapShourceList(sourceContext).Contains(sourceObject);
            return result;
        }

        /// <summary>
        /// Stores the state of the object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="sourceObject">The source object.</param>
        /// <param name="snapObject">The snap object.</param>
        private void storeObjectState(ClassBuildContext context, object sourceObject, object snapObject)
        {
            if (context.Worker.CancellationPending) return;

            if (sourceObject == null || snapObject == null) return;
            if (context.Map.ContainsKey(sourceObject)) return;

            context.Map.Add(sourceObject, snapObject);
            context.Worker.ShowStatus(string.Format(Localizer.SnapshotObject, sourceObject));

            var classInfo = ((IXPObject) sourceObject).ClassInfo;
            var modelClass = XafDeltaModule.XafApp.FindModelClass(classInfo.ClassType);

            IEnumerable<XPMemberInfo> sourceMembers = classInfo.Members;
            if (modelClass != null && modelClass.TypeInfo.IsInterface)
                sourceMembers = from m in modelClass.AllMembers select classInfo.FindMember(m.Name);

            foreach (var memberInfo in sourceMembers.Where(x => !(x is ServiceField) && x.IsPersistent
                                                                && !x.IsReadOnly && !x.IsKey).TakeWhile(
                                                                    x => !context.Worker.CancellationPending))
            {
                if (modelClass != null)
                {
                    var modelMember = modelClass.AllMembers[memberInfo.Name];
                    if (modelMember != null && modelMember.NonSnapshot())
                        continue;
                }

                context.Worker.ShowStatus(string.Format(Localizer.SnapshotProperty, sourceObject, memberInfo.Name));

                var sourceValue = memberInfo.GetValue(sourceObject);
                var memberIsCloned = false;

                // XP object references
                if (memberInfo.ReferenceType != null)
                {
                    // aggregated objects
                    if (memberInfo.IsAggregated && sourceValue != null)
                    {
                        var aggrObj = memberInfo.GetValue(snapObject);
                        // if aggregated object in target is not null
                        // then link it to source value and clone state
                        if (aggrObj != null)
                        {
                            storeObjectState(context, sourceValue, aggrObj);
                            memberIsCloned = true;
                        }
                    }

                    if (sourceValue != null && !memberIsCloned)
                    {
                        memberInfo.SetValue(snapObject, buildObjectSnapshot(context, sourceValue));
                        memberIsCloned = true;
                        context.Worker.ShowStatus(string.Format(Localizer.SnapshotObject, sourceObject));
                    }
                }

                if (!memberIsCloned)
                    memberInfo.SetValue(snapObject, sourceValue);
            }

            // clone collections
            if (modelClass != null)
            {
                foreach (var modelMember in modelClass.AllMembers.Where(x => x.MemberInfo.IsAssociation
                                                                             && x.MemberInfo.IsList).TakeWhile(
                                                                                 x =>
                                                                                 !context.Worker.CancellationPending))
                {
                    if (modelMember == null || modelMember.NonSnapshot())
                        continue;

                    context.Worker.ShowStatus(string.Format(Localizer.SnapshotProperty, sourceObject, modelMember.Name));

                    var sourceCollection = modelMember.MemberInfo.GetValue(sourceObject) as IList;
                    var targetMember = ((IXPObject) snapObject).ClassInfo.GetMember(modelMember.Name);

                    var targetCollection = targetMember.GetValue(snapObject) as IList;
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

        /// <summary>
        /// Gets objects to be snapshoted.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private IList<object> getSnapShourceList(ClassBuildContext context)
        {
            IList<object> result;
            if (!context.SnapSources.TryGetValue(context.ModelClass, out result))
            {
                IEnumerable<object> resultEnum = new List<object>();
                var classType = context.ModelClass.TypeInfo.Type;
                var tuple = from c in context.AppObjectSpace.GetObjects(classType).Cast<object>() select c;

                if (Owner.SelectorMode == SelectorMode.BlackList)
                    resultEnum = tuple;

                foreach (var selector in context.ModelClass.AllSnapshotSelectors())
                {
                    IEnumerable<object> selectedObjects;
                    if (string.IsNullOrEmpty(selector.Expression))
                        selectedObjects = tuple;
                    else
                        selectedObjects =
                            from c in
                                context.AppObjectSpace.GetObjects(classType, CriteriaOperator.Parse(selector.Expression))
                                .Cast<object>()
                            select c;

                    resultEnum = selector.SelectorType == SelectorType.Include
                                     ? resultEnum.Union(selectedObjects)
                                     : resultEnum.Except(selectedObjects);

                }
                result =
                    resultEnum.Except(context.DoneObjects).Where(
                        x => XafDeltaModule.XafApp.FindModelClass(x.GetType()) == context.ModelClass).ToList();
                context.SnapSources.Add(context.ModelClass, result);
            }
            return result;
        }

        #endregion

        #region Load

        public string LoadSnapshotPackage(LoadPackageContext context)
        {
            var result = string.Empty;

            XafDeltaModule.Instance.LoadService.BeginLoad();

            try
            {
                using (var snapshotObjectSpace = new ObjectSpace(context.Package.UnitOfWork))
                {
                    // create root context
                    var sourceMaps = snapshotObjectSpace.GetObjects<SnapshotOidMap>().OrderBy(x => x.OrdNo).ToList();
                    var rootContext = new SnapLoadContext(context, sourceMaps);

                    context.Worker.ReportProgress(string.Format(Localizer.ObjectsFoundInSnapshot, sourceMaps.Count));
                    var i = 0;
                    // restore all of object from snapshot
                    foreach (var sourceMap in sourceMaps.TakeWhile(x => !context.Worker.CancellationPending))
                    {
                        context.Worker.ReportPercent((double) i++/(double) sourceMaps.Count);
                        context.Worker.ShowStatus(string.Format(Localizer.SnapshotObject, sourceMap.Target.Target));
                        loadSnapshotObject(rootContext, sourceMap);
                    }

                    if (!context.Worker.CancellationPending)
                    {
                        // fix OidMap references
                        var newMaps = from c in context.ObjectSpace.ModifiedObjects.Cast<object>()
                                      where c is OidMap && ((OidMap) c).NewObject != null
                                      select c as OidMap;

                        context.ObjectSpace.CommitChanges();
                        newMaps.ToList().ForEach(x => x.FixReference());
                        snapshotPostLoad(context.ObjectSpace);
                        restoreLastLoadedNum(context, snapshotObjectSpace);
                        context.ObjectSpace.CommitChanges();
                        snapshotObjectSpace.Rollback();
                    }
                }
            }
            catch (Exception exception)
            {
                result = string.Format(Localizer.SnapshotLoadingIsFailed, exception.Message);
                context.Worker.ReportError(result);
            }

            XafDeltaModule.Instance.LoadService.EndLoad();

            if (string.IsNullOrEmpty(result))
            {
                if (context.Worker.CancellationPending)
                    result = Localizer.LoadingAborted;

                context.Worker.ReportProgress(Color.Blue, Localizer.SnapshotLoadingIs,
                                              (context.Worker.CancellationPending
                                                   ? Localizer.Aborted
                                                   : Localizer.Finished));
            }

            return result;
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
        private object loadSnapshotObject(SnapLoadContext context, SnapshotOidMap sourceMap)
        {
            // look for existing object in app database
            var result = OidMap.FindApplicationObject(context.ObjectSpace, sourceMap, context.Package.SenderNodeId);

            // if not found then create new one
            if (result == null)
            {
                var typeInfo = context.ObjectSpace.TypesInfo.FindTypeInfo(sourceMap.StoredClassName);
                result = context.ObjectSpace.CreateObject(typeInfo.Type);
            }
            // register mapping
            updateObjectMapping(context, sourceMap, result);

            // restore object's state from snapshot
            restoreSnapshotObjectState(context, (IXPObject) sourceMap.Target.Target, (IXPObject) result);
            return result;
        }

        /// <summary>
        /// Restores the state of the snapshot object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="snapshotObject">The snapshot object.</param>
        /// <param name="appObject">The app object.</param>
        private void restoreSnapshotObjectState(SnapLoadContext context, IXPObject snapshotObject,
                                                IXPObject appObject)
        {
            if (context.RestoredObjects.Contains(snapshotObject)) return;
            context.RestoredObjects.Add(snapshotObject);

            var snapClassInfo = snapshotObject.ClassInfo;
            var modelClass = XafDeltaModule.XafApp.FindModelClass(snapClassInfo.ClassType);
            var members = from m in snapClassInfo.Members select m;

            // restore non collection properties
            foreach (var snapMemberInfo in members.Where(x => x.IsPersistent && !x.IsReadOnly && !(x is ServiceField) && !x.IsKey))
            {
                // skip non snapshot members
                if (modelClass != null)
                {
                    var modelMember = modelClass.FindMember(snapMemberInfo.Name);
                    if (modelMember != null && modelMember.NonSnapshot())
                        continue;
                }

                var appMemberInfo = appObject.ClassInfo.GetMember(snapMemberInfo.Name);
                // skip unknown and readonly properties
                if (appMemberInfo == null || appMemberInfo.IsReadOnly) continue;

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
                            restoreSnapshotObjectState(context, (IXPObject) snapValue, (IXPObject) aggrObj);
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
                                                                            &&
                                                                            (x.IsManyToMany || x.IsManyToManyAlias ||
                                                                             x.IsCollection || x.IsAssociationList)))
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
        private void updateObjectMapping(SnapLoadContext context,
                                         IPackageObjectReference sourceMap, object appObject)
        {
            if (context.UpdatedMapping.Contains(appObject)) return;
            context.UpdatedMapping.Add(appObject);

            foreach (var mappingStr in sourceMap.KnownMapping.Split(new[] {'\n'},
                                                                    StringSplitOptions.RemoveEmptyEntries))
            {
                var mapArray = mappingStr.Split('\a');
                var nodeId = mapArray[0];
                var objectId = mapArray[1];
                var oidMap = OidMap.GetOidMap(appObject, nodeId);
                if (oidMap == null)
                    OidMap.CreateOidMap(context.ObjectSpace, objectId, nodeId, (IXPObject) appObject);
            }
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
            if (sender != null)
            {
                sender.LastLoadedPackageNumber = marker.LastSavedPackageNumber;
            }
        }

        #endregion

        #region Context classes

        internal class BuildContext
        {
            public IObjectSpace AppObjectSpace { get; set; }
            public ObjectSpace SnapObjectSpace { get; set; }
            public ReplicationNode TargetNode { get; set; }
            public ActionWorker Worker { get; set; }
            public Queue<object> DoneObjects { get; set; }
            public Dictionary<object, object> Map { get; set; }
            public Dictionary<IModelClass, IList<object>> SnapSources { get; set; }

            public BuildContext(IObjectSpace appObjectSpace, ObjectSpace snapObjectSpace, ReplicationNode targetNode,
                                ActionWorker worker)
            {
                AppObjectSpace = appObjectSpace;
                SnapObjectSpace = snapObjectSpace;
                TargetNode = targetNode;
                Worker = worker;
                DoneObjects = new Queue<object>();
                Map = new Dictionary<object, object>();
                SnapSources = new Dictionary<IModelClass, IList<object>>();
            }
        }

        internal class ClassBuildContext : BuildContext
        {
            public IModelClass ModelClass { get; set; }

            public ClassBuildContext(BuildContext rootContext, IModelClass modelClass) 
                : base(rootContext.AppObjectSpace, rootContext.SnapObjectSpace, 
                rootContext.TargetNode, rootContext.Worker)
            {
                ModelClass = modelClass;
                DoneObjects = rootContext.DoneObjects;
                SnapSources = rootContext.SnapSources;
                Map = rootContext.Map;
            }
        }

        public class SnapLoadContext : LoadPackageContext
        {
            public SnapLoadContext(LoadPackageContext context, IEnumerable<SnapshotOidMap> sourceMaps)
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
}
