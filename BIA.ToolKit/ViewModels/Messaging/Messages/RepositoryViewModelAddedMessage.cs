namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.ViewModel;
    /// <summary>
    /// Published when a new repository view model has been created by the user.
    /// </summary>
    public class RepositoryViewModelAddedMessage : IMessage
    {
        public required RepositoryViewModel Repository { get; set; }
    }
}
