namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Helper;
    using System.Collections.Generic;
    using System.Windows;

    /// <summary>
    /// Interaction logic for Log Detail Window.
    /// Refactored to use LogDetailViewModel (MVVM pattern).
    /// </summary>
    public partial class LogDetailUC : Window
    {
        private LogDetailViewModel ViewModel => DataContext as LogDetailViewModel;

        public LogDetailUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows the dialog with the specified messages.
        /// </summary>
        /// <param name="messages">The console messages to display.</param>
        /// <returns>Dialog result.</returns>
        internal bool? ShowDialog(List<ConsoleWriter.Message> messages)
        {
            // Convert ConsoleWriter.Message to LogMessage and create ViewModel
            var logMessages = new List<LogMessage>();
            foreach (var msg in messages)
            {
                logMessages.Add(new LogMessage(msg.message, msg.color));
            }
            
            var viewModel = new LogDetailViewModel(logMessages)
            {
                // Provide clipboard action from View layer (platform-specific)
                ClipboardCopyAction = text => System.Windows.Forms.Clipboard.SetText(text)
            };
            DataContext = viewModel;

            // Render messages to UI
            foreach (var msg in messages)
            {
                ConsoleWriter.AddMsgLine(OutputDetailText, OutputDetailTextViewer, msg.message, msg.color);
            }

            return ShowDialog();
        }
    }
}
