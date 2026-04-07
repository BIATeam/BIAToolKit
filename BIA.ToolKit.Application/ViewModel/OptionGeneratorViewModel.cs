namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.FeatureData;
    using Humanizer;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
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
        private bool disposed;
        private OptionGenerationInfo optionHistory;
        private string optionHistoryFileName;

        /// <summary>
        /// Constructor with dependency injection.
        /// </summary>
        public OptionGeneratorViewModel(
            CSharpParserService parserService,
            FileGeneratorService fileGeneratorService,
            IConsoleWriter consoleWriter,
            ZipParserService zipService,
            GenerateCrudService crudService,
            SettingsService settingsService)
        {
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.zipService = zipService ?? throw new ArgumentNullException(nameof(zipService));
            this.crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

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
        private Project currentProject;
        public Project CurrentProject
        {
            get => currentProject;
            set
            {
                currentProject = value;
                BiaFronts.Clear();
                if (currentProject != null)
                {
                    foreach (string biaFront in currentProject.BIAFronts)
                    {
                        BiaFronts.Add(biaFront);
                    }
                    BiaFront = BiaFronts.FirstOrDefault();
                }

                OnPropertyChanged(nameof(IsProjectCompatibleV7));
            }
        }

        private bool isProjectChosen;
        public bool IsProjectChosen
        {
            get => isProjectChosen;
            set
            {
                isProjectChosen = value;
                OnPropertyChanged(nameof(IsProjectChosen));
            }
        }

        private string _biaFront;
        public string BiaFront
        {
            get => _biaFront;
            set
            {
                _biaFront = value;
                OnPropertyChanged(nameof(BiaFront));
            }
        }

        private ObservableCollection<string> _biaFronts = [];
        public ObservableCollection<string> BiaFronts
        {
            get => _biaFronts;
            set
            {
                _biaFronts = value;
                OnPropertyChanged(nameof(BiaFronts));
            }
        }
        #endregion

        #region Entity
        private EntityInfo entity;
        public EntityInfo Entity
        {
            get => entity;
            set
            {
                if (entity != value)
                {
                    entity = value;
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        private ObservableCollection<EntityInfo> entities;
        public ObservableCollection<EntityInfo> Entities
        {
            get => entities;
            set
            {
                if (entities != value)
                {
                    entities = value;
                    OnPropertyChanged(nameof(Entities));
                }
            }
        }

        private ObservableCollection<string> entityDisplayItems;
        public ObservableCollection<string> EntityDisplayItems
        {
            get => entityDisplayItems;
            set
            {
                if (entityDisplayItems != value)
                {
                    entityDisplayItems = value;
                    OnPropertyChanged(nameof(EntityDisplayItems));
                }
            }
        }

        private bool isEntityParsed = false;
        public bool IsEntityParsed
        {
            get => isEntityParsed;
            set
            {
                if (isEntityParsed != value)
                {
                    isEntityParsed = value;
                    OnPropertyChanged(nameof(IsEntityParsed));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        private string entityDisplayItemSelected;
        public string EntityDisplayItemSelected
        {
            get => entityDisplayItemSelected;
            set
            {
                if (entityDisplayItemSelected != value)
                {
                    entityDisplayItemSelected = value;
                    OnPropertyChanged(nameof(EntityDisplayItemSelected));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        private bool isGenerated = false;
        public bool IsGenerated
        {
            get => isGenerated;
            set
            {
                if (isGenerated != value)
                {
                    isGenerated = value;
                    OnPropertyChanged(nameof(IsGenerated));
                }
            }
        }
        #endregion

        #region Entity Name

        private string entityNamePlural;
        public string EntityNamePlural
        {
            get => entityNamePlural;
            set
            {
                if (entityNamePlural != value)
                {
                    entityNamePlural = value;
                    OnPropertyChanged(nameof(EntityNamePlural));
                }
            }
        }
        #endregion

        #region Domain

        private string domain;
        public string Domain
        {
            get { return domain; }
            set
            {
                if (domain != value)
                {
                    domain = value;
                    OnPropertyChanged(nameof(Domain));
                    OnPropertyChanged(nameof(IsButtonGenerateOptionEnable));
                }
            }
        }

        #endregion

        #region ZipFile
        private List<ZipFeatureType> zipFeatureTypeList;
        public List<ZipFeatureType> ZipFeatureTypeList
        {
            get => zipFeatureTypeList;
            set
            {
                if (zipFeatureTypeList != value)
                {
                    zipFeatureTypeList = value;
                }
            }
        }
        #endregion

        #region Button
        public bool IsButtonGenerateOptionEnable
        {
            get
            {
                return IsEntityParsed
                    && !string.IsNullOrWhiteSpace(EntityNamePlural)
                    && !string.IsNullOrWhiteSpace(entityDisplayItemSelected)
                    && !string.IsNullOrEmpty(Domain);
            }
        }
        #endregion

        #region CheckBox

        public bool IsProjectCompatibleV7 => Version.TryParse(CurrentProject?.FrameworkVersion, out Version version) && version.Major >= 7;

        private bool _useHubClient;

        public bool UseHubClient
        {
            get { return _useHubClient; }
            set
            {
                _useHubClient = value;
                OnPropertyChanged(nameof(UseHubClient));
            }
        }

        #endregion

        #region Commands

        [RelayCommand]
        private void Generate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async () =>
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
                        BaseKeyType = "int" // Options typically use int as key type
                    });

                    UpdateOptionGenerationHistory();
                    return;
                }

                consoleWriter.AddMessageLine("Project is not compatible for option generation.", "Red");
            }));
        }

        [RelayCommand]
        private void DeleteLastGeneration()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(() =>
            {
                if (optionHistory == null || string.IsNullOrEmpty(optionHistoryFileName))
                {
                    consoleWriter.AddMessageLine("No generation history found.", "Yellow");
                    return Task.CompletedTask;
                }

                try
                {
                    // Note: DeleteLastGeneration for options is not fully implemented in GenerateCrudService
                    // For now, just delete the history file
                    if (File.Exists(optionHistoryFileName))
                    {
                        File.Delete(optionHistoryFileName);
                    }
                    optionHistory = null;
                    optionHistoryFileName = null;
                    consoleWriter.AddMessageLine("Last generation history deleted.", "Green");
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
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async () =>
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
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async () =>
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
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async () =>
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

        private async Task ParseEntityFileAsync()
        {
            if (Entity == null || CurrentProject == null)
                return;

            try
            {
                var entityFilePath = Entity.Path;
                var parsedEntity = await Task.Run(() => parserService.ParseClassFile(entityFilePath));

                if (parsedEntity != null)
                {
                    // Extract display items (string properties) from PropertyList
                    var displayItems = new List<string>();
                    foreach (var prop in parsedEntity.PropertyList)
                    {
                        var propType = prop.Type.ToString();
                        var propName = prop.Identifier.Text;

                        if (propType == "string" && !propName.EndsWith("Id"))
                        {
                            displayItems.Add(propName);
                        }
                    }

                    EntityDisplayItems = new ObservableCollection<string>(displayItems);
                    EntityDisplayItemSelected = displayItems.FirstOrDefault();

                    // Auto-fill EntityNamePlural
                    EntityNamePlural = Entity.Name.Pluralize();

                    // Auto-fill Domain from namespace
                    UpdateDomainPreSelection();

                    IsEntityParsed = true;
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error parsing entity file: {ex.Message}", "Red");
                IsEntityParsed = false;
            }
        }

        private void UpdateDomainPreSelection()
        {
            if (Entity == null)
            {
                Domain = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(Domain))
            {
                return;
            }

            var namespaceParts = Entity.Namespace.Split('.').ToList();
            var domainIndex = namespaceParts.IndexOf("Dto");
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
                var historyFolder = Path.Combine(CurrentProject.Folder, ".bia", "History");
                if (!Directory.Exists(historyFolder))
                    return;

                var historyFiles = Directory.GetFiles(historyFolder, "Option_*.json");
                if (historyFiles.Length > 0)
                {
                    optionHistoryFileName = historyFiles.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                    var json = File.ReadAllText(optionHistoryFileName);
                    optionHistory = JsonSerializer.Deserialize<OptionGenerationInfo>(json);
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error loading option generation history: {ex.Message}", "Yellow");
            }
        }

        private void UpdateOptionGenerationHistory()
        {
            if (CurrentProject == null || Entity == null)
                return;

            try
            {
                var historyFolder = Path.Combine(CurrentProject.Folder, ".bia", "History");
                Directory.CreateDirectory(historyFolder);

                optionHistory = new OptionGenerationInfo
                {
                    EntityName = Entity.Name,
                    Domain = Domain,
                    GeneratedDate = DateTime.Now
                };

                optionHistoryFileName = Path.Combine(historyFolder, $"Option_{Entity.Name}_{DateTime.Now:yyyyMMddHHmmss}.json");
                var json = JsonSerializer.Serialize(optionHistory, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(optionHistoryFileName, json);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error saving option generation history: {ex.Message}", "Yellow");
            }
        }

        #endregion
    }

    /// <summary>
    /// Helper class for option generation history (local to ViewModel).
    /// Named to avoid collision with Domain's OptionGeneration.
    /// </summary>
    public class OptionGenerationInfo
    {
        public string EntityName { get; set; }
        public string Domain { get; set; }
        public DateTime GeneratedDate { get; set; }
    }
}
