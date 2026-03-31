namespace BIA.ToolKit.Infrastructure
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Helper class providing strongly-typed ViewModel access for UserControls
    /// Use this as a mixin pattern since XAML already defines UserControl as base class
    /// </summary>
    public static class UserControlViewModelHelper
    {
        /// <summary>
        /// Gets the strongly-typed ViewModel from DataContext
        /// Returns null if DataContext is not of type TViewModel
        /// </summary>
        public static TViewModel GetViewModel<TViewModel>(this UserControl control)
            where TViewModel : ObservableObject
        {
            return control.DataContext as TViewModel;
        }

        /// <summary>
        /// Sets up automatic ViewModel property notification when DataContext changes
        /// </summary>
        public static void SetupViewModelBinding<TViewModel>(this UserControl control, System.Action onViewModelChanged = null)
            where TViewModel : ObservableObject
        {
            control.DataContextChanged += (sender, e) =>
            {
                onViewModelChanged?.Invoke();
            };
        }
    }

    /// <summary>
    /// Base class for UserControls with strongly-typed ViewModel access
    /// Note: Cannot be used directly due to XAML partial class limitation
    /// Use the pattern: public TViewModel ViewModel => this.GetViewModel<TViewModel>();
    /// </summary>
    public abstract class UserControlWithViewModel<TViewModel> : UserControl
        where TViewModel : ObservableObject
    {
        /// <summary>
        /// Gets the strongly-typed ViewModel from DataContext
        /// Returns null if DataContext is not of type TViewModel
        /// </summary>
        public TViewModel ViewModel => this.GetViewModel<TViewModel>();

        protected UserControlWithViewModel()
        {
            this.SetupViewModelBinding<TViewModel>();
        }
    }
}
