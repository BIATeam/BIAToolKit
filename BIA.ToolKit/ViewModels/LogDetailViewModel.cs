namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    /// <summary>
    /// ViewModel for LogDetailUC - displays detailed log messages.
    /// </summary>
    public partial class LogDetailViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<LogMessage> messages;

        public Action<string> ClipboardCopyAction { get; set; }

        public LogDetailViewModel()
        {
            Messages = new ObservableCollection<LogMessage>();
        }

        public LogDetailViewModel(List<LogMessage> initialMessages)
        {
            Messages = new ObservableCollection<LogMessage>(initialMessages);
        }

        [RelayCommand]
        private void CopyToClipboard(string text)
        {
            ClipboardCopyAction?.Invoke(text);
        }
    }

    /// <summary>
    /// Represents a single log message with its content and color.
    /// </summary>
    public class LogMessage
    {
        public string Content { get; set; }
        public string Color { get; set; }

        public LogMessage(string content, string color)
        {
            Content = content;
            Color = color;
        }
    }
}
