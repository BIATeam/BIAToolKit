namespace BIA.ToolKit.Application.ViewModel.Messaging.Messages
{
    /// <summary>
    /// Published when an existing repository view model has been replaced by an edited one.
    /// </summary>
    public class RepositoryViewModelChangedMessage : IMessage
    {
        public required RepositoryViewModel OldRepository { get; set; }
        public required RepositoryViewModel NewRepository { get; set; }
    }
}
