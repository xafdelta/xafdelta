using System.Collections.Generic;

namespace XafDelta.Delivery
{
    /// <summary>
    /// Xaf delta transport interface
    /// </summary>
    public interface IXafDeltaTransport
    {
        /// <summary>
        /// Gets a value indicating whether [use for download].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use for download]; otherwise, <c>false</c>.
        /// </value>
        bool UseForDownload { get; }
        /// <summary>
        /// Gets a value indicating whether [use for upload].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use for upload]; otherwise, <c>false</c>.
        /// </value>
        bool UseForUpload { get; }
        /// <summary>
        /// Gets the file names.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="mask">The mask.</param>
        /// <returns></returns>
        IEnumerable<string> GetFileNames(ActionWorker worker, string mask);
        /// <summary>
        /// Downloads the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="worker">The worker.</param>
        /// <returns></returns>
        byte[] DownloadFile(string fileName, ActionWorker worker);
        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileData">The file data.</param>
        /// <param name="recipientAddress">The recipient address.</param>
        /// <param name="worker">The worker.</param>
        /// <returns></returns>
        bool UploadFile(string fileName, byte[] fileData, string recipientAddress, ActionWorker worker);
        /// <summary>
        /// Opens the specified transport in selected mode.
        /// </summary>
        /// <param name="transportMode">The transport mode.</param>
        /// <param name="worker">The worker.</param>
        void Open(TransportMode transportMode, ActionWorker worker);
        /// <summary>
        /// Closes this transport.
        /// </summary>
        void Close();

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="worker"></param>
        void DeleteFile(string fileName, ActionWorker worker);
    }

    /// <summary>
    /// Transport mode
    /// </summary>
    public enum TransportMode
    {
        /// <summary>
        /// Download messages mode
        /// </summary>
        Download,
        /// <summary>
        /// Upload messages mode
        /// </summary>
        Upload
    }
}
