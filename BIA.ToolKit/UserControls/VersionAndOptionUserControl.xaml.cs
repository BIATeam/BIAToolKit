namespace BIA.ToolKit.UserControls
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Infrastructure;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// Code-behind contains ONLY UI logic
    /// All business logic is in VersionAndOptionViewModel
    /// DataContext is resolved automatically via ViewModelLocator in XAML
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        /// <summary>
        /// Strongly-typed ViewModel accessor using extension method
        /// </summary>
        public VersionAndOptionViewModel ViewModel => this.GetViewModel<VersionAndOptionViewModel>();

        /// <summary>
        /// Public accessor to ViewModel for backward compatibility
        /// </summary>
        public VersionAndOptionViewModel vm => ViewModel;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, UIEventBroker uiEventBroker)
        {
            ViewModel?.Inject(repositoryService, settingsService, gitService, consoleWriter, uiEventBroker);
        }

        // Public API methods that delegate to ViewModel
        public void SelectVersion(string version)
        {
            ViewModel?.SelectVersion(version);
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            ViewModel?.SetCurrentProjectPath(path, mapCompanyFileVersion, mapFrameworkVersion, originFeatureSettings);
        }

        public void LoadVersionAndOption(bool mapCompanyFileVersion, bool mapFrameworkVersion)
        {
            ViewModel?.LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
        }

        public async Task FillVersionFolderPathAsync()
        {
            if (ViewModel != null)
            {
                await ViewModel.FillVersionFolderPathAsync();
            }
        }
    }
}
