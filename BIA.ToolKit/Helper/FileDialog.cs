namespace BIA.ToolKit.Helper
{
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows.Controls;

    static class FileDialog
    {
        public static void BrowseFolder(TextBox destTextBox)
        {
            using (System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (Directory.Exists(destTextBox.Text))
                {
                    openFileDlg.SelectedPath = destTextBox.Text;
                }

                var result = openFileDlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    destTextBox.Text = openFileDlg.SelectedPath;
                }
            }
        }
        public static string BrowseFolder(string defaultFolder)
        {
            using (System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (Directory.Exists(defaultFolder))
                {
                    openFileDlg.SelectedPath = defaultFolder;
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
        }


        public static bool BrowseFile(TextBox destTextBox, string projectDir)
        {
            OpenFileDialog openFileDlg = new OpenFileDialog();

            if (Directory.Exists(projectDir))
            {
                openFileDlg.InitialDirectory = projectDir;
                openFileDlg.RestoreDirectory = true;
            }

            var result = openFileDlg.ShowDialog();
            if (result == true)
            {
                destTextBox.Text = openFileDlg.FileName;
                return true;
            }
            return false;

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
