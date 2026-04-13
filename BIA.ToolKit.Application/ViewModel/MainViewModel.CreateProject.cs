namespace BIA.ToolKit.Application.ViewModel
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class MainViewModel
    {
        [ObservableProperty]
        private string createProjectName;

        /// <summary>
        /// The VersionAndOptionViewModel for the Create tab, set by the view.
        /// </summary>
        public VersionAndOptionViewModel CreateVersionAndOptionVM { get; set; }

        [RelayCommand]
        private async Task CreateProject()
        {
            if (string.IsNullOrEmpty(settingsService.Settings.CreateProjectRootProjectsPath))
            {
                dialogService.ShowMessage("Please select root path.");
                return;
            }
            if (string.IsNullOrEmpty(settingsService.Settings.CreateCompanyName))
            {
                dialogService.ShowMessage("Please select company name.");
                return;
            }
            if (string.IsNullOrEmpty(CreateProjectName))
            {
                dialogService.ShowMessage("Please select project name.");
                return;
            }
            if (CreateVersionAndOptionVM?.WorkTemplate == null)
            {
                dialogService.ShowMessage("Please select framework version.");
                return;
            }

            string projectPath = settingsService.Settings.CreateProjectRootProjectsPath + "\\" + CreateProjectName;
            if (Directory.Exists(projectPath) && !IsDirectoryEmpty(projectPath))
            {
                dialogService.ShowMessage("The project path is not empty : " + projectPath);
                return;
            }

            await ExecuteWithBusyAsync(async (ct) =>
            {
                await projectCreatorService.Create(
                    true,
                    projectPath,
                    new ProjectParameters
                    {
                        CompanyName = settingsService.Settings.CreateCompanyName,
                        ProjectName = CreateProjectName,
                        VersionAndOption = CreateVersionAndOptionVM.VersionAndOption,
                        AngularFronts = new List<string> { Constants.FolderAngular }
                    },
                    ct: ct);
            });
        }

        private static bool IsDirectoryEmpty(string path)
        {
            string[] files = Directory.GetFiles(path);
            if (files.Length != 0) return false;
            List<string> dirs = [.. Directory.GetDirectories(path)];
            if (dirs.Where(d => !d.EndsWith("\\.git")).Any()) return false;
            return true;
        }
    }
}
