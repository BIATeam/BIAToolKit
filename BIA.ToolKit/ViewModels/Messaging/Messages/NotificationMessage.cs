namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    /// <summary>
    /// Severity level for a notification message.
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Published when a component wants to show a user-facing notification.
    /// </summary>
    public class NotificationMessage : IMessage
    {
        public required string Message { get; set; }
        public NotificationType Type { get; set; } = NotificationType.Info;
    }
}
