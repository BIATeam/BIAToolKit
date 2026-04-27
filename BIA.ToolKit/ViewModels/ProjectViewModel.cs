namespace BIA.ToolKit.ViewModels
{
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
    public partial class ProjectViewModel : ObservableObject,
        IRecipient<SettingsUpdatedMessage>,
        IRecipient<EntityGenerationCompletedMessage>
    {
        private readonly FileGeneratorService fileGeneratorService;
        private readonly SettingsService settingsService;
        private readonly ProjectService projectService;

        public ProjectViewModel(FileGeneratorService fileGeneratorService,
            SettingsService settingsService, ProjectService projectService)
        {
            this.fileGeneratorService = fileGeneratorService;
            this.settingsService = settingsService;
            this.projectService = projectService;

            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Receive(SettingsUpdatedMessage message) => EventBroker_OnSettingsUpdated(message.Settings);

        public void Receive(EntityGenerationCompletedMessage message) => RefreshProjectInformations();

        private void EventBroker_OnSettingsUpdated(IBIATKSettings settings)
        {
            OnPropertyChanged(nameof(RootProjectsPath));
            RefreshProjetsList();
        }

        public ICommand RefreshProjectInformationsCommand => new RelayCommand(() => RefreshProjectInformations());

        #region Project compatibility flags

        [ObservableProperty]
        private bool isFileGeneratorServiceInit;

        [ObservableProperty]
        private bool isProjectCompatibleCrudGenerator;

        [ObservableProperty]
        private bool isProjectCompatibleRegenerateFeatures;

        #endregion

        #region Project list

        [ObservableProperty]
        private ObservableCollection<string> projects = [];

        [RelayCommand]
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
                        await projectService.LoadProject(project, ct);
                    }

                    await InitFileGeneratorServiceFromProject(project, ct);
                    CurrentProject = project;

                    if (CurrentProject is not null)
                    {
                        await projectService.ParseProject(project, ct);
                    }

                    OnPropertyChanged(nameof(Folder));
                }));
            }
        }

        #endregion

        #region Current project

        public System.Collections.Generic.Dictionary<string, Application.Helper.NamesAndVersionResolver> CurrentProjectDetections { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FrameworkVersion))]
        [NotifyPropertyChangedFor(nameof(CompanyName))]
        [NotifyPropertyChangedFor(nameof(Name))]
        [NotifyPropertyChangedFor(nameof(IsProjectSelected))]
        [NotifyPropertyChangedFor(nameof(BIAFronts))]
        private Project currentProject;

        partial void OnCurrentProjectChanged(Project value)
        {
            WeakReferenceMessenger.Default.Send(new ProjectChangedMessage(value));
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

        /// <summary>
        /// Forces re-evaluation of computed properties when <see cref="CurrentProject"/> is mutated
        /// in place (same reference). [NotifyPropertyChangedFor] only fires when the reference changes.
        /// </summary>
        private void RefreshProjectDisplayProperties()
        {
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
            await fileGeneratorService.Init(project, cancellationToken: ct);
            IsFileGeneratorServiceInit = fileGeneratorService.IsInit;
            IsProjectCompatibleCrudGenerator = GenerateCrudService.IsProjectCompatible(project);
            IsProjectCompatibleRegenerateFeatures = RegenerateFeaturesDiscoveryService.IsProjectCompatibleForRegenerateFeatures(project);
        }

        private void RefreshProjectInformations()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                await projectService.LoadProject(CurrentProject, ct);
                await InitFileGeneratorServiceFromProject(CurrentProject, ct);
                await projectService.ParseProject(CurrentProject, ct);
                RefreshProjectDisplayProperties();
            }));
        }

        #endregion
    }
}
