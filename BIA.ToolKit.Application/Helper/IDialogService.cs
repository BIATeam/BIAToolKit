namespace BIA.ToolKit.Application.Helper
{
    using System.Collections.Generic;

    /// <summary>
    /// Abstraction for opening dialogs from ViewModels without coupling to UI types.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a log detail dialog with the given messages.
        /// </summary>
        bool? ShowLogDetail(List<LogMessage> messages);
    }

    /// <summary>
    /// Represents a single console/log message with color metadata.
    /// Shared between Application and UI layers.
    /// </summary>
    public class LogMessage
    {
        public string Text { get; set; }
        public string Color { get; set; }
    }
}
