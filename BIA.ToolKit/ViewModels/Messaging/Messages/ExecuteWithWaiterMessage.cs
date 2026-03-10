namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Published when a component requests that an async task be executed with the UI waiter visible.
    /// The main window subscribes to this message and runs the task inside ExecuteTaskWithWaiterAsync.
    /// </summary>
    public class ExecuteWithWaiterMessage : IMessage
    {
        public required Func<Task> Task { get; set; }
    }
}
