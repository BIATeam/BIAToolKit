namespace BIA.ToolKit.Infrastructure
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Dialogs;
    using BIA.ToolKit.Helper;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    /// <summary>
    /// WPF implementation of IDialogService.
    /// Responsible for creating and showing dialog windows.
    /// Owner window is resolved lazily to avoid circular DI dependencies.
    /// </summary>
    public class DialogService : IDialogService
    {
        private Window Owner => Application.Current.MainWindow;

        public bool? ShowLogDetail(List<LogMessage> messages)
        {
            var wpfMessages = messages
                .Select(m => new ConsoleWriter.Message { message = m.Text, color = m.Color })
                .ToList();

            var dialog = new LogDetailUC { Owner = Owner };
            return dialog.ShowDialogWithMessages(wpfMessages);
        }

        public string BrowseFolder(string defaultFolder, string description = null)
        {
            return FileDialog.BrowseFolder(defaultFolder, description);
        }

        public bool Confirm(string message, string title = "Warning")
        {
            var result = MessageBox.Show(Owner, message, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            return result == MessageBoxResult.OK;
        }

        public void ShowMessage(string message, string title = null)
        {
            MessageBox.Show(Owner, message, title ?? string.Empty, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public string BrowseFile(string defaultFolder, string fileFilter = null)
        {
            return FileDialog.BrowseFile(defaultFolder, fileFilter);
        }
    }
}
