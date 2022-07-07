namespace BIA.ToolKit.Helper
{
    using Microsoft.Win32;
    using System.IO;
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
    }
}
