namespace BIA.ToolKit.Application.ViewModel
{
    using System.IO;
    using System.Threading.Tasks;
    using BIA.ToolKit.Domain.Settings;
    using CommunityToolkit.Mvvm.Input;
    using Newtonsoft.Json;

    public partial class MainViewModel
    {
        // --- Console ---

        [RelayCommand]
        private void ClearConsole() => consoleWriter.Clear();

        [RelayCommand]
        private void CopyConsoleToClipboard() => consoleWriter.CopyToClipboard();

        // --- Browse ---

        [RelayCommand]
        private void BrowseCreateRootFolder()
        {
            Settings_RootProjectsPath = dialogService.BrowseFolder(Settings_RootProjectsPath, "Choose create project root path");
        }

        // --- Import / Export config ---

        [RelayCommand]
        private async Task ImportConfig()
        {
            string configFile = dialogService.BrowseFile(string.Empty, "btksettings");
            if (string.IsNullOrWhiteSpace(configFile) || !File.Exists(configFile))
                return;

            BIATKSettings config = JsonConvert.DeserializeObject<BIATKSettings>(File.ReadAllText(configFile));
            config.InitRepositoriesInterfaces();

            consoleWriter.AddMessageLine($"New configuration imported from {configFile}", "yellow");

            await ExecuteWithBusyAsync(async (ct) =>
            {
                await GetReleasesData(config, true, ct);

                settingsService.SetToolkitRepository(config.ToolkitRepository);
                settingsService.SetTemplateRepositories(config.TemplateRepositories);
                settingsService.SetCompanyFilesRepositories(config.CompanyFilesRepositories);
                settingsService.SetCreateProjectRootProjectPath(config.CreateProjectRootProjectsPath);
                settingsService.SetModifyProjectRootProjectPath(config.ModifyProjectRootProjectsPath);
                settingsService.SetAutoUpdate(config.AutoUpdate);
                settingsService.SetUseCompanyFiles(config.UseCompanyFiles);

                UpdateRepositories(settingsService.Settings);
            });
        }

        [RelayCommand]
        private void ExportConfig()
        {
            string targetDirectory = dialogService.BrowseFolder(string.Empty, "Choose export folder target");
            if (string.IsNullOrWhiteSpace(targetDirectory))
                return;

            string targetFile = Path.Combine(targetDirectory, "user.btksettings");
            string settings = JsonConvert.SerializeObject(settingsService.Settings);
            if (File.Exists(targetFile))
                File.Delete(targetFile);

            File.AppendAllText(targetFile, settings);

            consoleWriter.AddMessageLine($"Configuration exported in {targetFile}", "yellow");
        }

        // --- Tab selection ---

        [RelayCommand]
        private void OnTabSelected() => EnsureValidRepositoriesConfiguration();
    }
}
