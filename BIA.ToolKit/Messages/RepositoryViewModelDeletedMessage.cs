using BIA.ToolKit.ViewModels;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when a repository view model is deleted
    /// </summary>
    public class RepositoryViewModelDeletedMessage
    {
        public RepositoryViewModel Repository { get; }

        public RepositoryViewModelDeletedMessage(RepositoryViewModel repository)
        {
            Repository = repository;
        }
    }
}
