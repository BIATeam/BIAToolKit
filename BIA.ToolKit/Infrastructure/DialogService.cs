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
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly Window owner;

        public DialogService(Window owner)
        {
            this.owner = owner;
        }

        public bool? ShowLogDetail(List<LogMessage> messages)
        {
            var wpfMessages = messages
                .Select(m => new ConsoleWriter.Message { message = m.Text, color = m.Color })
                .ToList();

            var dialog = new LogDetailUC { Owner = owner };
            return dialog.ShowDialogWithMessages(wpfMessages);
        }

        public string BrowseFolder(string defaultFolder, string description = null)
        {
            return FileDialog.BrowseFolder(defaultFolder, description);
        }

        public bool Confirm(string message, string title = "Warning")
        {
            var result = MessageBox.Show(owner, message, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            return result == MessageBoxResult.OK;
        }
    }
}
