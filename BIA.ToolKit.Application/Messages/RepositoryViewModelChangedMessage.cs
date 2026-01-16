using BIA.ToolKit.Application.ViewModel;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when a repository view model is changed
    /// </summary>
    public class RepositoryViewModelChangedMessage
    {
        public RepositoryViewModel OldRepository { get; }
        public RepositoryViewModel NewRepository { get; }

        public RepositoryViewModelChangedMessage(RepositoryViewModel oldRepository, RepositoryViewModel newRepository)
        {
            OldRepository = oldRepository;
            NewRepository = newRepository;
        }
    }
}
