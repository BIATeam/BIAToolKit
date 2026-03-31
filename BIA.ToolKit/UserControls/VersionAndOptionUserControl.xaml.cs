namespace BIA.ToolKit.UserControls
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Interaction logic for VersionAndOptionView.xaml
    /// Code-behind contains ONLY UI logic
    /// All business logic is in VersionAndOptionViewModel
    /// DataContext is resolved automatically via ViewModelLocator in XAML
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        /// <summary>
        /// Public accessor to ViewModel for backward compatibility
        /// Prefer using DataContext pattern in new code
        /// </summary>
        public VersionAndOptionViewModel vm => DataContext as VersionAndOptionViewModel;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, UIEventBroker uiEventBroker)
        {
            if (DataContext is VersionAndOptionViewModel viewModel)
            {
                viewModel.Inject(repositoryService, settingsService, gitService, consoleWriter, uiEventBroker);
            }
        }

        // Public API methods that delegate to ViewModel
        public void SelectVersion(string version)
        {
            if (DataContext is VersionAndOptionViewModel viewModel)
            {
                viewModel.SelectVersion(version);
            }
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            if (DataContext is VersionAndOptionViewModel viewModel)
            {
                viewModel.SetCurrentProjectPath(path, mapCompanyFileVersion, mapFrameworkVersion, originFeatureSettings);
            }
        }

        public void LoadVersionAndOption(bool mapCompanyFileVersion, bool mapFrameworkVersion)
        {
            if (DataContext is VersionAndOptionViewModel viewModel)
            {
                viewModel.LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
            }
        }

        public async Task FillVersionFolderPathAsync()
        {
            if (DataContext is VersionAndOptionViewModel viewModel)
            {
                await viewModel.FillVersionFolderPathAsync();
            }
        }
    }
}
