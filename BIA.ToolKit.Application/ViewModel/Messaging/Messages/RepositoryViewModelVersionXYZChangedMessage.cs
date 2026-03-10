namespace BIA.ToolKit.Application.ViewModel.Messaging.Messages
{
    /// <summary>
    /// Published when a repository view model's IsVersionXYZ flag is toggled to true,
    /// so all other repositories can reset their own flag.
    /// </summary>
    public class RepositoryViewModelVersionXYZChangedMessage : IMessage
    {
        public required RepositoryViewModel Repository { get; set; }
    }
}
