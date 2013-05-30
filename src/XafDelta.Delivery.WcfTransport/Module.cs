using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using DevExpress.ExpressApp;
using DevExpress.Utils;
using XafDelta.Delivery.WcfTransport.WcfTransportClient;
using XafDelta.Delivery.WcfTransport.Localization;

namespace XafDelta.Delivery.WcfTransport
{
    [ToolboxItem(true)]
    [ToolboxTabName(XafAssemblyInfo.DXTabXafModules)]
    [Description("XafDelta WCF based transport.")]
    [Browsable(true)]
    [EditorBrowsable(EditorBrowsableState.Always)]
    [ToolboxBitmap(typeof(WcfTransportModule), "Resources.Delta.ico")]
    public sealed partial class WcfTransportModule : ModuleBase, IXafDeltaTransport
    {
        public WcfTransportModule()
        {
            ModelDifferenceResourceName = "XafDelta.Delivery.WcfTransport.Model.DesignedDiffs";
            InitializeComponent();
            UseForDownload = true;
            UseForUpload = true;
            ResourcesExportedToModel.Add(typeof(Localizer));
        }

        

        public override string ToString()
        {
            return Name;
        }

        private TransportServiceClient client;

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

        #region Events

        /// <summary>
        /// Occurs before open WCF client.
        /// </summary>
        public event EventHandler<BeforeOpenArgs> BeforeOpen;
        internal void OnBeforeOpen(BeforeOpenArgs e)
        {
            var handler = BeforeOpen;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before download.
        /// </summary>
        public event EventHandler<WcfClientArgs> BeforeDownload;
        public void OnBeforeDownload(WcfClientArgs e)
        {
            var handler = BeforeDownload;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs After download.
        /// </summary>
        public event EventHandler<WcfClientArgs> AfterDownload;
        public void OnAfterDownload(WcfClientArgs e)
        {
            var handler = AfterDownload;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs before Upload.
        /// </summary>
        public event EventHandler<WcfClientArgs> BeforeUpload;
        public void OnBeforeUpload(WcfClientArgs e)
        {
            var handler = BeforeUpload;
            if (handler != null) handler(this, e);
        }

        /// <summary>
        /// Occurs After Upload.
        /// </summary>
        public event EventHandler<WcfClientArgs> AfterUpload;
        public void OnAfterUpload(WcfClientArgs e)
        {
            var handler = AfterUpload;
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

        /// <summary>
        /// Gets the file names.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        public IEnumerable<string> GetFileNames(ActionWorker worker, string mask)
        {
            IEnumerable<string> result = null;
            if(client != null)
            {
                try
                {
                    result = client.GetFileNames(mask);
                    worker.ReportProgress(Localizer.FileListingOk);
                }
                catch (Exception exception)
                {
                    worker.ReportError(Localizer.FileListingError, exception.Message);
                }
            }
            else
                worker.ReportError(Localizer.ClientIsNotOpened);
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
            if (client != null)
            {
                try
                {
                    OnBeforeDownload(new WcfClientArgs(client, fileName));
                    result = client.DownloadFile(fileName);
                    OnAfterDownload(new WcfClientArgs(client, fileName));
                    worker.ReportProgress(Localizer.DownloadOk);
                }
                catch (Exception exception)
                {
                    worker.ReportError(Localizer.DownloadError, fileName, exception.Message);
                }
            }
            else
                worker.ReportError(Localizer.ClientIsNotOpened);
            return result;
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
            if (client != null)
            {
                try
                {
                    OnBeforeUpload(new WcfClientArgs(client, fileName));
                    client.UploadFile(fileName, fileData);
                    OnAfterUpload(new WcfClientArgs(client, fileName));
                    worker.ReportProgress(Localizer.UploadOk);
                    result = true;
                }
                catch (Exception exception)
                {
                    worker.ReportError(Localizer.UploadError, fileName, exception.Message);
                }
            }
            else
                worker.ReportError(Localizer.ClientIsNotOpened);
            return result;
        }

        /// <summary>
        /// Opens the specified transport in selected mode.
        /// </summary>
        /// <param name="transportMode">The transport mode.</param>
        /// <param name="worker">The worker.</param>
        public void Open(TransportMode transportMode, ActionWorker worker)
        {
            Close();
            client = new TransportServiceClient();
            if(!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
            {
                if (client.ClientCredentials != null)
                {
                    client.ClientCredentials.UserName.UserName = UserName;
                    client.ClientCredentials.UserName.Password = Password;
                }
            }
            OnBeforeOpen(new BeforeOpenArgs(client));
            client.Open();
            worker.ReportProgress(Localizer.ClientOpened);
        }

        /// <summary>
        /// Closes transport.
        /// </summary>
        public void Close()
        {
            if (client != null)
            {
                try { client.Close(); } catch { client = null;}
                client = null;
            }
        }

        public void DeleteFile(string fileName, ActionWorker worker)
        {
            if (client != null)
                client.DeleteFile(fileName);
            else
                worker.ReportError(Localizer.ClientIsNotOpened);
        }

        #endregion
    }

    #region Event args

    public class WcfClientArgs : EventArgs
    {
        public TransportServiceClient Client { get; private set; }
        public string FileName { get; private set; }

        public WcfClientArgs(TransportServiceClient client, string fileName)
        {
            Client = client;
            FileName = fileName;
        }
    }

    public class BeforeOpenArgs : EventArgs
    {
        public TransportServiceClient Client { get; private set; }

        public BeforeOpenArgs(TransportServiceClient client)
        {
            Client = client;
        }
    }

    #endregion
}
