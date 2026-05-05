namespace BIA.ToolKit.ViewModels
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Models.CrudGenerator;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public partial class OptionGeneratorViewModel : ObservableObject, IDisposable,
        IRecipient<ProjectChangedMessage>,
        IRecipient<SolutionClassesParsedMessage>
    {
        private readonly CSharpParserService parserService;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly ZipParserService zipService;
        private readonly GenerateCrudService crudService;
        private readonly SettingsService settingsService;
        private readonly GenerationHistoryService historyService;
        private bool disposed;
        private OptionGeneration optionGeneration;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public OptionGeneratorViewModel(
            CSharpParserService parserService,
            FileGeneratorService fileGeneratorService,
            IConsoleWriter consoleWriter,
            ZipParserService zipService,
            GenerateCrudService crudService,
            SettingsService settingsService,
            GenerationHistoryService historyService)
        {
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.zipService = zipService ?? throw new ArgumentNullException(nameof(zipService));
            this.crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            this.historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));

            ZipFeatureTypeList = [];
            Entities = [];
            EntityDisplayItems = [];

            // Subscribe to messenger events
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }

        public void Receive(ProjectChangedMessage message) => OnProjectChanged(message.Project);
        public void Receive(SolutionClassesParsedMessage message) => OnSolutionClassesParsed();

        #region CurrentProject

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsProjectCompatibleV7))]
        private Project currentProject;

        partial void OnCurrentProjectChanged(Project value)
        {
            BiaFronts.Clear();
            if (value != null)
            {
                foreach (string biaFront in value.BIAFronts)
                {
                    BiaFronts.Add(biaFront);
                }
                BiaFront = BiaFronts.FirstOrDefault();
            }
        }

        [ObservableProperty]
        private bool isProjectChosen;

        [ObservableProperty]
        private string biaFront;

        [ObservableProperty]
        private ObservableCollection<string> biaFronts = [];

        #endregion

        #region Entity

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private EntityInfo entity;

        [ObservableProperty]
        private ObservableCollection<EntityInfo> entities;

        [ObservableProperty]
        private ObservableCollection<string> entityDisplayItems;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private bool isEntityParsed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private string entityDisplayItemSelected;

        [ObservableProperty]
        private bool isGenerated;

        #endregion

        #region Entity Name

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private string entityNamePlural;

        #endregion

        #region Domain

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsButtonGenerateOptionEnable))]
        private string domain;

        #endregion

        #region ZipFile

        public List<ZipFeatureType> ZipFeatureTypeList { get; set; }

        #endregion

        #region Button
        public bool IsButtonGenerateOptionEnable
        {
            get
            {
                return IsEntityParsed
                    && !string.IsNullOrWhiteSpace(EntityNamePlural)
                    && !string.IsNullOrWhiteSpace(EntityDisplayItemSelected)
                    && !string.IsNullOrEmpty(Domain);
            }
        }
        #endregion

        #region CheckBox

        public bool IsProjectCompatibleV7 => Version.TryParse(CurrentProject?.FrameworkVersion, out Version version) && version.Major >= 7;

        [ObservableProperty]
        private bool useHubClient;

        #endregion

        #region Commands

        [RelayCommand]
        private void Generate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
                {
                    await fileGeneratorService.GenerateOptionAsync(new FileGeneratorOptionContext
                    {
                        CompanyName = CurrentProject.CompanyName,
                        ProjectName = CurrentProject.Name,
                        DomainName = Domain,
                        EntityName = Entity.Name,
                        EntityNamePlural = EntityNamePlural,
                        DisplayName = EntityDisplayItemSelected,
                        AngularFront = BiaFront,
                        GenerateBack = true,
                        GenerateFront = !string.IsNullOrWhiteSpace(BiaFront),
                        UseHubForClient = UseHubClient,
                        // BaseKeyType is the SOURCE entity's primary key type (Guid / int / long / string),
                        // not the OptionDto key type which is always int. It flows into
                        // OptionAppServiceBase<OptionDto, Entity, TKey, …> and BaseMapper<OptionDto, Entity, TKey>.
                        BaseKeyType = Entity.BaseKeyType ?? "int",
                    }, ct);

                    UpdateOptionGenerationHistory();
                    return;
                }

                consoleWriter.AddMessageLine("Project is not compatible for option generation.", "Red");
            }));
        }

        [RelayCommand]
        private void DeleteLastGeneration()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage((ct) =>
            {
                if (CurrentProject == null || Entity == null)
                {
                    consoleWriter.AddMessageLine("No project or entity selected.", "Yellow");
                    return Task.CompletedTask;
                }

                try
                {
                    optionGeneration ??= historyService.LoadOptionHistory(CurrentProject) ?? new OptionGeneration();
                    OptionGenerationHistory entry = optionGeneration.OptionGenerationHistory
                        .FirstOrDefault(h => string.Equals(h.EntityNameSingular, Entity.Name, StringComparison.OrdinalIgnoreCase));
                    if (entry == null)
                    {
                        consoleWriter.AddMessageLine($"No generation history found for {Entity.Name}.", "Yellow");
                        return Task.CompletedTask;
                    }

                    optionGeneration.OptionGenerationHistory.Remove(entry);
                    historyService.SaveOptionHistory(CurrentProject, optionGeneration);
                    consoleWriter.AddMessageLine($"Generation history removed for {Entity.Name}.", "Green");
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error deleting last generation: {ex.Message}", "Red");
                }

                return Task.CompletedTask;
            }));
        }

        [RelayCommand]
        private void DeleteBIAToolkitAnnotations()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                try
                {
                    var folders = new List<string>
                    {
                        Path.Combine(CurrentProject.Folder, "DotNet")
                    };

                    // Add Angular folders if they exist
                    foreach (var biaFront in CurrentProject.BIAFronts)
                    {
                        var angularFolder = Path.Combine(CurrentProject.Folder, biaFront, "src", "app");
                        if (Directory.Exists(angularFolder))
                        {
                            folders.Add(angularFolder);
                        }
                    }

                    await GenerateCrudService.DeleteBIAToolkitAnnotations(folders);
                    consoleWriter.AddMessageLine("BIA Toolkit annotations deleted successfully.", "Green");
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error deleting annotations: {ex.Message}", "Red");
                }
            }));
        }

        [RelayCommand]
        private void OnEntitySelectionChanged()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                if (Entity == null)
                {
                    IsEntityParsed = false;
                    EntityDisplayItems.Clear();
                    return;
                }

                try
                {
                    await ParseEntityFileAsync();
                }
                catch (Exception ex)
                {
                    DiagLog.Write($"Option.OnEntitySelectionChanged: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                    consoleWriter.AddMessageLine($"Error parsing entity: {ex.Message}", "Red");
                    IsEntityParsed = false;
                }
            }));
        }

        [RelayCommand]
        private void OnBiaFrontSelectionChanged(string biaFront)
        {
            // BiaFront property is already updated via binding
            // This command is for any additional logic needed when front changes
            OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
        }

        #endregion

        #region Event Handlers

        private void OnProjectChanged(Project project)
        {
            SetCurrentProject(project);
        }

        private void OnSolutionClassesParsed()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                await LoadEntitiesAsync();
            }));
        }

        #endregion

        #region Public Methods

        public void SetCurrentProject(Project project)
        {
            CurrentProject = project;
            IsProjectChosen = project != null;

            if (project != null)
            {
                LoadOptionGenerationHistory();
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadEntitiesAsync()
        {
            if (CurrentProject == null)
                return;

            try
            {
                var entities = await Task.Run(() => parserService.GetDomainEntities(CurrentProject).ToList());
                Entities = new ObservableCollection<EntityInfo>(entities);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading entities: {ex.Message}", "Red");
            }
        }

        private Task ParseEntityFileAsync()
        {
            if (Entity == null || CurrentProject == null)
                return Task.CompletedTask;

            try
            {
                // Use Entity.Properties — populated via Roslyn symbols (CSharpParserService.GetClassesInfoFromProject)
                // which include inherited members. Re-parsing the file would only see declared members.
                var displayItems = Entity.Properties
                    .Where(p => (p.Type == "string" || p.Type == "string?") && !p.Name.EndsWith("Id"))
                    .Select(p => p.Name)
                    .ToList();

                EntityDisplayItems = new ObservableCollection<string>(displayItems);
                EntityDisplayItemSelected = displayItems.FirstOrDefault();

                EntityNamePlural = Entity.Name.Pluralize();
                UpdateDomainPreSelection();

                IsEntityParsed = true;
            }
            catch (Exception ex)
            {
                DiagLog.Write($"Option.ParseEntityFileAsync: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                consoleWriter.AddMessageLine($"Error parsing entity file: {ex.Message}", "Red");
                IsEntityParsed = false;
            }

            return Task.CompletedTask;
        }

        private void UpdateDomainPreSelection()
        {
            Domain = null;

            if (Entity == null)
            {
                return;
            }

            var namespaceParts = Entity.Namespace.Split('.').ToList();
            var domainIndex = namespaceParts.IndexOf("Domain");
            if (domainIndex != -1 && domainIndex + 1 < namespaceParts.Count)
            {
                Domain = namespaceParts[domainIndex + 1];
            }
        }

        private void LoadOptionGenerationHistory()
        {
            if (CurrentProject == null)
                return;

            try
            {
                MigrateLegacyOptionHistoryFileIfPresent();
                WarnAboutLegacyPerEntityHistoryFiles();

                optionGeneration = historyService.LoadOptionHistory(CurrentProject);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading option generation history: {ex.Message}", "Yellow");
            }
        }

        /// <summary>
        /// Mirrors <c>CRUDGeneratorViewModel.SetGenerationSettings</c> behavior: if a legacy
        /// <c>OptionGeneration.json</c> file lives at the project root (older toolkit versions),
        /// move it under <c>.bia/</c> so the current pipeline finds it.
        /// </summary>
        private void MigrateLegacyOptionHistoryFileIfPresent()
        {
            string targetPath = historyService.GetOptionHistoryFilePath(CurrentProject);
            string legacyRootPath = Path.Combine(CurrentProject.Folder, Path.GetFileName(targetPath));
            if (File.Exists(legacyRootPath) && !File.Exists(targetPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Move(legacyRootPath, targetPath);
            }
        }

        /// <summary>
        /// Earlier V2.11.x builds wrote one <c>Option_&lt;Entity&gt;_&lt;timestamp&gt;.json</c>
        /// per generation in <c>.bia/History/</c>. Those files contain only EntityName/Domain/Date
        /// and cannot drive the regenerate-features migration. We do not delete them (keep user
        /// data intact) but emit a one-time warning so the dev knows to regenerate options through
        /// the new flow if they need migration support.
        /// </summary>
        private void WarnAboutLegacyPerEntityHistoryFiles()
        {
            string legacyFolder = Path.Combine(CurrentProject.Folder, Constants.FolderBia, "History");
            if (!Directory.Exists(legacyFolder))
                return;

            string[] legacyFiles = Directory.GetFiles(legacyFolder, "Option_*.json");
            if (legacyFiles.Length > 0)
            {
                consoleWriter.AddMessageLine(
                    $"Detected {legacyFiles.Length} legacy option history file(s) in '.bia/History/'. " +
                    "These are ignored by the new pipeline; regenerate the options to populate the consolidated " +
                    $"'{Path.GetFileName(historyService.GetOptionHistoryFilePath(CurrentProject))}' used by feature migration.",
                    "Orange");
            }
        }

        private void UpdateOptionGenerationHistory()
        {
            if (CurrentProject == null || Entity == null)
                return;

            try
            {
                optionGeneration ??= historyService.LoadOptionHistory(CurrentProject) ?? new OptionGeneration();

                OptionGenerationHistory entry = new()
                {
                    Date = DateTime.Now,
                    EntityNameSingular = Entity.Name,
                    EntityNamePlural = EntityNamePlural,
                    FrameworkVersion = CurrentProject.FrameworkVersion,
                    DisplayItem = EntityDisplayItemSelected,
                    Domain = Domain,
                    BiaFront = BiaFront,
                    UseHubClient = UseHubClient,
                    EntityNamespace = Entity.Namespace,
                    Mapping = new EntityMapping
                    {
                        Entity = Entity.Name,
                        Type = "DotNet",
                    },
                };

                entry.Generation.Add(new Generation
                {
                    FeatureType = FeatureType.Option.ToString(),
                    GenerationType = GenerationType.WebApi.ToString(),
                    Type = "DotNet",
                    Folder = Constants.FolderDotNet,
                });
                if (!string.IsNullOrWhiteSpace(BiaFront))
                {
                    entry.Generation.Add(new Generation
                    {
                        FeatureType = FeatureType.Option.ToString(),
                        GenerationType = GenerationType.Front.ToString(),
                        Type = "Angular",
                        Folder = BiaFront,
                    });
                }

                OptionGenerationHistory existing = optionGeneration.OptionGenerationHistory
                    .FirstOrDefault(h => string.Equals(h.EntityNameSingular, entry.EntityNameSingular, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    optionGeneration.OptionGenerationHistory.Remove(existing);
                }
                optionGeneration.OptionGenerationHistory.Add(entry);

                historyService.SaveOptionHistory(CurrentProject, optionGeneration);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error saving option generation history: {ex.Message}", "Yellow");
            }
        }

        #endregion
    }
}
