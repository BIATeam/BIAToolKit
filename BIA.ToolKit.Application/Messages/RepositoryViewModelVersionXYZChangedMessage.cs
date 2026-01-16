using BIA.ToolKit.Application.ViewModel;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when a repository view model version XYZ flag changes
    /// </summary>
    public class RepositoryViewModelVersionXYZChangedMessage
    {
        public RepositoryViewModel Repository { get; }

        public RepositoryViewModelVersionXYZChangedMessage(RepositoryViewModel repository)
        {
            Repository = repository;
        }
    }
}
