using BIA.ToolKit.ViewModels;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when repository view model release data is updated
    /// </summary>
    public class RepositoryViewModelReleaseDataUpdatedMessage
    {
        public RepositoryViewModel Repository { get; }

        public RepositoryViewModelReleaseDataUpdatedMessage(RepositoryViewModel repository)
        {
            Repository = repository;
        }
    }
}
