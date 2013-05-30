using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Web;
using DevExpress.Utils;

namespace XafDelta.Web
{
    /// <summary>
    /// XafDelta web module. Implements <see cref="IXafDeltaWebPlatform"/>
    /// </summary>
    [ToolboxItem(true)]
    [ToolboxTabName(XafAssemblyInfo.DXTabXafModules)]
    [Description("XafDelta web module.")]
    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [ToolboxBitmap(typeof(XafDeltaWebModule), "Resources.Delta.ico")]
    [ToolboxItemFilter("Xaf.Platform.Web")]
    public sealed partial class XafDeltaWebModule : ModuleBase, IXafDeltaWebPlatform
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XafDeltaWebModule"/> class.
        /// </summary>
        public XafDeltaWebModule()
        {
            ModelDifferenceResourceName = "XafDelta.Web.Model.DesignedDiffs";
            InitializeComponent();
            Instance = this;
        }

        /// <summary>
        /// Gets the <see cref="XafDeltaWebModule"/> singleton.
        /// </summary>
        public static XafDeltaWebModule Instance { get; private set; }

        /// <summary>
        /// Sets up a module after it has been added to the <see cref="P:DevExpress.ExpressApp.XafApplication.Modules"/> collection.
        /// </summary>
        /// <param name="application">An <see cref="T:DevExpress.ExpressApp.XafApplication"/> object that provides methods and properties to manage the current application. This parameter value is set for the <see cref="P:DevExpress.ExpressApp.ModuleBase.Application"/> property.</param>
        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            XafDeltaModule.CustomGetApplication += XafDeltaModule_CustomGetApplication;
            var xafApp = (WebApplication)application ?? WebApplication.Instance;
            xafApp.LoggedOff += xafApp_LoggedOff;
            XafDeltaModule.Instance.GetPlatform += Instance_GetPlatform;
            xafApp.Disposed += application_Disposed;
        }

        void Instance_GetPlatform(object sender, GetPlatformArgs e)
        {
            e.XafDeltaPlatform = this;
        }

        void xafApp_LoggedOff(object sender, System.EventArgs e)
        {
            XafDeltaModule.Instance.ClearCurrentNodeIdCache();
        }

        void application_Disposed(object sender, System.EventArgs e)
        {
            var application = (WebApplication) sender;
            application.LoggedOff -= xafApp_LoggedOff;
            XafDeltaModule.Instance.GetPlatform -= Instance_GetPlatform;
            application.Disposed -= application_Disposed;
        }
        
        void XafDeltaModule_CustomGetApplication(object sender, CustomGetApplicationArgs e)
        {
            e.Application = Application ?? WebApplication.Instance;
        }

        #region Implementation of IXafDeltaPlatform

        /// <summary>
        /// Exports the file.
        /// </summary>
        /// <param name="exportDir">The export dir.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileData">The file data.</param>
        public void ExportFile(string exportDir, string fileName, byte[] fileData)
        {
            HttpContext.Current.Response.ClearHeaders();
            HttpContext.Current.Response.ClearContent();
            HttpContext.Current.Response.ContentType = "application/octet-stream";
            HttpContext.Current.Response.AppendHeader("content-length", fileData.Length.ToString());
            HttpContext.Current.Response.AppendHeader("Content-Disposition",
                                                      "attachment; filename=\"" + HttpUtility.UrlEncode(fileName) + "\"");
            HttpContext.Current.Response.OutputStream.Write(fileData, 0, fileData.Length);
            HttpContext.Current.Response.OutputStream.Flush();
            HttpContext.Current.Response.OutputStream.Close();
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="actionWorker">The action worker.</param>
        /// <param name="completeHandler">The complete handler.</param>
        public void ExecuteAction(string actionName, DoWorkEventHandler eventHandler, ActionWorker actionWorker, RunWorkerCompletedEventHandler completeHandler)
        {
            eventHandler(null, new DoWorkEventArgs(actionWorker));
            completeHandler(null, new RunWorkerCompletedEventArgs(null, null, false));
        }

        /// <summary>
        /// Actions the is busy.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns></returns>
        public bool ActionIsBusy(string actionName)
        {
            return false;
        }

        /// <summary>
        /// Raises the <see cref="OnShowError"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.IO.ErrorEventArgs"/> instance containing the event data.</param>
        public void OnShowError(ErrorEventArgs e)
        {
            
        }

        #endregion

        #region Implementation of IXafDeltaWebPlatform

        /// <summary>
        /// Creates the import detail view.
        /// </summary>
        /// <param name="objectSpace">The object space.</param>
        /// <param name="extList">The ext list.</param>
        /// <returns></returns>
        public DetailView CreateImportDetailView(IObjectSpace objectSpace, List<string> extList)
        {
            var detView = Application.CreateDetailView(objectSpace, objectSpace.CreateObject<ImportMessages>());
            detView.ViewEditMode = ViewEditMode.Edit;
            return detView;
        }

        /// <summary>
        /// Gets the files to import.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <param name="view">The view.</param>
        public void GetFilesToImport(Dictionary<string, byte[]> files, View view)
        {
            var importData = (ImportMessages) view.CurrentObject;
            if (importData.File1 != null && !importData.File1.IsEmpty)
                files.Add(importData.File1.FileName, importData.File1.Content);
            if (importData.File2 != null && !importData.File2.IsEmpty)
                files.Add(importData.File2.FileName, importData.File2.Content);
            if (importData.File3 != null && !importData.File3.IsEmpty)
                files.Add(importData.File3.FileName, importData.File3.Content);
        }

        #endregion
    }
}
