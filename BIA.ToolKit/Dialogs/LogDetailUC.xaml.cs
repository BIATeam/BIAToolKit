namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.ViewModels;
    using System.Collections.Generic;
    using System.Windows.Controls;

    public partial class LogDetailUC : UserControl
    {
        private LogDetailViewModel ViewModel => DataContext as LogDetailViewModel;

        public LogDetailUC()
        {
            InitializeComponent();
        }

        internal void LoadMessages(List<ConsoleWriter.Message> messages, bool isDarkTheme = true)
        {
            ViewModel?.ShowDialog(messages);
            RenderMessages(messages, isDarkTheme);
        }

        private void RenderMessages(List<ConsoleWriter.Message> messages, bool isDarkTheme)
        {
            OutputDetailRichTextBox.Document.Blocks.Clear();
            if (messages == null) return;

            foreach (var msg in messages)
            {
                ConsoleWriter.AddMsgLine(OutputDetailRichTextBox, msg.message, msg.color, isDarkTheme: isDarkTheme);
            }
        }
    }
}
