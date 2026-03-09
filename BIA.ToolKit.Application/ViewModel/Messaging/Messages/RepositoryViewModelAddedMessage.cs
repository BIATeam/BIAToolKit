namespace BIA.ToolKit.Application.ViewModel.Messaging.Messages
{
    /// <summary>
    /// Published when a new repository view model has been created by the user.
    /// </summary>
    public class RepositoryViewModelAddedMessage : IMessage
    {
        public required RepositoryViewModel Repository { get; set; }
    }
}
