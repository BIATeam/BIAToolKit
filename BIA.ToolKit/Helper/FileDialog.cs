namespace BIA.ToolKit.Helper
{
    using System.Windows.Controls;

    static class FileDialog
    {
        public static void BrowseFolder(TextBox destTextBox)
        {
            using (System.Windows.Forms.FolderBrowserDialog openFileDlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = openFileDlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    destTextBox.Text = openFileDlg.SelectedPath;
                }
            }
        }
    }
}
