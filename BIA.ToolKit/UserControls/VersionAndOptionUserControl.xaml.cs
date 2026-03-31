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
    /// </summary>
    public partial class VersionAndOptionUserControl : UserControl
    {
        public VersionAndOptionViewModel vm;

        public VersionAndOptionUserControl()
        {
            InitializeComponent();
            vm = (VersionAndOptionViewModel)base.DataContext;
        }

        public void Inject(RepositoryService repositoryService, GitService gitService, IConsoleWriter consoleWriter, SettingsService settingsService, UIEventBroker uiEventBroker)
        {
            vm.Inject(repositoryService, settingsService, gitService, consoleWriter, uiEventBroker);
        }

        // Public API methods that delegate to ViewModel
        public void SelectVersion(string version)
        {
            vm.SelectVersion(version);
        }

        public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion, IEnumerable<FeatureSetting> originFeatureSettings = null)
        {
            vm.SetCurrentProjectPath(path, mapCompanyFileVersion, mapFrameworkVersion, originFeatureSettings);
        }

        public void LoadVersionAndOption(bool mapCompanyFileVersion, bool mapFrameworkVersion)
        {
            vm.LoadVersionAndOption(mapCompanyFileVersion, mapFrameworkVersion);
        }

        public async Task FillVersionFolderPathAsync()
        {
            await vm.FillVersionFolderPathAsync();
        }
    }
}
