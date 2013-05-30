namespace XafDelta
{
    /// <summary>
    /// Snapshotable object interface
    /// </summary>
    public interface ISnapshotable
    {
        /// <summary>
        /// Determine whether object should be included in snapshot.
        /// </summary>
        /// <param name="snapshotRecipientNode">The snapshot recipient node.</param>
        /// <returns>
        ///     <c>true</c> if this instance should be included in snapshot; otherwise, <c>false</c>.
        /// </returns>
        bool ShouldBeIncludedInSnapshot(ReplicationNode snapshotRecipientNode);
    }
}
