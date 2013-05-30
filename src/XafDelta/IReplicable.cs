using XafDelta.Replication;

namespace XafDelta
{
    /// <summary>
    /// Replicable object interface
    /// </summary>
    public interface IReplicable
    {
        /// <summary>
        /// Gets the recipients.
        /// </summary>
        /// <param name="args">The <see cref="GetRecipientsEventArgs"/> instance containing the event data.</param>
        void GetRecipients(GetRecipientsEventArgs args);
    }
}
