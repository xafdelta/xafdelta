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

namespace XafDelta.Delivery.WebTransport
{
    [ToolboxItem(true)]
    [ToolboxTabName(XafAssemblyInfo.DXTabXafModules)]
    [Description("XafDelta WebClient based transport.")]
    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [ToolboxBitmap(typeof(WebTransportModule), "Resources.Delta.ico")]
    public sealed partial class WebTransportModule : ModuleBase, IXafDeltaTransport
    {
        static WebTransportModule()
        {
            ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
        }

        public WebTransportModule()
        {
            ModelDifferenceResourceName = "XafDelta.Delivery.WebTransport.Model.DesignedDiffs";
            InitializeComponent();
            UseForDownload = true;
            UseForUpload = true;
            ResourcesExportedToModel.Add(typeof(Localization.Localizer));
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the download URL.
        /// </summary>
        /// <value>
        /// The download URL.
        /// </value>
        public string DownloadUrl { get; set; }
        /// <summary>
        /// Gets or sets the upload URL.
        /// </summary>
        /// <value>
        /// The upload URL.
        /// </value>
        public string UploadUrl { get; set; }

        #region Events

        /// <summary>
        /// Occurs when getting URI.
        /// </summary>
        public event EventHandler<GetUriArgs> CustomUri;
        internal void OnCustomUri(GetUriArgs e)
        {
            var handler = CustomUri;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<CommandArgs> BeforeCommand;
        public void OnBeforeCommand(CommandArgs e)
        {
            EventHandler<CommandArgs> handler = BeforeCommand;
            if (handler != null) handler(this, e);
        }

        public event EventHandler<CommandArgs> AfterCommand;
        public void OnAfterCommand(CommandArgs e)
        {
            EventHandler<CommandArgs> handler = AfterCommand;
            if (handler != null) handler(this, e);
        }

        #endregion

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

        private static bool uriIsValid(Uri uri)
        {
            var result = uri != null && (new[]
                                             {
                                                 Uri.UriSchemeFile, 
                                                 Uri.UriSchemeHttp, 
                                                 Uri.UriSchemeHttps, 
                                                 Uri.UriSchemeFtp
                                             }).Contains(uri.Scheme);
            return result;
        }

        /// <summary>
        /// Gets the credentials.
        /// </summary>
        /// <returns></returns>
        private ICredentials getCredentials()
        {
            ICredentials credentials;
            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                credentials = new NetworkCredential(UserName, Password);
            else
                credentials = CredentialCache.DefaultCredentials;
            return credentials;
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmdName">Name of the CMD.</param>
        /// <param name="workStr">The work STR.</param>
        /// <param name="finishStr">The finish STR.</param>
        /// <param name="worker">The worker.</param>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="uriProcessor">The URI processor.</param>
        /// <param name="cmdProcessor">The CMD processor.</param>
        /// <param name="postProcessor">The post processor.</param>
        /// <param name="cmdArgs">The CMD args.</param>
        /// <returns></returns>
        private T executeCommand<T>(string cmdName, string workStr, string finishStr,
            ActionWorker worker, string baseUri, 
            Func<string, ActionWorker, string, object[], Uri> uriProcessor,
            Func<string, ActionWorker, Uri, object[], T> cmdProcessor, 
            Func<string, ActionWorker, object, T> postProcessor,
            params object[] cmdArgs)
        {
            var uri = uriProcessor(cmdName, worker, baseUri, cmdArgs);
            var args = new GetUriArgs(cmdName, uri, cmdArgs);
            OnCustomUri(args);
            uri = args.ResultUri;
            T result = default(T);
            if (uriIsValid(uri))
            {
                OnBeforeCommand(new CommandArgs(cmdName, uri, cmdArgs));
                worker.ReportProgress(workStr, cmdArgs);
                try
                {
                    result = cmdProcessor(cmdName, worker, uri, cmdArgs);
                }
                catch(Exception exception)
                {
                    worker.ReportError(Localization.Localizer.CommandError, cmdName, exception.Message);
                }
                OnAfterCommand(new CommandArgs(cmdName, uri, cmdArgs));
                if (postProcessor != null)
                    result = postProcessor(cmdName, worker, result);
                worker.ReportProgress(finishStr, cmdArgs);
            }
            else
                worker.ReportError(Localization.Localizer.InvalidUri, uri.ToString());

            return result;
        }

        #region GetFileNames

        /// <summary>
        /// Gets the file names.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public IEnumerable<string> GetFileNames(ActionWorker worker, string mask)
        {
            return executeCommand<IEnumerable<string>>("List", Localization.Localizer.Listing, Localization.Localizer.Listed, worker, DownloadUrl, listingUriProcessor,
                                                       listingCmdProcessor, null, mask);
        }

        private IEnumerable<string> listingCmdProcessor(string cmdName, ActionWorker worker, Uri uri, object[] cmdArgs)
        {
            string responceString;
            var credentials = getCredentials();
            if (uri.Scheme == Uri.UriSchemeFtp)
                responceString = getFtpFileNames(uri, credentials);
            else if (uri.Scheme == Uri.UriSchemeFile)
                responceString = getDirectoryFileNames(uri, worker);
            else
                responceString = getWebFileNames(uri, credentials);
            var mask = cmdArgs[0].ToString();
            var names = Regex.Matches(responceString, mask).Cast<Match>().Select(x => x.Value).Distinct();
            return names;
        }

        private Uri listingUriProcessor(string cmdName, ActionWorker worker, string baseUri, object[] cmdArgs)
        {
            return new Uri(baseUri);
        }

        /// <summary>
        /// Gets the web file names.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="credentials">The credentials.</param>
        /// <returns></returns>
        private string getWebFileNames(Uri uri, ICredentials credentials)
        {
            string result;
            using (var webClient = new WebClient())
            {
                webClient.Credentials = credentials;
                result = webClient.DownloadString(uri);
            }
            return result;
        }

        /// <summary>
        /// Gets the directory file names.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="worker">The worker.</param>
        /// <returns></returns>
        private string getDirectoryFileNames(Uri uri, ActionWorker worker)
        {
            var dirInfo = new DirectoryInfo(uri.LocalPath);
            var result = string.Join("\n", dirInfo.GetFiles().Select(x => x.Name).ToArray());
            return result;
        }

        /// <summary>
        /// Gets the FTP file names.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="credentials">The credentials.</param>
        /// <returns></returns>
        private string getFtpFileNames(Uri uri, ICredentials credentials)
        {
            var result = string.Empty;
            var request = (FtpWebRequest) WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = credentials;
            using (var response = (FtpWebResponse) request.GetResponse())
            {
                var responceStream = response.GetResponseStream();
                if (responceStream != null)
                {
                    var streamReader = new StreamReader(responceStream);
                    result = streamReader.ReadToEnd();
                }
            }
            return result;
        }

        #endregion

        #region DownloadFile

        /// <summary>
        /// Downloads the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="worker">The worker.</param>
        /// <returns></returns>
        public byte[] DownloadFile(string fileName, ActionWorker worker)
        {
            return executeCommand<byte[]>("Download", Localization.Localizer.Downloading, Localization.Localizer.Downloaded, worker, DownloadUrl, downloadUriProcessor,
                                          downloadCmdProcessor, null, fileName);
        }

        private byte[] downloadCmdProcessor(string cmdName, ActionWorker worker, Uri uri, object[] cmdArgs)
        {
            byte[] result;
            using (var webClient = new WebClient())
            {
                webClient.Credentials = getCredentials();
                result = webClient.DownloadData(uri);
            }
            return result;
        }

        private Uri downloadUriProcessor(string cmdName, ActionWorker worker, string baseUri, object[] cmdArgs)
        {
            var uri = new Uri(baseUri);
            var fileName = cmdArgs[0].ToString();
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                uri = new Uri(baseUri + @"?DownloadFile=" + fileName);
            else
                uri = new Uri(baseUri + @"/" + fileName);
            return uri;
        }

        #endregion

        #region UploadFile

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
            return executeCommand<bool>("Upload", Localization.Localizer.Uploading, Localization.Localizer.Uploaded, worker, DownloadUrl, uploadUriProcessor,
                                        uploadCmdProcessor, null, fileName, fileData, recipientAddress);
        }

        private bool uploadCmdProcessor(string cmdName, ActionWorker worker, Uri uri, object[] cmdArgs)
        {
            var fileName = cmdArgs[0].ToString();
            var fileData = (byte[]) cmdArgs[1];

            using (var webClient = new WebClient())
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                }

                webClient.Credentials = getCredentials();

                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    var tempFileName = Path.Combine(Path.GetTempPath(), fileName);
                    File.WriteAllBytes(tempFileName, fileData);
                    try
                    {
                        webClient.UploadFile(uri, tempFileName);
                    }
                    finally
                    {
                        if (File.Exists(tempFileName))
                            File.Delete(tempFileName);
                    }
                }
                else
                    webClient.UploadData(uri, fileData);
            }

            return true;
        }

        private Uri uploadUriProcessor(string cmdName, ActionWorker worker, string baseUri, object[] cmdArgs)
        {
            var fileName = cmdArgs[0].ToString();
            var uri = new Uri(UploadUrl);
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                uri = new Uri(UploadUrl + @"/" + fileName);
            return uri;
        }

        #endregion

        #region DeleteFile

        public void DeleteFile(string fileName, ActionWorker worker)
        {
            executeCommand<bool>("Delete", Localization.Localizer.Deleting, Localization.Localizer.Deleted, worker, DownloadUrl, deleteUriProcessor,
                                 deleteCmdProcessor, null, fileName);
        }

        private bool deleteCmdProcessor(string cmdName, ActionWorker worker, Uri uri, object[] cmdArgs)
        {
            var fileName = cmdArgs[0].ToString();
            using (var webClient = new WebClient())
            {
                webClient.Credentials = getCredentials();
                if (uri.Scheme == Uri.UriSchemeFtp)
                    deleteFtpFile(uri, webClient.Credentials);
                else if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    webClient.DownloadString(uri);
                else if (uri.Scheme == Uri.UriSchemeFile)
                {
                    var fullFileName = Path.Combine(uri.LocalPath, fileName);
                    if (File.Exists(fullFileName))
                        File.Delete(fullFileName);
                }
            }
            return true;
        }

        private Uri deleteUriProcessor(string cmdName, ActionWorker worker, string baseUri, object[] cmdArgs)
        {
            var uri = new Uri(baseUri);
            var fileName = cmdArgs[0].ToString();
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                uri = new Uri(baseUri + @"?DeleteFile=" + fileName);
            else
                uri = new Uri(baseUri + @"/" + fileName);
            return uri;
        }

        private void deleteFtpFile(Uri uri, ICredentials credentials)
        {
            var request = (FtpWebRequest) WebRequest.Create(uri);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = credentials;
            using (var response = (FtpWebResponse) request.GetResponse())
            {
                response.Close();
            }
        }

        #endregion

        /// <summary>
        /// Opens the specified transport in selected mode.
        /// </summary>
        /// <param name="transportMode">The transport mode.</param>
        /// <param name="worker">The worker.</param>
        public void Open(TransportMode transportMode, ActionWorker worker)
        {
        }

        /// <summary>
        /// Closes this transport.
        /// </summary>
        public void Close()
        {
        }

        #endregion
    }
   

    #region Event args

    public class CommandArgs : EventArgs
    {
        public string CmdName { get; set; }
        public Uri Uri { get; set; }
        public object[] CmdArgs { get; set; }

        public CommandArgs(string cmdName, Uri uri, object[] cmdArgs)
        {
            CmdName = cmdName;
            Uri = uri;
            CmdArgs = cmdArgs;
        }
    }

    public class GetUriArgs : EventArgs
    {
        public string CmdName { get; set; }
        public Uri ResultUri { get; set; }
        public object[] CmdArgs { get; set; }

        public GetUriArgs(string cmdName, Uri defaultUri, params object[] cmdArgs)
        {
            CmdName = cmdName;
            ResultUri = defaultUri;
            CmdArgs = cmdArgs;
        }
    }

    #endregion
}
