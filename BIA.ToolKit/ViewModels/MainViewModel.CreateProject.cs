namespace BIA.ToolKit.ViewModels
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

                await TryFixUsingsAfterCreate(projectPath, ct);
            });
        }

        /// <summary>
        /// Resolves missing using statements in the freshly-created project (CS0103 errors
        /// like 'BiaPermissionId does not exist' that the BIA template can introduce).
        /// Best-effort: failures here do not invalidate the creation — the user can always
        /// run the manual "Fix Usings" button on the Modify Project tab.
        /// </summary>
        private async Task TryFixUsingsAfterCreate(string projectPath, System.Threading.CancellationToken ct)
        {
            try
            {
                string slnPath = Directory
                    .EnumerateFiles(projectPath, "*.sln", SearchOption.AllDirectories)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(slnPath))
                {
                    consoleWriter.AddMessageLine(
                        "Skipping post-creation Fix Usings: no .sln found in the new project.",
                        "orange");
                    return;
                }

                consoleWriter.AddMessageLine(
                    "Resolving missing usings on the freshly-created project (this can take a few minutes)...",
                    "pink");

                await cSharpParserService.LoadSolution(slnPath, ct);
                await cSharpParserService.FixUsings(ct);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                consoleWriter.AddMessageLine(
                    $"Post-creation Fix Usings failed ({ex.Message}). Run it manually from Modify Project > 6 - Fix Usings.",
                    "orange");
            }
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
