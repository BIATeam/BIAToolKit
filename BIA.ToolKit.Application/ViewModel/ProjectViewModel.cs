namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.RegenerateFeatures;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
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
    public class ProjectViewModel : ObservableObject
    {
        private UIEventBroker eventBroker;
        private FileGeneratorService fileGeneratorService;
        private SettingsService settingsService;
        private ProjectService projectService;

        private Project currentProject;

        public void Inject(UIEventBroker eventBroker, FileGeneratorService fileGeneratorService,
            SettingsService settingsService, ProjectService projectService)
        {
            this.eventBroker = eventBroker;
            this.fileGeneratorService = fileGeneratorService;
            this.settingsService = settingsService;
            this.projectService = projectService;

            eventBroker.OnSettingsUpdated += EventBroker_OnSettingsUpdated;
        }

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            RaisePropertyChanged(nameof(RootProjectsPath));
            RefreshProjetsList();
        }

        public ICommand RefreshProjectInformationsCommand => new RelayCommand((_) => RefreshProjectInformations());

        #region Project compatibility flags

        private bool isFileGeneratorServiceInit;
        public bool IsFileGeneratorServiceInit
        {
            get => isFileGeneratorServiceInit;
            set
            {
                isFileGeneratorServiceInit = value;
                RaisePropertyChanged(nameof(IsFileGeneratorServiceInit));
            }
        }

        private bool isProjectCompatibleCrudGenerator;
        public bool IsProjectCompatibleCrudGenerator
        {
            get => isProjectCompatibleCrudGenerator;
            set
            {
                isProjectCompatibleCrudGenerator = value;
                RaisePropertyChanged(nameof(IsProjectCompatibleCrudGenerator));
            }
        }

        private bool isProjectCompatibleRegenerateFeatures;
        public bool IsProjectCompatibleRegenerateFeatures
        {
            get => isProjectCompatibleRegenerateFeatures;
            set
            {
                isProjectCompatibleRegenerateFeatures = value;
                RaisePropertyChanged(nameof(IsProjectCompatibleRegenerateFeatures));
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
                    RaisePropertyChanged(nameof(Projects));
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

                eventBroker.RequestExecuteActionWithWaiter(async (ct) =>
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
                        await projectService.LoadProject(project, ct);
                    }

                    await InitFileGeneratorServiceFromProject(project, ct);
                    CurrentProject = project;

                    if (CurrentProject is not null)
                    {
                        await projectService.ParseProject(project, ct);
                    }

                    RaisePropertyChanged(nameof(Folder));
                });
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
                    eventBroker.NotifyProjectChanged(value);
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
            RaisePropertyChanged(nameof(CurrentProject));
            RaisePropertyChanged(nameof(FrameworkVersion));
            RaisePropertyChanged(nameof(CompanyName));
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(IsProjectSelected));
            RaisePropertyChanged(nameof(BIAFronts));
        }

        #endregion

        #region Private loading methods

        private async Task InitFileGeneratorServiceFromProject(Project project, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            await fileGeneratorService.Init(project, cancellationToken: ct);
            IsFileGeneratorServiceInit = fileGeneratorService.IsInit;
            IsProjectCompatibleCrudGenerator = GenerateCrudService.IsProjectCompatible(project);
            IsProjectCompatibleRegenerateFeatures = RegenerateFeaturesDiscoveryService.IsProjectCompatibleForRegenerateFeatures(project);
        }

        private void RefreshProjectInformations()
        {
            eventBroker.RequestExecuteActionWithWaiter(async (ct) =>
            {
                await projectService.LoadProject(CurrentProject, ct);
                await InitFileGeneratorServiceFromProject(CurrentProject, ct);
                await projectService.ParseProject(CurrentProject, ct);
                RefreshProjectDisplayProperties();
            });
        }

        #endregion
    }
}
