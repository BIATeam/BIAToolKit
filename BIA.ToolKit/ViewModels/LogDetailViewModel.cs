namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Helper;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// ViewModel for LogDetailUC - displays detailed log messages.
    /// </summary>
    public partial class LogDetailViewModel(IDialogService dialogService) : ObservableObject
    {
        private readonly IDialogService dialogService = dialogService;
        [ObservableProperty]
        private List<ConsoleWriter.Message> messages = new();

        [ObservableProperty]
        private string outputText = string.Empty;

        /// <summary>
        /// Initializes the VM with messages.
        /// </summary>
        public void ShowDialog(List<ConsoleWriter.Message> messages)
        {
            Messages = messages ?? new List<ConsoleWriter.Message>();
            
            var textBuilder = new System.Text.StringBuilder();
            foreach (var msg in Messages)
            {
                textBuilder.AppendLine(msg.message);
            }
            OutputText = textBuilder.ToString();
        }

        /// <summary>
        /// Copies all messages to clipboard.
        /// </summary>
        [RelayCommand]
        private void CopyToClipboard()
        {
            if (Messages == null || !Messages.Any())
                return;

            var text = string.Join(Environment.NewLine, Messages.Select(m => m.message));
            dialogService.CopyToClipboard(text);
        }
    }
}
