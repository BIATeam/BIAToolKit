namespace BIA.ToolKit.Application.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Domain.Settings;

    /// <summary>
    /// Published when application settings have been updated.
    /// </summary>
    public class SettingsUpdatedMessage : IMessage
    {
        public required IBIATKSettings Settings { get; set; }
    }
}
