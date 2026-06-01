namespace BIA.ToolKit.Domain.Model
{
    /// <summary>
    /// Per-card sync state of a repository, used by RepositoryCardUC to
    /// pick the status row content (✓ N versions / ⏳ Fetching / ⚠ Failed)
    /// and the card's border color.
    /// </summary>
    public enum RepositorySyncStatus
    {
        Idle,
        Syncing,
        Failed,
    }
}
