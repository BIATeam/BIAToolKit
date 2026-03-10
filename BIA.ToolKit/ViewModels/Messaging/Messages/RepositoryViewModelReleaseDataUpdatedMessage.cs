namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.ViewModel;
    /// <summary>
    /// Published when a repository's release data has been refreshed,
    /// so dependent components (e.g. VersionAndOptionViewModel) can update.
    /// </summary>
    public class RepositoryViewModelReleaseDataUpdatedMessage : IMessage
    {
        public required RepositoryViewModel Repository { get; set; }
    }
}
