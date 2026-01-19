namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Helper;
    using System;
    using System.Collections.Generic;
    using System.Windows;

    /// <summary>
    /// Interaction logic for Log Detail Window
    /// </summary>
    public partial class LogDetailUC : Window
    {
        List<ConsoleWriter.Message> Messages = new List<ConsoleWriter.Message>();
        public LogDetailUC()
        {
            InitializeComponent();
        }

        internal bool? ShowDialog(List<ConsoleWriter.Message> messages)
        {
            Messages = messages;
            foreach (ConsoleWriter.Message msg in messages)
            {
                ConsoleWriter.AddMsgLine(OutputDetailText, OutputDetailTextViewer, msg.message, msg.color);
            }
            return ShowDialog();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            string text = string.Empty;
            foreach (ConsoleWriter.Message msg in Messages)
            {
                text += msg.message;
                text += Environment.NewLine;
            }
            System.Windows.Forms.Clipboard.SetText(text);
        }
    }
}
