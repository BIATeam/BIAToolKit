namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    /// <summary>
    /// ViewModel for the Log Detail dialog.
    /// Handles message display and clipboard operations.
    /// </summary>
    public partial class LogDetailViewModel : ObservableObject
    {
        private readonly List<LogMessage> messages;

        /// <summary>
        /// Action delegate for clipboard operations. Set by the View layer.
        /// </summary>
        public Action<string> ClipboardCopyAction { get; set; }

        public LogDetailViewModel(List<LogMessage> messages)
        {
            this.messages = messages ?? new List<LogMessage>();
        }

        /// <summary>
        /// Gets the messages to display.
        /// </summary>
        public IReadOnlyList<LogMessage> Messages => messages;

        /// <summary>
        /// Gets the formatted text for clipboard copy.
        /// </summary>
        public string GetFormattedText()
        {
            var sb = new StringBuilder();
            foreach (var msg in messages)
            {
                sb.AppendLine(msg.Text);
            }
            return sb.ToString();
        }

        [RelayCommand]
        private void CopyToClipboard()
        {
            var text = GetFormattedText();
            if (!string.IsNullOrEmpty(text))
            {
                ClipboardCopyAction?.Invoke(text);
            }
        }
    }

    /// <summary>
    /// Represents a log message with text and color.
    /// </summary>
    public class LogMessage
    {
        public string Text { get; set; }
        public string Color { get; set; }

        public LogMessage(string text, string color = null)
        {
            Text = text;
            Color = color;
        }
    }
}
