using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.Utils;

namespace XafDelta.Win
{
    /// <summary>
    /// XafDelta winforms module. Implements <see cref="IXafDeltaWinFormsPlatform"/> interface.
    /// </summary>
    [ToolboxItem(true)]
    [ToolboxTabName(XafAssemblyInfo.DXTabXafModules)]
    [Description("XafDelta winforms module.")]
    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [ToolboxBitmap(typeof(XafDeltaWinModule), "Resources.Delta.ico")]
    [ToolboxItemFilter("Xaf.Platform.Win")]
    public sealed partial class XafDeltaWinModule : ModuleBase, IXafDeltaWinFormsPlatform
    {
        public XafDeltaWinModule()
        {
            ModelDifferenceResourceName = "XafDelta.Win.Model.DesignedDiffs";
            InitializeComponent();
            Instance = this;
        }

        /// <summary>
        /// Gets the <see cref="XafDeltaWinModule"/> singleton.
        /// </summary>
        public static XafDeltaWinModule Instance { get; private set; }

        /// <summary>
        /// Sets up a module after it has been added to the <see cref="P:DevExpress.ExpressApp.XafApplication.Modules"/> collection.
        /// </summary>
        /// <param name="application">An <see cref="T:DevExpress.ExpressApp.XafApplication"/> object that provides methods and properties to manage the current application. This parameter value is set for the <see cref="P:DevExpress.ExpressApp.ModuleBase.Application"/> property.</param>
        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            if (!DesignMode && XafDeltaModule.Instance != null)
            {
                XafDeltaModule.Instance.GetPlatform += instanceGetPlatform;
                XafDeltaModule.Instance.Disposed += Instance_Disposed;
            }
        }

        void instanceGetPlatform(object sender, GetPlatformArgs e)
        {
            e.XafDeltaPlatform = this;
        }

        void Instance_Disposed(object sender, EventArgs e)
        {
            XafDeltaModule.Instance.GetPlatform -= instanceGetPlatform;
            XafDeltaModule.Instance.Disposed -= Instance_Disposed;
        }

        private readonly Dictionary<BackgroundWorker, ProgressForm> progressForms =
            new Dictionary<BackgroundWorker, ProgressForm>();

        private void progressChanged(object sender, ProgressChangedEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            if (!progressForms.ContainsKey(worker))
            {
                var form = new ProgressForm(worker);
                progressForms.Add(worker, form);
                form.Closed += formClosed;
                form.Show();
            }

            /* 11.2.7 */
            if (e.UserState == null)
                progressForms[worker].ShowPercent(e.ProgressPercentage);
            else
            {
                string text = e.UserState.ToString();
                if(e.ProgressPercentage == ActionWorker.StatusCode)
                    progressForms[worker].ShowStatus(text);
                else
                    progressForms[worker].AddProgessText(text, e.ProgressPercentage);
            }
        }

        void formClosed(object sender, EventArgs e)
        {
            progressForms.Remove(((ProgressForm)sender).Worker);
        }

        #region Implementation of IXafDeltaPlatform

        /// <summary>
        /// Selects the export directory while export messages.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        /// <returns></returns>
        public bool SelectExportDirectory(out string dirName)
        {
            bool result;
            dirName = string.Empty;
            using (var folderDialog = new FolderBrowserDialog())
            {
                result = folderDialog.ShowDialog() == DialogResult.OK;
                if (result)
                    dirName = folderDialog.SelectedPath;
            }
            return result;
        }

        /// <summary>
        /// Exports message to file having a specified name.
        /// </summary>
        /// <param name="exportDir">The export dir.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileData">The file data.</param>
        public void ExportFile(string exportDir, string fileName, byte[] fileData)
        {
            File.WriteAllBytes(Path.Combine(exportDir, fileName), fileData);
        }

        private readonly Dictionary<string, BackgroundWorker> workers = new Dictionary<string, BackgroundWorker>();

        /// <summary>
        /// Executes the specified action worker asynchronously.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="actionWorker">The action worker.</param>
        /// <param name="completeHandler">The complete handler.</param>
        public void ExecuteAction(string actionName, DoWorkEventHandler eventHandler,
            ActionWorker actionWorker, RunWorkerCompletedEventHandler completeHandler)
        {
            BackgroundWorker worker;
            if(!workers.TryGetValue(actionName, out worker))
            {
                worker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
                worker.ProgressChanged += progressChanged;
                worker.DoWork += eventHandler;
                worker.RunWorkerCompleted += completeHandler;
                workers.Add(actionName, worker);
            }
            actionWorker.Worker = worker;
            worker.RunWorkerAsync(actionWorker);
        }

        /// <summary>
        /// Actions the is busy.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns></returns>
        public bool ActionIsBusy(string actionName)
        {
            BackgroundWorker worker;
            return workers.TryGetValue(actionName, out worker) && worker.IsBusy;
        }

        /// <summary>
        /// Raises the <see cref="OnShowError"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.IO.ErrorEventArgs"/> instance containing the event data.</param>
        public void OnShowError(ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().Message, Application.ApplicationName,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Selects the files to import.
        /// </summary>
        /// <param name="extensions">The extensions.</param>
        /// <param name="files">The files.</param>
        /// <returns></returns>
        public bool SelectFilesToImport(List<string> extensions, out Dictionary<string, byte[]> files)
        {
            bool result;
            files = new Dictionary<string, byte[]>();
            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.RestoreDirectory = true;
                var fileterList = new List<string>();
                foreach (var extension in extensions)
                {
                    fileterList.Add("*" +  extension);
                    fileterList.Add("*" +  extension);
                }
                dialog.Filter = string.Join("|", fileterList.ToArray());
                result = dialog.ShowDialog() == DialogResult.OK;
                if(result)
                    foreach (var fileName in dialog.FileNames)
                        files.Add(Path.GetFileName(fileName), File.ReadAllBytes(fileName));
            }

            return result;
        }

        #endregion
    }
}
