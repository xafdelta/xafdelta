namespace XafDelta.Messaging
{
    /// <summary>
    /// Replication message interface (implemented by Package and Ticket)
    /// </summary>
    public interface IReplicationMessage
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        string FileName { get; }

        /// <summary>
        /// Gets the recipient address for upload.
        /// </summary>
        string RecipientAddress { get; }

        /// <summary>
        /// Gets the message binary data.
        /// </summary>
        /// <returns></returns>
        byte[] GetData();
    }
}
