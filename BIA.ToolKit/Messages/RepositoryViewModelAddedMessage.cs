using BIA.ToolKit.ViewModels;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when a repository view model is added
    /// </summary>
    public class RepositoryViewModelAddedMessage
    {
        public RepositoryViewModel Repository { get; }

        public RepositoryViewModelAddedMessage(RepositoryViewModel repository)
        {
            Repository = repository;
        }
    }
}
