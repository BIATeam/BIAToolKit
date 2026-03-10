namespace BIA.ToolKit.Application.ViewModel.Messaging.Messages
{
    /// <summary>
    /// Published when a ViewModel requests that the repository form dialog be shown.
    /// The main window subscribes and shows RepositoryFormUC with Owner = this.
    /// </summary>
    public class OpenRepositoryFormRequestMessage : IMessage
    {
        public required RepositoryViewModel Repository { get; set; }
        public required RepositoryFormMode Mode { get; set; }
    }
}
