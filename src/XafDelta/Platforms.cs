using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using DevExpress.ExpressApp;

namespace XafDelta
{
    /// <summary>
    /// XafDelta platform interface
    /// </summary>
    public interface IXafDeltaPlatform
    {
        /// <summary>
        /// Exports replication message to the file.
        /// </summary>
        /// <param name="exportDir">The export dir.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileData">The file data.</param>
        void ExportFile(string exportDir, string fileName, byte[] fileData);
        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <param name="eventHandler">The event handler.</param>
        /// <param name="actionWorker">The action worker.</param>
        /// <param name="completeHandler">The complete handler.</param>
        void ExecuteAction(string actionName, DoWorkEventHandler eventHandler,
            ActionWorker actionWorker, RunWorkerCompletedEventHandler completeHandler);
        /// <summary>
        /// Actions the is busy.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns></returns>
        bool ActionIsBusy(string actionName);
        /// <summary>
        /// Raises the ShowError event.
        /// </summary>
        /// <param name="e">The <see cref="System.IO.ErrorEventArgs"/> instance containing the event data.</param>
        void OnShowError(ErrorEventArgs e);
    }

    /// <summary>
    /// XafDelta winforms platform
    /// </summary>
    public interface IXafDeltaWinFormsPlatform : IXafDeltaPlatform
    {
        /// <summary>
        /// Selects the export directory.
        /// </summary>
        /// <param name="dirName">Name of the dir.</param>
        /// <returns></returns>
        bool SelectExportDirectory(out string dirName);
        /// <summary>
        /// Selects the files to import.
        /// </summary>
        /// <param name="extensions">The extensions.</param>
        /// <param name="files">The files.</param>
        /// <returns></returns>
        bool SelectFilesToImport(List<string> extensions, out Dictionary<string, byte[]> files);
    }

    /// <summary>
    ///XafDelta web platform interface
    /// </summary>
    public interface IXafDeltaWebPlatform : IXafDeltaPlatform
     {
         /// <summary>
         /// Creates the import detail view.
         /// </summary>
         /// <param name="objectSpace">The object space.</param>
         /// <param name="extList">The ext list.</param>
         /// <returns></returns>
         DetailView CreateImportDetailView(IObjectSpace objectSpace, List<string> extList);
         /// <summary>
         /// Gets the files to import.
         /// </summary>
         /// <param name="files">The files.</param>
         /// <param name="view">The view.</param>
         void GetFilesToImport(Dictionary<string, byte[]> files, View view);
     }
}
