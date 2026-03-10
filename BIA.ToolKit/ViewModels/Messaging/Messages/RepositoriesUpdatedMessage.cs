namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    /// <summary>
    /// Published when the collection of active repositories has changed (e.g. UseRepository toggled).
    /// </summary>
    public class RepositoriesUpdatedMessage : IMessage { }
}
