namespace BIA.ToolKit.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// Available pages in the navigation rail.
    /// </summary>
    public enum AppPage
    {
        Creation,
        Migration,
        Generation,
        Configuration
    }

    public partial class MainViewModel
    {
        [ObservableProperty]
        private AppPage selectedPage = AppPage.Generation;

        partial void OnSelectedPageChanged(AppPage value)
        {
            // Validate repository configuration when leaving Config
            if (value != AppPage.Configuration && IsInitialized)
                EnsureValidRepositoriesConfiguration();
        }

        /// <summary>
        /// Navigates to the Configuration page. Called via messenger
        /// when repository configuration is invalid.
        /// </summary>
        public void NavigateToConfig()
        {
            SelectedPage = AppPage.Configuration;
        }
    }
}
