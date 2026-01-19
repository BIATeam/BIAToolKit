namespace BIA.ToolKit.Infrastructure.Services
{
    /// <summary>
    /// Service interface for file and folder browsing dialogs.
    /// Abstracts the underlying dialog implementation from the application logic.
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Opens a folder browser dialog and returns the selected path.
        /// </summary>
        /// <param name="initialPath">Initial path to start browsing from.</param>
        /// <param name="title">Dialog title.</param>
        /// <returns>Selected folder path, or null if user cancels.</returns>
        string BrowseFolder(string initialPath, string title);

        /// <summary>
        /// Opens a file open dialog and returns the selected path.
        /// </summary>
        /// <param name="filter">File filter (e.g., "C# Files (*.cs)|*.cs").</param>
        /// <returns>Selected file path, or null if user cancels.</returns>
        string BrowseFile(string filter);

        /// <summary>
        /// Opens a file save dialog and returns the selected path.
        /// </summary>
        /// <param name="fileName">Default file name.</param>
        /// <param name="filter">File filter (e.g., "Text Files (*.txt)|*.txt").</param>
        /// <returns>Selected file path, or null if user cancels.</returns>
        string SaveFile(string fileName, string filter);
    }
}
