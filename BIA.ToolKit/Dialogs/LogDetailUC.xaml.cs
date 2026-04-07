namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.ViewModels;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;

    /// <summary>
    /// Interaction logic for LogDetailUC.
    /// Code-behind contains ONLY UI rendering logic (colored Inlines).
    /// DataContext is resolved via ViewModelLocator in XAML (Transient).
    /// </summary>
    public partial class LogDetailUC : Window
    {
        private LogDetailViewModel ViewModel => DataContext as LogDetailViewModel;

        public LogDetailUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Entry point called by DialogService / ConsoleWriter.
        /// Delegates data to ViewModel, then renders colored Inlines (pure UI).
        /// </summary>
        internal bool? ShowDialogWithMessages(List<ConsoleWriter.Message> messages)
        {
            ViewModel?.ShowDialog(messages);
            RenderMessages(messages);
            return ShowDialog();
        }

        private void RenderMessages(List<ConsoleWriter.Message> messages)
        {
            OutputDetailText.Inlines.Clear();
            if (messages == null) return;

            foreach (var msg in messages)
            {
                ConsoleWriter.AddMsgLine(OutputDetailText, OutputDetailTextViewer, msg.message, msg.color);
            }
        }
    }
}
