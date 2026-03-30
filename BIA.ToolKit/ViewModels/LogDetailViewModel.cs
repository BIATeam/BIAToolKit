namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Helper;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// ViewModel for LogDetailUC - displays detailed log messages
    /// Pilot ViewModel for CommunityToolkit.Mvvm migration
    /// </summary>
    public partial class LogDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<ConsoleWriter.Message> messages = new();

        [ObservableProperty]
        private string outputText = string.Empty;

        [ObservableProperty]
        private bool isVisible = false;

        /// <summary>
        /// Shows the dialog with messages
        /// This is the entry point - ConsoleWriter calls this method
        /// </summary>
        public void ShowDialog(List<ConsoleWriter.Message> messages)
        {
            Messages = messages ?? new List<ConsoleWriter.Message>();
            
            // Format messages for display
            var textBuilder = new System.Text.StringBuilder();
            foreach (var msg in Messages)
            {
                textBuilder.AppendLine(msg.message);
            }
            OutputText = textBuilder.ToString();
            
            // Trigger the view to show the dialog
            IsVisible = true;
        }

        /// <summary>
        /// Copies all messages to clipboard
        /// </summary>
        [RelayCommand]
        private void CopyToClipboard()
        {
            if (Messages == null || !Messages.Any())
                return;

            var text = string.Join(Environment.NewLine, Messages.Select(m => m.message));
            Clipboard.SetText(text);
        }

        /// <summary>
        /// Closes the dialog
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }
    }
}
