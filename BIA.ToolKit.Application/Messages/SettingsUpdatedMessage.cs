using BIA.ToolKit.Domain.Settings;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when application settings are updated
    /// </summary>
    public class SettingsUpdatedMessage
    {
        public IBIATKSettings Settings { get; }

        public SettingsUpdatedMessage(IBIATKSettings settings)
        {
            Settings = settings;
        }
    }
}
