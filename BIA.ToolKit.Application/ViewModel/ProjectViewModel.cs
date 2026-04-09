namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.RegenerateFeatures;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Input;

    /// <summary>
    /// Singleton ViewModel shared between ModifyProjectUC and GenerateUC.
    /// Manages project selection, loading, and parsed project state.
    /// </summary>
    public class ProjectViewModel : ObservableObject,
        IRecipient<SettingsUpdatedMessage>
    {
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly SettingsService settingsService;
        private readonly CSharpParserService parserService;

        private Project currentProject;

        public ProjectViewModel(FileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter,
            SettingsService settingsService, CSharpParserService parserService)
        {
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;
            this.settingsService = settingsService;
            this.parserService = parserService;

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(SettingsUpdatedMessage message) => EventBroker_OnSettingsUpdated(message.Settings);

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            OnPropertyChanged(nameof(RootProjectsPath));
            RefreshProjetsList();
        }

        public ICommand RefreshProjectInformationsCommand => new RelayCommand(() => RefreshProjectInformations());

        #region Project compatibility flags

        private bool isFileGeneratorServiceInit;
        public bool IsFileGeneratorServiceInit
        {
            get => isFileGeneratorServiceInit;
            set
            {
                isFileGeneratorServiceInit = value;
                OnPropertyChanged(nameof(IsFileGeneratorServiceInit));
            }
        }

        private bool isProjectCompatibleCrudGenerator;
        public bool IsProjectCompatibleCrudGenerator
        {
            get => isProjectCompatibleCrudGenerator;
            set
            {
                isProjectCompatibleCrudGenerator = value;
                OnPropertyChanged(nameof(IsProjectCompatibleCrudGenerator));
            }
        }

        private bool isProjectCompatibleRegenerateFeatures;
        public bool IsProjectCompatibleRegenerateFeatures
        {
            get => isProjectCompatibleRegenerateFeatures;
            set
            {
                isProjectCompatibleRegenerateFeatures = value;
                OnPropertyChanged(nameof(IsProjectCompatibleRegenerateFeatures));
            }
        }

        #endregion

        #region Project list

        private ObservableCollection<string> projects = [];
        public ObservableCollection<string> Projects
        {
            get => projects;
            set
            {
                if (projects != value)
                {
                    projects = value;
                    OnPropertyChanged(nameof(Projects));
                }
            }
        }

        public void RefreshProjetsList()
        {
            if (!Directory.Exists(RootProjectsPath))
                return;

            DirectoryInfo di = new(RootProjectsPath);
            DirectoryInfo[] versionDirectories = di.GetDirectories("*", SearchOption.TopDirectoryOnly);

            var newProjects = versionDirectories.Select(d => d.Name).ToList();

            if (!newProjects.Select(x => Path.Combine(RootProjectsPath, x)).Contains(CurrentProject?.Folder))
            {
                Folder = null;
            }

            for (int i = 0; i < newProjects.Count; i++)
            {
                if (Projects.Any(x => x == newProjects[i]))
                    continue;
                Projects.Insert(i, newProjects[i]);
            }

            for (int i = 0; i < Projects.Count; i++)
            {
                if (newProjects.Any(x => x == Projects[i]))
                    continue;
                Projects.RemoveAt(i);
                i--;
            }
        }

        #endregion

        #region Root path & folder selection

        public string RootProjectsPath
        {
            get => settingsService?.Settings?.ModifyProjectRootProjectsPath;
            set
            {
                // Setting the value triggers NotifySettingsUpdated in SettingsService,
                // which fires EventBroker_OnSettingsUpdated and calls RefreshProjetsList().
                settingsService.SetModifyProjectRootProjectPath(value);
            }
        }

        public string Folder
        {
            get => Path.GetFileName(CurrentProject?.Folder);
            set
            {
                if (value == Folder)
                    return;

                WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
                {
                    IsFileGeneratorServiceInit = false;
                    IsProjectCompatibleCrudGenerator = false;

                    Project project = null;
                    if (value is not null)
                    {
                        project = new Project
                        {
                            Name = value,
                            Folder = Path.Combine(RootProjectsPath, value)
                        };
                        await LoadProject(project, ct);
                    }

                    await InitFileGeneratorServiceFromProject(project, ct);
                    CurrentProject = project;

                    if (CurrentProject is not null)
                    {
                        await ParseProject(project, ct);
                    }

                    OnPropertyChanged(nameof(Folder));
                }));
            }
        }

        #endregion

        #region Current project

        public Dictionary<string, NamesAndVersionResolver> CurrentProjectDetections { get; set; }

        public Project CurrentProject
        {
            get => currentProject;
            set
            {
                if (currentProject != value)
                {
                    currentProject = value;
                    RefreshProjectDisplayProperties();
                    WeakReferenceMessenger.Default.Send(new ProjectChangedMessage(value));
                }
            }
        }

        public bool IsProjectSelected => CurrentProject != null;

        public string FrameworkVersion =>
            string.IsNullOrEmpty(CurrentProject?.FrameworkVersion) ? "???" : CurrentProject.FrameworkVersion;

        public string CompanyName =>
            string.IsNullOrEmpty(CurrentProject?.CompanyName) ? "???" : CurrentProject.CompanyName;

        public string Name =>
            string.IsNullOrEmpty(CurrentProject?.Name) ? "???" : CurrentProject.Name;

        public string BIAFronts =>
            CurrentProject?.BIAFronts?.Count > 0
                ? string.Join(", ", CurrentProject.BIAFronts)
                : "???";

        private void RefreshProjectDisplayProperties()
        {
            OnPropertyChanged(nameof(CurrentProject));
            OnPropertyChanged(nameof(FrameworkVersion));
            OnPropertyChanged(nameof(CompanyName));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(IsProjectSelected));
            OnPropertyChanged(nameof(BIAFronts));
        }

        #endregion

        #region Private loading methods

        private async Task InitFileGeneratorServiceFromProject(Project project, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await fileGeneratorService.Init(project);
            IsFileGeneratorServiceInit = fileGeneratorService.IsInit;
            IsProjectCompatibleCrudGenerator = GenerateCrudService.IsProjectCompatible(project);
            IsProjectCompatibleRegenerateFeatures = RegenerateFeaturesDiscoveryService.IsProjectCompatibleForRegenerateFeatures(project);
        }

        private async Task LoadProject(Project project, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                consoleWriter.AddMessageLine($"Loading project {project.Name}", "pink");

                consoleWriter.AddMessageLine("List project's files...", "darkgray");
                await project.ListProjectFiles();
                project.SolutionPath = project.ProjectFiles.FirstOrDefault(path =>
                    path.EndsWith($"{project.Name}.sln", StringComparison.InvariantCultureIgnoreCase));
                consoleWriter.AddMessageLine("Project's files listed", "lightgreen");

                consoleWriter.AddMessageLine("Resolving names and version...", "darkgray");

                NamesAndVersionResolver nvResolverOldVersions = new()
                {
                    ConstantFileRegExpPath = @"\\.*\\(.*)\.(.*)\.Common\\Constants\.cs$",
                    ConstantFileNameSearchPattern = "Constants.cs",
                    ConstantFileNamespace = @"^namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.",
                    ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                    FrontFileRegExpPath = null,
                    FrontFileUsingBiaNg = null,
                    FrontFileBiaNgImportRegExp = null,
                    FrontFileNameSearchPattern = null
                };

                NamesAndVersionResolver nvResolver = new()
                {
                    ConstantFileRegExpPath = @"\\DotNet\\(.*)\.(.*)\.Crosscutting\.Common\\[Bia\\]*Constants\.cs$",
                    ConstantFileNameSearchPattern = "Constants.cs",
                    ConstantFileNamespace = @"^namespace\s+([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\.",
                    ConstantFileRegExpVersion = @" FrameworkVersion[\s]*=[\s]* ""([0-9]+\.[0-9]+\.[0-9]+)""[\s]*;[\s]*$",
                    FrontFileRegExpPath =
                    [
                        @"\\(.*)\\src\\app\\core\\bia-core\\bia-core.module\.ts$",
                        @"\\(.*)\\packages\\bia-ng\\core\\bia-core.module\.ts$"
                    ],
                    FrontFileUsingBiaNg = @"\\(?!.*(?:\\node_modules\\|\\dist\\|\\\.angular\\))(.*)\\package\.json$",
                    FrontFileBiaNgImportRegExp = "\"@bia-team/bia-ng\":",
                    FrontFileNameSearchPattern = "bia-core.module.ts"
                };

                var resolverTask = Task.Run(() => nvResolver.ResolveNamesAndVersion(project), ct);
                var resolverOldVersionsTask = Task.Run(() => nvResolverOldVersions.ResolveNamesAndVersion(project), ct);
                await Task.WhenAll(resolverTask, resolverOldVersionsTask);

                consoleWriter.AddMessageLine("Names and version resolved", "lightgreen");

                if (project.BIAFronts.Count == 0)
                {
                    consoleWriter.AddMessageLine("Unable to find any BIA front folder for this project", "orange");
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project : {ex.Message}", "red");
            }
        }

        private async Task ParseProject(Project project, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await parserService.LoadSolution(project.SolutionPath, ct);
                await parserService.ParseSolutionClasses(ct);
            }
            catch (OperationCanceledException)
            {
                consoleWriter.AddMessageLine("Operation cancelled.", "Yellow");
                throw;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error while loading project solution : {ex.Message}", "red");
            }
        }

        private void RefreshProjectInformations()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                await LoadProject(CurrentProject, ct);
                await InitFileGeneratorServiceFromProject(CurrentProject, ct);
                await ParseProject(CurrentProject, ct);
                RefreshProjectDisplayProperties();
            }));
        }

        #endregion
    }
}
