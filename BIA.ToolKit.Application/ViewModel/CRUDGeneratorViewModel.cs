namespace BIA.ToolKit.Application.ViewModel
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Application.ViewModel.Base;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Application.ViewModel.MicroMvvm;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
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
    using System.Windows.Input;

    public class CRUDGeneratorViewModel : ViewModelBase
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";

        private readonly CSharpParserService service;
        private readonly ZipParserService zipService;
        private readonly GenerateCrudService crudService;
        private readonly CRUDSettings settings;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;

        private CRUDGeneration crudHistory;
        private string crudHistoryFileName;
        private List<FeatureGenerationSettings> backSettingsList = [];
        private List<FeatureGenerationSettings> frontSettingsList = [];

        /// <summary>
        /// Constructor.
        /// </summary>
        public CRUDGeneratorViewModel(IMessenger messenger, CSharpParserService service, ZipParserService zipService, GenerateCrudService crudService, SettingsService settingsService, FileGeneratorService fileGeneratorService, IConsoleWriter consoleWriter)
            : base(messenger)
        {
            this.service = service;
            this.zipService = zipService;
            this.crudService = crudService;
            this.settings = new CRUDSettings(settingsService);
            this.fileGeneratorService = fileGeneratorService;
            this.consoleWriter = consoleWriter;

            OptionItems = [];
            ZipFeatureTypeList = [];
            FeatureNames = [];
            DtoEntities = [];
        }

        public override void Initialize()
        {
            Messenger.Subscribe<ProjectChangedMessage>(OnProjectChanged);
            Messenger.Subscribe<SolutionParsedMessage>(OnSolutionParsed);
        }

        public override void Cleanup()
        {
            Messenger.Unsubscribe<ProjectChangedMessage>(OnProjectChanged);
            Messenger.Unsubscribe<SolutionParsedMessage>(OnSolutionParsed);
        }

        private void OnProjectChanged(ProjectChangedMessage msg)
        {
            if (msg.Project == CurrentProject)
                return;
            if (msg.Project is null)
            {
                ClearAll();
                CurrentProject = null;
                return;
            }
            InitProjectTask(msg.Project);
        }

        private void OnSolutionParsed(SolutionParsedMessage msg)
        {
            InitProjectTask(CurrentProject);
            if (CurrentProject is not null)
                ListDtoFiles();
        }

        public void ClearAll()
        {
            backSettingsList.Clear();
            frontSettingsList.Clear();
            OptionItems?.Clear();
            ZipFeatureTypeList.Clear();
            FeatureNames.Clear();

            DtoEntity = null;
            DtoEntities.Clear();
            IsWebApiSelected = false;
            IsFrontSelected = false;
            FeatureNameSelected = null;
            BiaFronts.Clear();
            BiaFront = null;
            IsTeam = false;
            DisplayHistorical = false;
            UseDomainUrl = false;

            crudHistory = null;
        }

        public void InitProjectTask(Project project)
        {
            ClearAll();

            if (project is not null)
            {
                InitProject(project);
            }
        }

        private void InitProject(Project project)
        {
            try
            {
                SetGenerationSettings(project);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on initializing project: {ex.Message}", "Red");
            }
        }

        private void SetGenerationSettings(Project project)
        {
            string dotnetBiaFolderPath = Path.Combine(project.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(project.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            crudHistoryFileName = Path.Combine(project.Folder, Constants.FolderBia, settings.CrudGenerationHistoryFileName);

            var oldCrudHistoryFilePath = Path.Combine(project.Folder, settings.CrudGenerationHistoryFileName);
            if (File.Exists(oldCrudHistoryFilePath))
            {
                File.Move(oldCrudHistoryFilePath, crudHistoryFileName);
            }

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                UseFileGenerator = true;
                FeatureNames.Add("CRUD");
                FeatureNameSelected = "CRUD";
                crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
            }
            else
            {
                UseFileGenerator = false;

                if (File.Exists(backSettingsFileName))
                {
                    backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));
                    if (project.FrameworkVersion == "3.9.0")
                    {
                        var crudPlanesFeature = backSettingsList.FirstOrDefault(x => x.Feature == "crud-planes");
                        if (crudPlanesFeature != null)
                        {
                            crudPlanesFeature.Feature = "planes";
                        }
                    }
                }

                foreach (var setting in backSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType == FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(featureType, GenerationType.WebApi, setting.ZipName, dotnetBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent, setting.AdaptPaths, setting.FeatureDomain);
                    ZipFeatureTypeList.Add(zipFeatureType);
                }

                foreach (var featureName in ZipFeatureTypeList.Select(x => x.Feature).Distinct())
                {
                    FeatureNames.Add(featureName);
                }

                crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
            }

            CurrentProject = project;
            IsProjectChosen = true;

            crudService.CurrentProject = project;
            crudService.CrudNames = new(backSettingsList, frontSettingsList);

            IsTeam = IsTeam;
        }

        public void SetFrontGenerationSettings(string biaFront)
        {
            frontSettingsList.Clear();
            ZipFeatureTypeList.RemoveAll(x => x.GenerationType == GenerationType.Front);

            string angularBiaFolderPath = Path.Combine(CurrentProject.Folder, biaFront, Constants.FolderBia);
            string frontSettingsFileName = Path.Combine(CurrentProject.Folder, biaFront, settings.GenerationSettingsFileName);

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                return;
            }

            if (File.Exists(frontSettingsFileName))
            {
                frontSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(frontSettingsFileName));
                if (CurrentProject.FrameworkVersion == "3.9.0")
                {
                    var featuresToRemove = frontSettingsList.Where(x => x.Feature == "planes-full-code" || x.Feature == "aircraft-maintenance-companies");
                    frontSettingsList = frontSettingsList.Except(featuresToRemove).ToList();
                }
            }

            foreach (var setting in frontSettingsList)
            {
                var featureType = Enum.Parse<FeatureType>(setting.Type);
                if (featureType == FeatureType.Option)
                    continue;

                var zipFeatureType = new ZipFeatureType(featureType, GenerationType.Front, setting.ZipName, angularBiaFolderPath, setting.Feature, setting.Parents, setting.NeedParent, setting.AdaptPaths, setting.FeatureDomain);
                ZipFeatureTypeList.Add(zipFeatureType);
            }
        }

        public void ParseFrontDomains()
        {
            const string suffix = "-option";
            const string domainsPath = @"src\app\domains";
            List<string> foldersName = [];

            string folderPath = Path.Combine(CurrentProject.Folder, BiaFront, domainsPath);
            if (!Directory.Exists(folderPath))
                return;

            List<string> folders = [.. Directory.GetDirectories(folderPath, $"*{suffix}", SearchOption.AllDirectories)];
            folders.ForEach(f => foldersName.Add(new System.IO.DirectoryInfo(f).Name.Replace(suffix, "")));

            AddOptionItems(foldersName.Select(x => new OptionItem(CommonTools.ConvertKebabToPascalCase(x))));
        }

        public void ListDtoFiles()
        {
            List<EntityInfo> dtoEntities = [];

            string dtoFolder = $"{CurrentProject.CompanyName}.{CurrentProject.Name}.Domain.Dto";
            string dtoFolderPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet, dtoFolder);

            try
            {
                if (Directory.Exists(dtoFolderPath))
                {
                    foreach (var dtoClass in service.CurrentSolutionClasses.Where(x =>
                        x.FilePath.StartsWith(dtoFolderPath, StringComparison.InvariantCultureIgnoreCase)
                        && x.FilePath.EndsWith("Dto.cs")))
                    {
                        dtoEntities.Add(new EntityInfo(dtoClass));
                    }
                }
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine(ex.Message, "Red");
            }

            DtoEntities = new(dtoEntities.OrderBy(x => x.Name));
        }

        public bool ParseDtoFile()
        {
            try
            {
                if (DtoEntity is null)
                    return false;

                List<string> displayItems = [];
                foreach (var property in DtoEntity.Properties.OrderBy(x => x.Name))
                {
                    displayItems.Add(property.Name);
                }

                DtoDisplayItems = displayItems;
                DtoDisplayItemSelected = DtoEntity.Properties.FirstOrDefault(p => p.Type.StartsWith("string", StringComparison.CurrentCultureIgnoreCase))?.Name;

                return true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing Dto File: {ex.Message}", "Red");
            }
            return false;
        }

        public void OnDtoSelected(EntityInfo dto)
        {
            IsDtoParsed = false;
            DtoDisplayItems = null;

            if (CurrentProject is null)
                return;

            IsDtoParsed = ParseDtoFile();
            CRUDNameSingular = GetEntityNameFromDto(DtoEntity?.Name);
            IsTeam = DtoEntity?.IsTeam == true;
            IsVersioned = DtoEntity?.IsVersioned == true;
            IsFixable = DtoEntity?.IsFixable == true;
            IsArchivable = DtoEntity?.IsArchivable == true;
            AncestorTeam = DtoEntity?.AncestorTeamName;
            SelectedBaseKeyType = DtoEntity?.BaseKeyType;
            var isBackSelected = IsWebApiAvailable;
            var isFrontSelected = IsFrontAvailable;

            foreach (var optionItem in OptionItems)
            {
                optionItem.Check = false;
            }
            if (DtoEntity != null)
            {
                foreach (var property in DtoEntity.Properties.Where(p => p.IsOptionDto))
                {
                    var optionType = property.Annotations.FirstOrDefault(a => a.Key == "Type").Value;
                    if (string.IsNullOrEmpty(optionType))
                        continue;

                    var optionItem = OptionItems.FirstOrDefault(oi => oi.OptionName == optionType);
                    if (optionItem is null)
                        continue;

                    optionItem.Check = true;
                }
            }

            bool wasAlreadyGenerated = false;
            if (crudHistory != null)
            {
                string dtoName = GetDtoSelectedPath();
                if (!string.IsNullOrEmpty(dtoName))
                {
                    CRUDGenerationHistory history = crudHistory.CRUDGenerationHistory.FirstOrDefault(h => h.Mapping.Dto == dtoName);

                    if (history != null)
                    {
                        CRUDNameSingular = history.EntityNameSingular;
                        CRUDNamePlural = history.EntityNamePlural;
                        DtoDisplayItemSelected = history.DisplayItem;
                        FeatureNameSelected = history.Feature;
                        HasParent = history.HasParent;
                        ParentName = history.ParentName;
                        ParentNamePlural = history.ParentNamePlural;
                        Domain = history.Domain;
                        BiaFront = history.BiaFront;
                        IsTeam = history.IsTeam;
                        TeamTypeId = history.TeamTypeId;
                        TeamRoleId = history.TeamRoleId;
                        UseHubClient = history.UseHubClient;
                        HasCustomRepository = history.UseCustomRepository;
                        HasFormReadOnlyMode = history.HasFormReadOnlyMode;
                        UseImport = history.UseImport;
                        IsFixable = history.IsFixable;
                        HasFixableParent = history.HasFixableParent;
                        UseAdvancedFilter = history.HasAdvancedFilter;
                        AncestorTeam = history.AncestorTeam;
                        SelectedFormReadOnlyMode = history.FormReadOnlyMode;
                        IsVersioned = history.IsVersioned;
                        IsArchivable = history.IsArchivable;
                        SelectedBaseKeyType = history.EntityBaseKeyType;
                        DisplayHistorical = history.DisplayHistorical;
                        UseDomainUrl = history.UseDomainUrl;
                        if (history.OptionItems != null)
                        {
                            foreach (var option in OptionItems)
                            {
                                option.Check = false;
                            }

                            history.OptionItems.ForEach(o =>
                            {
                                OptionItem item = OptionItems.FirstOrDefault(x => x.OptionName == o);
                                if (item != null)
                                    item.Check = true;
                            });
                        }

                        isBackSelected = history.Generation.Any(g => g.GenerationType == GenerationType.WebApi.ToString());
                        isFrontSelected = history.Generation.Any(g => g.GenerationType == GenerationType.Front.ToString());
                        wasAlreadyGenerated = true;
                    }
                }
            }

            IsDtoGenerated = wasAlreadyGenerated;
            IsWebApiSelected = isBackSelected;
            IsFrontSelected = isFrontSelected;
        }

        public void UpdateCrudGenerationHistory()
        {
            try
            {
                crudHistory ??= new();

                CRUDGenerationHistory history = new()
                {
                    Date = DateTime.Now,
                    EntityNameSingular = CRUDNameSingular,
                    EntityNamePlural = CRUDNamePlural,
                    DisplayItem = DtoDisplayItemSelected,
                    OptionItems = OptionItems?.Where(o => o.Check).Select(o => o.OptionName).ToList(),
                    Feature = FeatureNameSelected,
                    HasParent = HasParent,
                    ParentName = ParentName,
                    ParentNamePlural = ParentNamePlural,
                    Domain = Domain,
                    BiaFront = BiaFront,
                    IsTeam = IsTeam,
                    TeamTypeId = TeamTypeId,
                    TeamRoleId = TeamRoleId,
                    UseHubClient = UseHubClient,
                    UseCustomRepository = HasCustomRepository,
                    HasFormReadOnlyMode = HasFormReadOnlyMode,
                    UseImport = UseImport,
                    IsFixable = IsFixable,
                    HasFixableParent = HasFixableParent,
                    HasAdvancedFilter = UseAdvancedFilter,
                    AncestorTeam = AncestorTeam,
                    FormReadOnlyMode = SelectedFormReadOnlyMode,
                    IsVersioned = IsVersioned,
                    IsArchivable = IsArchivable,
                    EntityBaseKeyType = SelectedBaseKeyType,
                    DisplayHistorical = DisplayHistorical,
                    UseDomainUrl = UseDomainUrl,
                    Mapping = new()
                    {
                        Dto = GetDtoSelectedPath(),
                        Type = DOTNET_TYPE,
                    }
                };

                if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
                {
                    if (IsWebApiSelected)
                    {
                        history.Generation.Add(new Generation
                        {
                            FeatureType = FeatureType.CRUD.ToString(),
                            GenerationType = GenerationType.WebApi.ToString(),
                            Type = DOTNET_TYPE,
                            Folder = Constants.FolderDotNet
                        });
                    }
                    if (IsFrontSelected)
                    {
                        history.Generation.Add(new Generation
                        {
                            FeatureType = FeatureType.CRUD.ToString(),
                            GenerationType = GenerationType.Front.ToString(),
                            Type = ANGULAR_TYPE,
                            Folder = BiaFront
                        });
                    }
                }
                else
                {
                    ZipFeatureTypeList.Where(f => f.FeatureDataList != null).ToList().ForEach(feature =>
                    {
                        if (feature.IsChecked)
                        {
                            Generation crudGeneration = new()
                            {
                                GenerationType = feature.GenerationType.ToString(),
                                FeatureType = feature.FeatureType.ToString(),
                                Template = feature.ZipName
                            };
                            if (feature.GenerationType == GenerationType.WebApi)
                            {
                                crudGeneration.Type = DOTNET_TYPE;
                                crudGeneration.Folder = Constants.FolderDotNet;
                            }
                            else if (feature.GenerationType == GenerationType.Front)
                            {
                                crudGeneration.Type = ANGULAR_TYPE;
                                crudGeneration.Folder = BiaFront;
                            }
                            history.Generation.Add(crudGeneration);
                        }
                    });
                }

                CRUDGenerationHistory genFound = crudHistory.CRUDGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    crudHistory.CRUDGenerationHistory.Remove(genFound);
                }

                crudHistory.CRUDGenerationHistory.Add(history);

                CommonTools.SerializeToJsonFile<CRUDGeneration>(crudHistory, crudHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        public void DeleteLastGenerationHistory(CRUDGenerationHistory history)
        {
            crudHistory?.CRUDGenerationHistory?.Remove(history);

            foreach (CRUDGenerationHistory optionHistory in crudHistory?.CRUDGenerationHistory)
            {
                optionHistory.OptionItems.Remove(history.EntityNameSingular);
            }

            CommonTools.SerializeToJsonFile<CRUDGeneration>(crudHistory, crudHistoryFileName);
        }

        public CRUDGenerationHistory GetCurrentDtoHistory()
        {
            string dtoName = GetDtoSelectedPath();
            if (string.IsNullOrEmpty(dtoName))
                return null;
            return crudHistory?.CRUDGenerationHistory?.FirstOrDefault(h => h.Mapping.Dto == dtoName);
        }

        public List<CRUDGenerationHistory> GetHistoriesUsedByOptions()
        {
            return crudHistory?.CRUDGenerationHistory?.Where(h => h.OptionItems.Contains(CRUDNameSingular)).ToList();
        }

        public async Task GenerateCRUDAsync()
        {
            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                await fileGeneratorService.GenerateCRUDAsync(new FileGeneratorCrudContext
                {
                    CompanyName = CurrentProject.CompanyName,
                    ProjectName = CurrentProject.Name,
                    DomainName = Domain,
                    EntityName = CRUDNameSingular,
                    EntityNamePlural = CRUDNamePlural,
                    BaseKeyType = SelectedBaseKeyType,
                    IsTeam = IsTeam,
                    Properties = [.. DtoEntity.Properties],
                    OptionItems = [.. OptionItems.Where(x => x.Check).Select(x => x.OptionName)],
                    HasParent = HasParent,
                    ParentName = ParentName,
                    ParentNamePlural = ParentNamePlural,
                    AncestorTeamName = AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrWhiteSpace(AncestorTeam),
                    AngularFront = BiaFront,
                    GenerateBack = IsWebApiSelected,
                    GenerateFront = IsFrontSelected,
                    DisplayItemName = DtoDisplayItemSelected,
                    TeamTypeId = TeamTypeId,
                    TeamRoleId = TeamRoleId,
                    UseHubForClient = UseHubClient,
                    HasCustomRepository = HasCustomRepository,
                    HasReadOnlyMode = HasFormReadOnlyMode,
                    CanImport = UseImport,
                    IsFixable = IsFixable,
                    HasFixableParent = HasFixableParent,
                    HasAdvancedFilter = UseAdvancedFilter,
                    FormReadOnlyMode = SelectedFormReadOnlyMode,
                    IsVersioned = IsVersioned,
                    IsArchivable = IsArchivable,
                    DisplayHistorical = DisplayHistorical,
                    UseDomainUrl = UseDomainUrl,
                });

                UpdateCrudGenerationHistory();
                return;
            }

            if (!zipService.ParseZips(ZipFeatureTypeList, CurrentProject, BiaFront, settings))
                return;

            var crudParent = new CrudParent
            {
                Exists = HasParent,
                Name = ParentName,
                NamePlural = ParentNamePlural,
                Domain = Domain
            };

            crudService.CrudNames.InitRenameValues(CRUDNameSingular, CRUDNamePlural);

            List<string> optionsItems = OptionItems.Any() ? OptionItems.Where(o => o.Check).Select(o => o.OptionName).ToList() : null;
            IsDtoGenerated = crudService.GenerateFiles(DtoEntity, ZipFeatureTypeList, DtoDisplayItemSelected, optionsItems, crudParent, FeatureNameSelected, Domain, BiaFront);

            UpdateCrudGenerationHistory();

            consoleWriter.AddMessageLine($"End of '{CRUDNameSingular}' generation.", "Blue");
        }

        public async Task DeleteLastGenerationAsync(CRUDGenerationHistory history)
        {
            if (history == null)
            {
                consoleWriter.AddMessageLine($"No previous '{CRUDNameSingular}' generation found.", "Orange");
                return;
            }

            List<CRUDGenerationHistory> historyOptions = GetHistoriesUsedByOptions();

            crudService.DeleteLastGeneration(ZipFeatureTypeList, CurrentProject, history, FeatureNameSelected,
                new CrudParent { Exists = history.HasParent, Domain = history.Domain, Name = history.ParentName, NamePlural = history.ParentNamePlural },
                historyOptions);

            DeleteLastGenerationHistory(history);

            consoleWriter.AddMessageLine($"End of '{CRUDNameSingular}' suppression.", "Purple");
        }

        public static string GetEntityNameFromDto(string dtoFileName)
        {
            if (string.IsNullOrWhiteSpace(dtoFileName))
                return null;

            var fileName = Path.GetFileNameWithoutExtension(dtoFileName);
            if (!string.IsNullOrWhiteSpace(fileName) && fileName.ToLower().EndsWith("dto"))
            {
                return fileName[..^3];
            }

            return fileName;
        }

        public string GetDtoSelectedPath()
        {
            if (DtoEntity is null)
                return null;

            string dotNetPath = Path.Combine(CurrentProject.Folder, Constants.FolderDotNet);
            return DtoEntity.Path.Replace(dotNetPath, "").TrimStart(Path.DirectorySeparatorChar);
        }

        public async Task DeleteAnnotationsAsync(List<string> folders)
        {
            await crudService.DeleteBIAToolkitAnnotations(folders);
            consoleWriter.AddMessageLine($"End of annotations suppression.", "Purple");
        }

        public async Task DeleteAnnotationsAsync()
        {
            var folders = new List<string>
            {
                Path.Combine(CurrentProject.Folder, Constants.FolderDotNet),
                Path.Combine(CurrentProject.Folder, BiaFront, "src", "app")
            };
            await DeleteAnnotationsAsync(folders);
        }

        #region CurrentProject
        private Project currentProject;
        public Project CurrentProject
        {
            get => currentProject;
            set
            {
                currentProject = value;

                BiaFronts.Clear();
                if(currentProject != null)
                {
                    foreach(var biaFront in currentProject.BIAFronts)
                    {
                        BiaFronts.Add(biaFront);
                    }
                    BiaFront = BiaFronts.FirstOrDefault();
                }

                RaisePropertyChanged(nameof(IsProjectCompatibleV6));
                RaisePropertyChanged(nameof(IsFrontAvailable));
            }
        }

        private bool isProjectChosen;
        public bool IsProjectChosen
        {
            get => isProjectChosen;
            set
            {
                isProjectChosen = value;
                RaisePropertyChanged(nameof(IsProjectChosen));
            }
        }

        private bool _useFileGenerator;
        public bool UseFileGenerator
        {
            get => _useFileGenerator;
            set
            {
                _useFileGenerator = value;
                RaisePropertyChanged(nameof(UseFileGenerator));
            }
        }
        #endregion

        #region Commands
        public ICommand GenerateCrudCommand => new RelayCommand(_ => Messenger.Send(new ExecuteWithWaiterMessage { Task = GenerateCRUDAsync }));
        public ICommand DeleteLastGenerationCommand => new RelayCommand(_ =>
        {
            try
            {
                var history = GetCurrentDtoHistory();
                Messenger.Send(new ExecuteWithWaiterMessage
                {
                    Task = async () =>
                    {
                        await DeleteLastGenerationAsync(history);
                        IsDtoGenerated = false;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error on deleting last '{CRUDNameSingular}' generation: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        });
        #endregion

        #region Dto
        private EntityInfo dtoEntity;
        public EntityInfo DtoEntity
        {
            get => dtoEntity;
            set
            {
                if (dtoEntity != value)
                {
                    dtoEntity = value;
                    RaisePropertyChanged(nameof(DtoEntity));
                    UpdateParentPreSelection();
                    UpdateDomainPreSelection();
                    if (value is not null)
                    {
                        OnDtoSelected(value);
                    }
                }
            }
        }

        private ObservableCollection<EntityInfo> dtoEntities;
        public ObservableCollection<EntityInfo> DtoEntities
        {
            get => dtoEntities;
            set
            {
                if (dtoEntities != value)
                {
                    dtoEntities = value;
                    RaisePropertyChanged(nameof(DtoEntities));
                }
            }
        }

        private List<string> dtoDisplayItems;
        public List<string> DtoDisplayItems
        {
            get => dtoDisplayItems;
            set
            {
                if (dtoDisplayItems != value)
                {
                    dtoDisplayItems = value;
                    RaisePropertyChanged(nameof(DtoDisplayItems));
                }
            }
        }

        private bool isDtoParsed = false;
        public bool IsDtoParsed
        {
            get => isDtoParsed;
            set
            {
                if (isDtoParsed != value)
                {
                    isDtoParsed = value;
                    RaisePropertyChanged(nameof(IsDtoParsed));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                    RaisePropertyChanged(nameof(IsOptionItemEnable));
                }
            }
        }

        private string dtoDisplayItemSelected;
        public string DtoDisplayItemSelected
        {
            get => dtoDisplayItemSelected;
            set
            {
                if (dtoDisplayItemSelected != value)
                {
                    dtoDisplayItemSelected = value;
                    RaisePropertyChanged(nameof(DtoDisplayItemSelected));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private ObservableCollection<OptionItem> optionItems = [];
        public ObservableCollection<OptionItem> OptionItems
        {
            get => optionItems;
            set
            {
                if (optionItems != value)
                {
                    optionItems = value;
                    RaisePropertyChanged(nameof(OptionItems));
                }
            }
        }

        public static IEnumerable<string> BaseKeyTypeItems => Constants.PrimitiveTypes;
        private string selectedBaseKeyType;

        public string SelectedBaseKeyType
        {
            get { return selectedBaseKeyType; }
            set
            {
                selectedBaseKeyType = value;
                RaisePropertyChanged(nameof(SelectedBaseKeyType));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        public string SelectedOptionItems => string.Join(", ", OptionItems.Where(x => x.Check).Select(x => x.OptionName));

        private bool isDtoGenerated = false;
        public bool IsDtoGenerated
        {
            get => isDtoGenerated;
            set
            {
                if (isDtoGenerated != value)
                {
                    isDtoGenerated = value;
                    RaisePropertyChanged(nameof(IsDtoGenerated));
                }
            }
        }

        public void AddOptionItems(IEnumerable<OptionItem> optionItems)
        {
            OptionItems.Clear();
            foreach (var optionItem in optionItems.OrderBy(x => x.OptionName))
            {
                OptionItems.Add(optionItem);
            }
            foreach(var optionItem in OptionItems)
            {
                optionItem.PropertyChanged += OptionItem_PropertyChanged;
            }
        }

        private void OptionItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(SelectedOptionItems));
        }
        #endregion

        #region Feature
        private ObservableCollection<string> featureNames;
        public ObservableCollection<string> FeatureNames
        {
            get => featureNames;
            set
            {
                if (featureNames != value)
                {
                    featureNames = value;
                    RaisePropertyChanged(nameof(FeatureNames));
                }
            }
        }

        private string featureNameSelected;
        public string FeatureNameSelected
        {
            get => featureNameSelected;
            set
            {
                if (featureNameSelected != value)
                {
                    featureNameSelected = value;
                    RaisePropertyChanged(nameof(FeatureNameSelected));
                    RaisePropertyChanged(nameof(IsOptionItemEnable));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                    RaisePropertyChanged(nameof(IsWebApiAvailable));
                    RaisePropertyChanged(nameof(IsFrontAvailable));
                    IsWebApiSelected = IsWebApiAvailable;
                    IsFrontSelected = IsFrontAvailable;
                    UpdateParentPreSelection();
                    UpdateDomainPreSelection();

                    if (!UseFileGenerator)
                    {
                        IsTeam = IsTeam || featureNameSelected == "Team";
                    }
                }
            }
        }

        private void UpdateFeatureSelection()
        {
            ZipFeatureTypeList.ForEach(x => x.IsChecked = false);
            if (string.IsNullOrEmpty(featureNameSelected))
                return;

            foreach (var zipFeatureType in ZipFeatureTypeList.Where(x => x.Feature == FeatureNameSelected))
            {
                if (zipFeatureType.GenerationType == GenerationType.WebApi && isWebApiSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
                }
                if (zipFeatureType.GenerationType == GenerationType.Front && isFrontSelected)
                {
                    zipFeatureType.IsChecked = true;
                    continue;
                }
            }
        }

        #endregion

        #region CRUD Name
        private string entityNameSingular;
        public string CRUDNameSingular
        {
            get => entityNameSingular;
            set
            {
                if (entityNameSingular != value)
                {
                    entityNameSingular = value;
                    RaisePropertyChanged(nameof(CRUDNameSingular));
                    CRUDNamePlural = value.Pluralize();
                }
            }
        }

        private string entityNamePlural;
        public string CRUDNamePlural
        {
            get => entityNamePlural;
            set
            {
                if (entityNamePlural != value)
                {
                    entityNamePlural = value;
                    RaisePropertyChanged(nameof(CRUDNamePlural));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }
        #endregion

        #region Parent
        public bool IsCheckboxParentEnable
        {
            get
            {
                if(UseFileGenerator)
                {
                    return true;
                }

                // CRUD feature always disable
                if (!string.IsNullOrEmpty(FeatureNameSelected) && FeatureNameSelected.Equals("CRUD"))
                    return false;

                var selectedFeaturesWithParent = ZipFeatureTypeList.Where(x => x.Feature == FeatureNameSelected && x.Parents.Any(y => y.IsPrincipal));
                // Let checkbox editable if parent is not needed
                if (selectedFeaturesWithParent.Any() && selectedFeaturesWithParent.All(x => !x.NeedParent))
                    return true;

                return false;
            }
        }

        private bool hasParent;
        public bool HasParent
        {
            get { return hasParent; }
            set
            {
                if (hasParent != value)
                {
                    hasParent = value;
                    RaisePropertyChanged(nameof(HasParent));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));

                    if (value == false)
                    {
                        ParentName = null;
                        ParentNamePlural = null;
                    }
                    else
                    {
                        UpdateParentPreSelection();
                    }
                }
            }
        }

        private string domain;
        public string Domain
        {
            get { return domain; }
            set
            {
                if (domain != value)
                {
                    domain = value;
                    RaisePropertyChanged(nameof(Domain));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private string parentName;
        public string ParentName
        {
            get { return parentName; }
            set
            {
                if (parentName != value)
                {
                    parentName = value;
                    RaisePropertyChanged(nameof(ParentName));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                    ParentNamePlural = value.Pluralize();
                }
            }
        }

        private string parentNamePlural;
        public string ParentNamePlural
        {
            get { return parentNamePlural; }
            set
            {
                if (parentNamePlural != value)
                {
                    parentNamePlural = value;
                    RaisePropertyChanged(nameof(ParentNamePlural));
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private void UpdateParentPreSelection()
        {
            if(!string.IsNullOrWhiteSpace(ParentName))
            {
                return;
            }

            var selectedFeaturesWithParent = ZipFeatureTypeList.Where(x => x.Feature == FeatureNameSelected && x.Parents.Any(y => y.IsPrincipal));
            if ((UseFileGenerator || selectedFeaturesWithParent.Any()) && DtoEntity != null)
            {
                var propertiesWithParent = DtoEntity.Properties.Where(x => x.Annotations != null && x.Annotations.Any(y => y.Key == "IsParent"));
                var parentPropertyName = propertiesWithParent.FirstOrDefault(x => x.Name.EndsWith("Id"))?.Name;
                if (!string.IsNullOrEmpty(parentPropertyName))
                {
                    var parentName = parentPropertyName.Replace("Id", string.Empty);
                    ParentName = parentName;
                    HasParent = true;
                }
                if(!UseFileGenerator && !HasParent)
                {
                    HasParent = selectedFeaturesWithParent.Any(x => x.NeedParent);
                }
            }
            else
            {
                HasParent = selectedFeaturesWithParent.Any(x => x.NeedParent);
            }

            RaisePropertyChanged(nameof(IsCheckboxParentEnable));
        }

        private void UpdateDomainPreSelection()
        {
            if (DtoEntity == null)
            {
                Domain = null;
                return;
            }

            if (!string.IsNullOrWhiteSpace(Domain))
            {
                return;
            }

            var namespaceParts = DtoEntity.Namespace.Split('.').ToList();
            var domainIndex = namespaceParts.IndexOf("Dto");
            if (domainIndex != -1)
            {
                Domain = namespaceParts[domainIndex + 1];
            }
        }

        #endregion

        #region CheckBox
        private readonly bool isSelectionChange = false;
        public bool IsSelectionChange
        {
            get => isSelectionChange;
            set
            {
                if (isSelectionChange != value)
                {
                    RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                }
            }
        }

        private bool isWebApiSelected;
        public bool IsWebApiSelected
        {
            get => isWebApiSelected;
            set
            {
                if (isWebApiSelected != value)
                {
                    isWebApiSelected = value;
                    RaisePropertyChanged(nameof(IsWebApiSelected));
                }
                UpdateFeatureSelection();
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        private bool isFrontSelected;
        public bool IsFrontSelected
        {
            get => isFrontSelected;
            set
            {
                if (isFrontSelected != value)
                {
                    isFrontSelected = value;
                    RaisePropertyChanged(nameof(IsFrontSelected));
                    if(value == false)
                    {
                        BiaFront = null;
                    }
                }
                UpdateFeatureSelection();
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        private string _biaFront;
        public string BiaFront
        {
            get => _biaFront;
            set
            {
                _biaFront = value;
                RaisePropertyChanged(nameof(BiaFront));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
                if (!string.IsNullOrEmpty(value) && CurrentProject != null)
                {
                    SetFrontGenerationSettings(value);
                    ParseFrontDomains();
                }
            }
        }

        private ObservableCollection<string> _biaFronts = [];
        public ObservableCollection<string> BiaFronts
        {
            get => _biaFronts;
            set
            {
                _biaFronts = value;
                RaisePropertyChanged(nameof(BiaFronts));
            }
        }

        public bool IsWebApiAvailable => UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.WebApi));
        public bool IsFrontAvailable => (UseFileGenerator || (!string.IsNullOrEmpty(FeatureNameSelected) && ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.GenerationType == GenerationType.Front))) && (CurrentProject != null && CurrentProject.BIAFronts.Count > 0);

        private bool _isTeam;

        public bool IsTeam
        {
            get => _isTeam;
            set
            {
                _isTeam = value;
                RaisePropertyChanged(nameof(IsTeam));
                RaisePropertyChanged(nameof(IsCheckBoxIsTeamEnable));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        public bool IsCheckBoxIsTeamEnable
        {
            get
            {
                if (UseFileGenerator)
                    return true;

                if (!string.IsNullOrEmpty(FeatureNameSelected) && FeatureNameSelected.Equals("Team"))
                    return false;

                return IsDtoParsed;
            }
        }

        private bool _useHubClient;

        public bool UseHubClient
        {
            get { return _useHubClient; }
            set 
            { 
                _useHubClient = value; 
                RaisePropertyChanged(nameof(UseHubClient));
            }
        }

        private bool _hasCustomRepository;

        public bool HasCustomRepository
        {
            get { return _hasCustomRepository; }
            set
            {
                _hasCustomRepository = value;
                RaisePropertyChanged(nameof(HasCustomRepository));
            }
        }

        private bool _hasFormReadOnlyMode;

        public bool HasFormReadOnlyMode
        {
            get { return _hasFormReadOnlyMode; }
            set
            {
                _hasFormReadOnlyMode = value;
                RaisePropertyChanged(nameof(HasFormReadOnlyMode));
                SelectedFormReadOnlyMode = value ? FormReadOnlyModes.First() : string.Empty;
            }
        }

        private bool _useImport;

        public bool UseImport
        {
            get { return _useImport; }
            set
            {
                _useImport = value;
                RaisePropertyChanged(nameof(UseImport));
            }
        }

        private bool _isFixable;

        public bool IsFixable
        {
            get { return _isFixable; }
            set
            {
                _isFixable = value;
                RaisePropertyChanged(nameof(IsFixable));
            }
        }

        private bool _hasFixableParent;

        public bool HasFixableParent
        {
            get { return _hasFixableParent; }
            set
            {
                _hasFixableParent = value;
                RaisePropertyChanged(nameof(HasFixableParent));
            }
        }

        private bool _isVersioned;

        public bool IsVersioned
        {
            get { return _isVersioned; }
            set
            {
                _isVersioned = value;
                RaisePropertyChanged(nameof(IsVersioned));
            }
        }

        private bool _isArchivable;

        public bool IsArchivable
        {
            get { return _isArchivable; }
            set
            {
                _isArchivable = value;
                RaisePropertyChanged(nameof(IsArchivable));
            }
        }



        private bool _useAdvancedFilter;

        public bool UseAdvancedFilter
        {
            get { return _useAdvancedFilter; }
            set
            {
                _useAdvancedFilter = value;
                RaisePropertyChanged(nameof(UseAdvancedFilter));
            }
        }

        public bool IsProjectCompatibleV6 => Version.TryParse(CurrentProject?.FrameworkVersion, out var version) && version.Major >= 6;

        private bool displayHistorical;
        public bool DisplayHistorical
        {
            get { return displayHistorical; }
            set
            {
                displayHistorical = value;
                RaisePropertyChanged(nameof(DisplayHistorical));
            }
        }

        private bool useDomainUrl;
        public bool UseDomainUrl
        {
            get { return useDomainUrl; }
            set
            {
                useDomainUrl = value;
                RaisePropertyChanged(nameof(UseDomainUrl));
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
        public bool IsOptionItemEnable
        {
            get
            {
                return isDtoParsed && UseFileGenerator || (!string.IsNullOrEmpty(featureNameSelected) && !ZipFeatureTypeList.Any(x => x.Feature == featureNameSelected && x.FeatureType == FeatureType.Option));
            }
        }

        public bool IsButtonGenerateCrudEnable
        {
            get
            {
                return IsDtoParsed
                    && !string.IsNullOrWhiteSpace(CRUDNameSingular)
                    && !string.IsNullOrWhiteSpace(CRUDNamePlural)
                    && !string.IsNullOrEmpty(Domain)
                    && (!string.IsNullOrWhiteSpace(dtoDisplayItemSelected) || ZipFeatureTypeList.Any(x => x.Feature == FeatureNameSelected && x.FeatureType == FeatureType.Option))
                    && ((IsWebApiSelected && !IsFrontSelected) || (IsWebApiSelected && IsFrontSelected && !string.IsNullOrWhiteSpace(BiaFront)) || (!IsWebApiSelected && IsFrontSelected && !string.IsNullOrWhiteSpace(BiaFront)))
                    && !string.IsNullOrEmpty(featureNameSelected)
                    && (!HasParent || (HasParent && !string.IsNullOrEmpty(ParentName) && !string.IsNullOrEmpty(parentNamePlural)))
                    && (!IsTeam || (IsTeam && !UseFileGenerator) || (UseFileGenerator && IsTeam && TeamRoleId > 0 && TeamTypeId > 0))
                    && !string.IsNullOrWhiteSpace(SelectedBaseKeyType);
            }
        }
        #endregion

        #region Team
        private string _ancestorTeam;

        public string AncestorTeam
        {
            get { return _ancestorTeam; }
            set 
            { 
                _ancestorTeam = value;
                RaisePropertyChanged(nameof(AncestorTeam));
            }
        }

        private int _teamTypeId;

        public int TeamTypeId
        {
            get { return _teamTypeId; }
            set 
            { 
                _teamTypeId = value; 
                RaisePropertyChanged(nameof(TeamTypeId));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        private int _teamRoleId;

        public int TeamRoleId
        {
            get { return _teamRoleId; }
            set
            {
                _teamRoleId = value;
                RaisePropertyChanged(nameof(TeamRoleId));
                RaisePropertyChanged(nameof(IsButtonGenerateCrudEnable));
            }
        }

        #endregion

        #region FormReadOnly
        private List<string> _formReadOnlyModes;

        public List<string> FormReadOnlyModes
        {
            get
            {
                _formReadOnlyModes ??= new List<string>(Enum.GetNames<FormReadOnlyMode>());
                return _formReadOnlyModes;
            }
        }

        private string _selectedFormReadOnlyMode;

        public string SelectedFormReadOnlyMode
        {
            get { return _selectedFormReadOnlyMode; }
            set 
            { 
                _selectedFormReadOnlyMode = value; 
                RaisePropertyChanged(nameof(SelectedFormReadOnlyMode));
            }
        }
        #endregion
    }

    public class OptionItem : ObservableObject
    {
        private bool check;
        public bool Check
        {
            get => check;
            set
            {
                check = value;
                RaisePropertyChanged(nameof(Check));
            }
        }

        public string OptionName { get; set; }

        public OptionItem(string name, bool check = false)
        {
            this.Check = check;
            this.OptionName = name;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public class ZipFeatureType(FeatureType type, GenerationType generation, string zipName, string zipPath, string feature, List<FeatureParent> parents, bool needParent, List<FeatureAdaptPath> adaptPaths, string domain)
    {
        /// <summary>
        /// Is to generate?
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// The Feature type.
        /// </summary>
        public FeatureType FeatureType { get; } = type;

        /// <summary>
        /// The Generation type.
        /// </summary>
        public GenerationType GenerationType { get; } = generation;

        /// <summary>
        /// Angular zip file name.
        /// </summary>
        public string ZipName { get; } = zipName;

        /// <summary>
        /// Angular zip file path.
        /// </summary>
        public string ZipPath { get; } = zipPath;

        /// <summary>
        /// Name of the feature associated to the ZIP
        /// </summary>
        public string Feature { get; } = feature;

        public List<FeatureData> FeatureDataList { get; set; }

        /// <summary>
        /// Parents of the feature
        /// </summary>
        public List<FeatureParent> Parents { get; set; } = parents;

        /// <summary>
        /// Indicates if the feature needs a parent
        /// </summary>
        public bool NeedParent { get; set; } = needParent;

        /// <summary>
        /// The adapt paths to apply
        /// </summary>
        public List<FeatureAdaptPath> AdaptPaths { get; set; } = adaptPaths;

        /// <summary>
        /// The feature domain
        /// </summary>
        public string Domain { get; set; } = domain;
    }

    public class WebApiNamespace(WebApiFileType fileType, string crudNamespace)
    {
        public WebApiFileType FileType { get; } = fileType;

        public string CrudNamespace { get; } = crudNamespace;

        public string CrudNamespaceGenerated { get; set; }
    }

    public class CrudParent
    {
        public bool Exists { get; set; }
        public string Name { get; set; }
        public string NamePlural { get; set; }
        public string Domain { get; set; }
    }
}