namespace BIA.ToolKit.Helper
{
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows.Controls;

    static class FileDialog
    {
        public static string BrowseFolder(string defaultFolder, string dialogDescription = null)
        {
            using var openFileDlg = new System.Windows.Forms.FolderBrowserDialog();
            if (Directory.Exists(defaultFolder))
            {
                openFileDlg.InitialDirectory = defaultFolder;
            }

            if(!string.IsNullOrWhiteSpace(dialogDescription))
            {
                openFileDlg.Description = dialogDescription;
                openFileDlg.UseDescriptionForTitle = true;
            }

            var result = openFileDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                return openFileDlg.SelectedPath;
            }
            else
            {
                return defaultFolder;
            }
        }

        public static string BrowseFile(string projectDir, string fileFilter = null)
        {
            var openFileDlg = new OpenFileDialog();
            if (!string.IsNullOrWhiteSpace(fileFilter))
                openFileDlg.Filter = $"{fileFilter.ToUpper()} Files|*.{fileFilter}";

            if (Directory.Exists(projectDir))
            {
                openFileDlg.InitialDirectory = projectDir;
                openFileDlg.RestoreDirectory = true;
            }

            var result = openFileDlg.ShowDialog();
            if (result == true)
            {
                return openFileDlg.FileName;
            }
            else
            {
                return projectDir;
            }
        }

        public static bool IsDirectoryEmpty(string path)
        {
            string[] files = System.IO.Directory.GetFiles(path);
            if (files.Length != 0) return false;

            List<string> dirs = System.IO.Directory.GetDirectories(path).ToList();

            if (dirs.Where(d => !d.EndsWith("\\.git")).Count() != 0) return false;

            return true;

            //return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
