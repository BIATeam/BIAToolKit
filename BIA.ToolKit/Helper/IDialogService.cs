namespace BIA.ToolKit.Helper
{
    using System.Collections.Generic;
    using BIA.ToolKit.ViewModels;

    /// <summary>
    /// Abstraction for opening dialogs from ViewModels without coupling to UI types.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Opens the repository form dialog pre-populated with the given repository.
        /// Returns the resulting <see cref="RepositoryViewModel"/> if the user
        /// confirmed, or null if the dialog was cancelled.
        /// </summary>
        RepositoryViewModel ShowRepositoryForm(RepositoryViewModel repository);

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

        /// <summary>
        /// Copies the given text to the system clipboard.
        /// </summary>
        void CopyToClipboard(string text);

        /// <summary>
        /// Shows the DefaultTeamSettings dialog and returns the results (name, plural, domain)
        /// or null if the user cancelled.
        /// </summary>
        (string TeamName, string TeamNamePlural, string DomainName)? ShowDefaultTeamSettings(
            string currentName, string currentNamePlural, string currentDomainName);

        /// <summary>
        /// Opens a lightweight popup to edit the Company Files settings (version, profile, options).
        /// Bindings are live on the passed <paramref name="viewModel"/>; closing the dialog
        /// simply dismisses the popup.
        /// </summary>
        void ShowCompanyFilesEditor(VersionAndOptionViewModel viewModel);
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
