namespace BIA.ToolKit.Infrastructure.Services
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Implementation of IFileDialogService using WinForms dialogs.
    /// Centralizes file and folder browsing logic.
    /// </summary>
    public class FileDialogService : IFileDialogService
    {
        /// <summary>
        /// Opens a folder browser dialog and returns the selected path.
        /// </summary>
        public string BrowseFolder(string initialPath, string title)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = title,
                SelectedPath = initialPath ?? string.Empty,
                ShowNewFolderButton = false
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Opens a file open dialog and returns the selected path.
        /// </summary>
        public string BrowseFile(string filter)
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = filter ?? "All Files (*.*)|*.*",
                CheckFileExists = true
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }

            return null;
        }

        /// <summary>
        /// Opens a file save dialog and returns the selected path.
        /// </summary>
        public string SaveFile(string fileName, string filter)
        {
            using (var dialog = new SaveFileDialog
            {
                FileName = fileName ?? string.Empty,
                Filter = filter ?? "All Files (*.*)|*.*"
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public bool IsDirectoryEmpty(string path)
        {
            if (!System.IO.Directory.Exists(path))
                return true;

            return !System.IO.Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
