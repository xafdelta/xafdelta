using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using ICSharpCode.SharpZipLib.Zip;
using XafDelta.Localization;
using XafDelta.Messaging;
using XafDelta.Replication;

namespace XafDelta
{
    /// <summary>
    /// Actions view controller
    /// </summary>
    public partial class VcActions : ViewController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VcActions"/> class.
        /// </summary>
        public VcActions()
        {
            InitializeComponent();
            RegisterActions(components);
        }

        private void VcActions_Activated(object sender, EventArgs e)
        {
            var isActive = View.ObjectTypeInfo != null && View.ObjectTypeInfo.Implements<IReplicationMessage>();
            ImportFilesAction.Active["TargetType"] = isActive;
            WinImportFilesAction.Active["TargetType"] = isActive;
            ExportFilesAction.Active["TargetType"] = isActive;

            var platform = GetPlatform("");

            ImportFilesAction.Enabled["XafDeltaPlatform assigned"] = platform != null;
            ExportFilesAction.Enabled["XafDeltaPlatform assigned"] = platform != null;
            BuildPackageAction.Enabled["XafDeltaPlatform assigned"] = platform != null;
            BuildSnapshotAction.Enabled["XafDeltaPlatform assigned"] = platform != null;
            LoadAllAction.Enabled["XafDeltaPlatform assigned"] = platform != null;
            LoadPackageAction.Enabled["XafDeltaPlatform assigned"] = platform != null;

            if (platform != null)
            {
                WinImportFilesAction.Active["IsWinApp"] = (platform is IXafDeltaWinFormsPlatform);
                ImportFilesAction.Active["IsWinApp"] = (platform is IXafDeltaWebPlatform);
            }
        }

        /// <summary>
        /// Gets the platform.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns>Current XafDelta platform</returns>
        public IXafDeltaPlatform GetPlatform(string actionName)
        {
            var args = new GetPlatformArgs();
            XafDeltaModule.Instance.OnGetPlatform(args);
            if (args.XafDeltaPlatform == null) throw new ApplicationException("XafDeltaPlatform is null");
            if (args.XafDeltaPlatform.ActionIsBusy(actionName)) args.XafDeltaPlatform = null;
            return args.XafDeltaPlatform;
        }

        private void actionComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            View.ObjectSpace.Refresh();
        }

        #region Import

        private List<string> getImportExtList()
        {
            var extList = new List<string>();
            if (View.ObjectTypeInfo.Type == typeof(Package))
            {
                extList.Add(Package.PackageFileExtension);
            }
            else if (View.ObjectTypeInfo.Type == typeof(Ticket))
            {
                extList.Add(Ticket.TicketFileExtension);
            }
            return extList;
        }

        private void WinImportFilesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var extList = getImportExtList();
            var platform = GetPlatform(e.Action.Id);
            if (platform != null && platform is IXafDeltaWinFormsPlatform)
            {
                Dictionary<string, byte[]> files;
                if (((IXafDeltaWinFormsPlatform)platform).SelectFilesToImport(extList, out files))
                    platform.ExecuteAction(e.Action.Id, importFilesExec, new FilesWorker(files), actionComplete);
            }
        }

        private void ImportFilesAction_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var extList = getImportExtList();
            var objectSpace = Application.CreateObjectSpace();
            e.View = ((IXafDeltaWebPlatform) GetPlatform("")).CreateImportDetailView(objectSpace, extList);
        }

        private void ImportFilesAction_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var platform = GetPlatform(e.Action.Id);
            if (platform != null)
            {
                var files = new Dictionary<string, byte[]>();
                ((IXafDeltaWebPlatform) platform).GetFilesToImport(files, e.PopupWindow.View);
                platform.ExecuteAction(e.Action.Id, importFilesExec, new FilesWorker(files), actionComplete);
            }
        }

        private void importFilesExec(object sender, DoWorkEventArgs e)
        {
            var worker = (FilesWorker) e.Argument;
            worker.ReportProgress(string.Format(Localizer.FilesSelectedForImport, worker.Files.Keys.Count));
            using (var objectSpace = Application.CreateObjectSpace())
            {
                foreach (var fileName in worker.Files.Keys.TakeWhile(x => !worker.CancellationPending))
                {
                    worker.ReportProgress(string.Format(Localizer.ImportingFile, fileName));
                    var fileExt = Path.GetExtension(fileName);
                    Debug.Assert(fileExt != null, "fileExt != null");

                    if (fileExt.ToLower() == Package.PackageFileExtension)
                    {
                        if (objectSpace.FindObject<Package>(CriteriaOperator.Parse("FileName = ?", fileName)) == null)
                            Package.ImportFromBytes(objectSpace, fileName, worker.Files[fileName]);
                        else
                            worker.ReportProgress(Color.BlueViolet, 
                                Localizer.ImportPackageExists, fileName);
                    }
                    else if (fileExt.ToLower() == Ticket.TicketFileExtension)
                    {
                        if (objectSpace.FindObject<Ticket>(CriteriaOperator.Parse("FileName = ?", fileName)) == null)
                            Ticket.ImportTicket(objectSpace, worker.Files[fileName]);
                        else
                            worker.ReportProgress(Color.BlueViolet, 
                                Localizer.ImportTicketExists, fileName);
                    }
                    else
                    {
                        worker.ReportError(Localizer.ImportInvalidFileExtension, fileName);
                    }
                    objectSpace.CommitChanges();
                }
            }

            if (worker.CancellationPending)
                worker.ReportProgress(Color.BlueViolet, Localizer.ImportAborted);

            worker.ReportProgress(Color.Blue, Localizer.ImportFinished);
        }


        #endregion

        #region Export

        private void ExportFilesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var source = View.SelectedObjects.Cast<IReplicationMessage>().ToList();
            var platform = GetPlatform(e.Action.Id);
            if (platform != null && source.Count > 0)
            {
                string dirName = "";
                if (!(platform is IXafDeltaWinFormsPlatform) 
                    || ((IXafDeltaWinFormsPlatform) platform).SelectExportDirectory(out dirName))
                        platform.ExecuteAction(e.Action.Id, exportFilesExec, 
                            new ExportWorker(dirName, source), actionComplete);
            }
        }

        private void exportFilesExec(object sender, DoWorkEventArgs e)
        {
            var worker = (ExportWorker) e.Argument;
            worker.ReportProgress(string.Format(Localizer.FilesSelectedForExport, worker.Source.Count()));
            var platform = GetPlatform("");

            using (var objectSpace = Application.CreateObjectSpace())
            {
                var fileName = string.Empty;
                byte[] fileData = null;
                if(worker.Source.Count() > 1)
                {
                    fileData = zipMessages(worker.Source);
                    fileName = "XafDeltaExport" + DateTime.UtcNow.ToString("o").Replace(":", "") + ".zip";
                }
                else if(worker.Source.Count() == 1)
                {
                    fileName = worker.Source.ElementAt(0).FileName;
                    fileData = worker.Source.ElementAt(0).GetData();
                }

                worker.ReportProgress(string.Format(Localizer.ExportingFile, fileName));
                platform.ExportFile(worker.ExportDir, fileName, fileData);
                objectSpace.CommitChanges();
            }


            if (worker.CancellationPending)
                worker.ReportProgress(Color.BlueViolet, Localizer.ExportAborted);

            worker.ReportProgress(Color.Blue, Localizer.ExportFinished);
        }

        private byte[] zipMessages(IEnumerable<IReplicationMessage> source)
        {
            byte[] result;
            using (var ms = new MemoryStream())
            using (var zip = new ZipOutputStream(ms))
            {
                zip.SetLevel(9);
                foreach (var message in source)
                {
                    var entry = (new ZipEntryFactory()).MakeFileEntry(message.FileName);
                    entry.DateTime = DateTime.UtcNow;
                    zip.PutNextEntry(entry);
                    var buffer = message.GetData();
                    zip.Write(buffer, 0, buffer.Length);
                }
                zip.Finish();
                zip.Close();
                ms.Close();
                result = ms.ToArray();
            }
            return result;
        }

        #endregion

        #region Load package

        private void LoadPackageAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var source = View.SelectedObjects.Cast<Package>().OrderBy(x => x.PackageDateTime).ToList();
            var platform = GetPlatform(e.Action.Id);
            if(platform != null)
                platform.ExecuteAction(e.Action.Id, loadPackageExec, new LoadPackageWorker(source), actionComplete);
        }

        private void loadPackageExec(object sender, DoWorkEventArgs e)
        {
            var worker = (LoadPackageWorker) e.Argument;
            worker.ReportProgress(string.Format(Localizer.SelectedForLoading, worker.Source.Count()));
            using (var objectSpace = Application.CreateObjectSpace())
            {
                foreach (var package in worker.Source.TakeWhile(x => !worker.CancellationPending))
                {
                    XafDeltaModule.Instance.LoadService.LoadPackage(worker, package);
                    objectSpace.CommitChanges();
                }
            }

            if (worker.CancellationPending)
                worker.ReportProgress(Color.BlueViolet, Localizer.LoadingAborted);
        }

        #endregion

        #region BuildPackage

        private void BuildPackage_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var platform = GetPlatform(e.Action.Id);
            if (platform != null)
                platform.ExecuteAction(e.Action.Id, buildPackageExec, new ActionWorker(), actionComplete);
        }

        private void buildPackageExec(object sender, DoWorkEventArgs e)
        {
            var worker = (ActionWorker) e.Argument;
            XafDeltaModule.Instance.ProtocolReplicationService.BuildPackages(worker);
        }

        #endregion

        #region LoadAll

        private void LoadAllAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var platform = GetPlatform(e.Action.Id);
            if (platform != null)
                platform.ExecuteAction(e.Action.Id, loadAllExec, new ActionWorker(), actionComplete);
        }

        private void loadAllExec(object sender, DoWorkEventArgs e)
        {
            var worker = (ActionWorker) e.Argument;
            XafDeltaModule.Instance.LoadService.Load(worker);
        }

        #endregion

        #region BuildSnapshot

        private void BuildSnapshotAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var platform = GetPlatform(e.Action.Id);
            if (platform != null)
            {
                var target = (ReplicationNode) View.CurrentObject;
                if (target == null) return;
                platform.ExecuteAction(e.Action.Id, buildSnapshotExec, new SnapshotWorker(target), actionComplete);
            }
        }

        private void buildSnapshotExec(object sender, DoWorkEventArgs e)
        {
            var worker = (SnapshotWorker)e.Argument;
            XafDeltaModule.Instance.SnapshotService.BuildSnapshot(worker, worker.ReplicationNode);
        }


        private void BuildSnapshotActionPop_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var obs = Application.CreateObjectSpace();
            var target = obs.CreateObject<SnapshotTarget>();
            var targetView = Application.CreateDetailView(obs, target);
            targetView.ViewEditMode = ViewEditMode.Edit;
            e.View = targetView;
        }


        private void BuildSnapshotActionPop_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var target = (SnapshotTarget) e.PopupWindow.View.CurrentObject;
            var platform = GetPlatform(e.Action.Id);
            if (platform != null)
            {
                if (target != null && target.TargetNode != null)
                {
                    platform.ExecuteAction(e.Action.Id, buildSnapshotExec, 
                        new SnapshotWorker(target.TargetNode), actionComplete);
                }
            }
        }

        #endregion
    }

    #region Typed workers

    /// <summary>
    /// ActionWorker for snapshots
    /// </summary>
    internal class SnapshotWorker : ActionWorker
    {
        /// <summary>
        /// Gets the snapshot replication node.
        /// </summary>
        public ReplicationNode ReplicationNode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapshotWorker"/> class.
        /// </summary>
        /// <param name="replicationNode">The replication node.</param>
        public SnapshotWorker(ReplicationNode replicationNode)
        {
            ReplicationNode = replicationNode;
        }
    }

    /// <summary>
    /// ActionWorker for package loading
    /// </summary>
    internal class LoadPackageWorker : ActionWorker
    {
        /// <summary>
        /// Gets or sets the list of replicas for load.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public List<Package> Source { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadPackageWorker"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public LoadPackageWorker(List<Package> source)
        {
            Source = source;
        }
    }

    /// <summary>
    /// ActionWorker for export
    /// </summary>
    internal class ExportWorker : ActionWorker
    {
        /// <summary>
        /// Gets the export directory.
        /// </summary>
        public string ExportDir { get; private set; }

        /// <summary>
        /// Gets the list of messages for export.
        /// </summary>
        public IEnumerable<IReplicationMessage> Source { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportWorker"/> class.
        /// </summary>
        /// <param name="exportDir">The export dir.</param>
        /// <param name="source">The source.</param>
        public ExportWorker(string exportDir, IEnumerable<IReplicationMessage> source)
        {
            ExportDir = exportDir;
            Source = source;
        }
    }

    /// <summary>
    /// ActionWorker for working with files
    /// </summary>
    internal class FilesWorker : ActionWorker
    {
        /// <summary>
        /// Gets the files list.
        /// </summary>
        public Dictionary<string, byte[]> Files { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilesWorker"/> class.
        /// </summary>
        /// <param name="files">The files.</param>
        public FilesWorker(Dictionary<string, byte[]> files)
        {
            Files = files;
        }
    }

    #endregion

}
