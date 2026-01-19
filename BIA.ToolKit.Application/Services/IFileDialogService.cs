namespace BIA.ToolKit.Application.Services;

/// <summary>
/// Service for file dialog operations (folder and file browsing)
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Opens a folder browser dialog
    /// </summary>
    /// <param name="initialPath">Initial path to display</param>
    /// <param name="description">Description shown in dialog</param>
    /// <returns>Selected folder path, or null if cancelled</returns>
    string? BrowseFolder(string initialPath, string description);

    /// <summary>
    /// Opens a file browser dialog
    /// </summary>
    /// <param name="filter">File extension filter (e.g., "btksettings")</param>
    /// <returns>Selected file path, or null if cancelled</returns>
    string? BrowseFile(string filter);

    /// <summary>
    /// Checks if a directory is empty
    /// </summary>
    /// <param name="path">Directory path to check</param>
    /// <returns>True if empty or doesn't exist, false otherwise</returns>
    bool IsDirectoryEmpty(string path);
}
