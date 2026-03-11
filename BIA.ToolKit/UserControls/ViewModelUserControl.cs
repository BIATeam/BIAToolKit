namespace BIA.ToolKit.UserControls
{
    using BIA.ToolKit.Application.ViewModel.Base;
    using System.Windows.Controls;

    /// <summary>
    /// Base class for all UserControls bound to a <see cref="ViewModelBase"/>.
    /// Automatically calls <see cref="ViewModelBase.Initialize"/> on <c>Loaded</c>
    /// and <see cref="ViewModelBase.Cleanup"/> on <c>Unloaded</c>, using the
    /// DataContext set by the WPF DataTemplate mechanism.
    /// Exposes a typed <see cref="ViewModel"/> property for use in code-behind.
    /// </summary>
    public class ViewModelUserControl<TViewModel> : UserControl
        where TViewModel : ViewModelBase
    {
        public ViewModelUserControl()
        {
            Loaded   += (_, _) => ViewModel?.Initialize();
            Unloaded += (_, _) => ViewModel?.Cleanup();
        }

        /// <summary>Gets the DataContext cast to the specific ViewModel type.</summary>
        protected TViewModel ViewModel => DataContext as TViewModel;
    }
}
