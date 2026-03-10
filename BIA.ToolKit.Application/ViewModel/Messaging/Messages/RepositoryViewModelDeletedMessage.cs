namespace BIA.ToolKit.Application.ViewModel.Messaging.Messages
{
    /// <summary>
    /// Published when a repository view model has been deleted by the user.
    /// </summary>
    public class RepositoryViewModelDeletedMessage : IMessage
    {
        public required RepositoryViewModel Repository { get; set; }
    }
}
