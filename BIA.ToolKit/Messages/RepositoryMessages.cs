namespace BIA.ToolKit.Messages
{
    using BIA.ToolKit.ViewModels;

    public sealed record RepositoryChangedMessage(RepositoryViewModel OldRepository, RepositoryViewModel NewRepository);

    public sealed record RepositoryDeletedMessage(RepositoryViewModel Repository);

    public sealed record RepositoryAddedMessage(RepositoryViewModel Repository);

    public sealed record RepositoryVersionXYZChangedMessage(RepositoryViewModel Repository);

    public sealed record RepositoryReleaseDataUpdatedMessage(RepositoryViewModel Repository);

    public sealed record OpenRepositoryFormMessage(RepositoryViewModel Repository, RepositoryFormMode Mode);

    public enum RepositoryFormMode
    {
        Create,
        Edit
    }
}
