namespace BIA.ToolKit.Helper
{
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
    }
}
