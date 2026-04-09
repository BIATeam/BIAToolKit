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

        /// <summary>
        /// Opens a folder browser dialog and returns the selected path.
        /// Returns <paramref name="defaultFolder"/> if the user cancels.
        /// </summary>
        string BrowseFolder(string defaultFolder, string description = null);

        /// <summary>
        /// Shows a confirmation dialog. Returns true if the user confirms.
        /// </summary>
        bool Confirm(string message, string title = "Warning");

        /// <summary>
        /// Shows an informational message to the user.
        /// </summary>
        void ShowMessage(string message, string title = null);

        /// <summary>
        /// Opens a file browser dialog and returns the selected file path.
        /// Returns empty string if the user cancels.
        /// </summary>
        string BrowseFile(string defaultFolder, string fileFilter = null);
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
