namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.ViewModel;
    /// <summary>
    /// Published when a repository view model's IsVersionXYZ flag is toggled to true,
    /// so all other repositories can reset their own flag.
    /// </summary>
    public class RepositoryViewModelVersionXYZChangedMessage : IMessage
    {
        public required RepositoryViewModel Repository { get; set; }
    }
}
