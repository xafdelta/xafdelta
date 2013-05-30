using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using DevExpress.Utils;

namespace XafDelta.Delivery.Ftp
{
    [ToolboxItem(true)]
    [ToolboxTabName(XafAssemblyInfo.DXTabXafModules)]
    [Description("XafDelta FTP transport.")]
    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [ToolboxBitmap(typeof(XafDeltaFtpModule), "Resources.Delta.ico")]
    public sealed partial class XafDeltaFtpModule : ModuleBase, IXafDeltaTransport
    {

        public XafDeltaFtpModule()
        {
            ModelDifferenceResourceName = "XafDelta.Delivery.Ftp.Model.DesignedDiffs";
            InitializeComponent();
            Host = @"ftp:\\localhost";
            UseForDownload = true;
            UseForDownload = true;
        }

        public override string ToString()
        {
            return Name;
        }

        public string Password { get; set; }
        public string UserName { get; set; }
        public string Host { get; set; }
        public string InitialDir { get; set; }

        #region Implementation of IXafDeltaTransport

        /// <summary>
        /// Gets a value indicating whether [use for download].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use for download]; otherwise, <c>false</c>.
        /// </value>
        public bool UseForDownload { get; set; }

        /// <summary>
        /// Gets a value indicating whether [use for upload].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use for upload]; otherwise, <c>false</c>.
        /// </value>
        public bool UseForUpload { get; set; }
        

        /// <summary>
        /// Opens the specified transport in selected mode.
        /// </summary>
        /// <param name="transportMode">The transport mode.</param>
        /// <param name="worker">The worker.</param>
        public void Open(TransportMode transportMode, ActionWorker worker)
        {
            var request = (FtpWebRequest)WebRequest.Create(Host + @"/" + InitialDir );
            request.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;
            request.Credentials = new NetworkCredential(UserName, Password);
            try
            {
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    var sr = new StreamReader(response.GetResponseStream());
                    var currentDir = sr.ReadToEnd();
                    worker.ReportProgress("FTP connection is opened. Current FTP directory is '" + currentDir + "'");
                }
            }
            catch (Exception exception)
            {
                worker.ReportProgress(Color.Red, "Error: failed to open FTP connection. " + exception.Message);
            }
        }

        /// <summary>
        /// Closes this transport.
        /// </summary>
        public void Close()
        {
        }

        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileData">The file data.</param>
        /// <param name="recipientAddress">The recipient address.</param>
        /// <param name="worker">The worker.</param>
        /// <returns></returns>
        public bool UploadFile(string fileName, byte[] fileData, string recipientAddress, ActionWorker worker)
        {
            var result = false;

            var requestUrl = Host + @"/";
            if (!string.IsNullOrEmpty(InitialDir))
                requestUrl += InitialDir + @"/";
            requestUrl += fileName;

            var request = (FtpWebRequest)WebRequest.Create(requestUrl);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(UserName, Password);
            try
            {
                using (var ms = new MemoryStream(fileData))
                using (var reqStream = request.GetRequestStream())
                {
                    ms.CopyTo(reqStream);
                }

                var responce = (FtpWebResponse) request.GetResponse();
                result = responce.StatusCode == FtpStatusCode.CommandOK || responce.StatusCode == FtpStatusCode.ClosingData;
                if(result)
                    worker.ReportProgress(Color.Green, "Uploaded OK");
                else
                    worker.ReportProgress(Color.Red, "Uploading failed with status '" + responce.StatusDescription + "'");
            }
            catch (Exception exception)
            {
                worker.ReportProgress(Color.Red, "Uploading error: " + exception.Message);
            }

            return result;
        }

        /// <summary>
        /// Downloads the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="worker">The worker.</param>
        /// <returns></returns>
        public byte[] DownloadFile(string fileName, ActionWorker worker)
        {
            byte[] result = null;

            var requestUrl = Host + @"/";
            if (!string.IsNullOrEmpty(InitialDir))
                requestUrl += InitialDir + @"/";
            requestUrl += fileName;

            var request = (FtpWebRequest)WebRequest.Create(requestUrl);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(UserName, Password);
            try
            {
                using (var ms = new MemoryStream())
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    response.GetResponseStream().CopyTo(ms);
                    worker.ReportProgress(Color.Green, "Downloaded OK.");
                    ms.Close();
                    result = ms.ToArray();
                }
            }
            catch (Exception exception)
            {
                worker.ReportProgress(Color.Red, "Error: failed to download file. " + exception.Message);
            }

            return result;
        }

        /// <summary>
        /// Gets the file names.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public IEnumerable<string> GetFileNames(ActionWorker worker, string mask)
        {
            var result = new List<string>();
            var request = (FtpWebRequest)WebRequest.Create(Host + @"/" + InitialDir);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(UserName, Password);
            try
            {
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    var sr = new StreamReader(response.GetResponseStream());
                    var listResult = sr.ReadToEnd();
                    result.AddRange(listResult.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch (Exception exception)
            {
                worker.ReportProgress(Color.Red, "Error: failed to list FTP directory. " + exception.Message);
            }

            var fileNames = result.Where(x => Regex.IsMatch(x, mask)).ToList();

            return fileNames;
        }

        public void DeleteFile(string fileName, ActionWorker worker)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
