namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Helper;
    using BIA.ToolKit.ViewModels;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;

    /// <summary>
    /// Interaction logic for LogDetailUC
    /// Code-behind contains ONLY UI logic
    /// All business logic is in LogDetailViewModel
    /// DataContext is resolved automatically via ViewModelLocator in XAML
    /// </summary>
    public partial class LogDetailUC : Window
    {
        public LogDetailUC()
        {
            InitializeComponent();

            // Listen to ViewModel property changes for UI updates
            if (DataContext is LogDetailViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogDetailViewModel.Messages) && DataContext is LogDetailViewModel vm)
            {
                // UI Logic: Update TextBlock with colored messages when Messages change
                OutputDetailText.Inlines.Clear();
                foreach (ConsoleWriter.Message msg in vm.Messages)
                {
                    ConsoleWriter.AddMsgLine(OutputDetailText, OutputDetailTextViewer, msg.message, msg.color);
                }
            }
        }

        /// <summary>
        /// Entry point called by ConsoleWriter
        /// Delegates to ViewModel immediately
        /// </summary>
        internal bool? ShowDialog(List<ConsoleWriter.Message> messages)
        {
            if (DataContext is LogDetailViewModel viewModel)
            {
                viewModel.ShowDialog(messages);
            }
            return ShowDialog();
        }
    }
}
