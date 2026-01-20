namespace BIA.ToolKit.Application.Services.CRUD
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Settings;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;

    /// <summary>
    /// Service implementation for CRUD generation operations.
    /// Encapsulates business logic extracted from CRUDGeneratorViewModel.
    /// </summary>
    public class CRUDGenerationService : ICRUDGenerationService
    {
        private readonly CSharpParserService parserService;
        private readonly ZipParserService zipService;
        private readonly GenerateCrudService crudService;
        private readonly CRUDSettings settings;
        private readonly FileGeneratorService fileGeneratorService;
        private readonly IConsoleWriter consoleWriter;
        private readonly ITextParsingService textParsingService;

        private readonly List<FeatureGenerationSettings> backSettingsList = [];
        private List<FeatureGenerationSettings> frontSettingsList = [];
        private List<ZipFeatureType> zipFeatureTypeList = [];
        private CRUDGeneration crudHistory;
        private string crudHistoryFileName;

        public Project CurrentProject { get; set; }

        public CRUDGenerationService(
            CSharpParserService parserService,
            ZipParserService zipService,
            GenerateCrudService crudService,
            SettingsService settingsService,
            FileGeneratorService fileGeneratorService,
            IConsoleWriter consoleWriter,
            ITextParsingService textParsingService)
        {
            this.parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            this.zipService = zipService ?? throw new ArgumentNullException(nameof(zipService));
            this.crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            this.settings = new CRUDSettings(settingsService ?? throw new ArgumentNullException(nameof(settingsService)));
            this.fileGeneratorService = fileGeneratorService ?? throw new ArgumentNullException(nameof(fileGeneratorService));
            this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
            this.textParsingService = textParsingService ?? new TextParsingService();
        }

        /// <inheritdoc/>
        public Task<CRUDInitializationResult> InitializeAsync(Project project)
        {
            CurrentProject = project;
            zipFeatureTypeList = [];

            var result = new CRUDInitializationResult();

            // Get files/folders name
            string dotnetBiaFolderPath = Path.Combine(project.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(project.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            crudHistoryFileName = Path.Combine(project.Folder, Constants.FolderBia, settings.CrudGenerationHistoryFileName);

            // Handle old path of CRUD history file
            var oldCrudHistoryFilePath = Path.Combine(project.Folder, settings.CrudGenerationHistoryFileName);
            if (File.Exists(oldCrudHistoryFilePath))
            {
                File.Move(oldCrudHistoryFilePath, crudHistoryFileName);
            }

            // Load generation history
            crudHistory = CommonTools.DeserializeJsonFile<CRUDGeneration>(crudHistoryFileName);
            result.History = crudHistory;

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                result.UseFileGenerator = true;
                result.FeatureNames.Add("CRUD");
                result.ZipFeatureTypeList = zipFeatureTypeList;
                return Task.FromResult(result);
            }

            // Load BIA settings
            if (File.Exists(backSettingsFileName))
            {
                backSettingsList.Clear();
                backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));

                // Handle specific version adjustments
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

                var zipFeatureType = new ZipFeatureType(
                    featureType,
                    GenerationType.WebApi,
                    setting.ZipName,
                    dotnetBiaFolderPath,
                    setting.Feature,
                    setting.Parents,
                    setting.NeedParent,
                    setting.AdaptPaths,
                    setting.FeatureDomain);

                zipFeatureTypeList.Add(zipFeatureType);
            }

            foreach (var featureName in zipFeatureTypeList.Select(x => x.Feature).Distinct())
            {
                result.FeatureNames.Add(featureName);
            }

            result.BackSettings = [.. backSettingsList];
            result.FrontSettings = [.. frontSettingsList];
            result.ZipFeatureTypeList = zipFeatureTypeList;

            crudService.CurrentProject = project;
            crudService.CrudNames = new(backSettingsList, frontSettingsList);

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public IEnumerable<EntityInfo> ListDtoFiles(Project project)
        {
            var dtoEntities = new List<EntityInfo>();

            string dtoFolder = $"{project.CompanyName}.{project.Name}.Domain.Dto";
            string dtoFolderPath = Path.Combine(project.Folder, Constants.FolderDotNet, dtoFolder);

            try
            {
                if (Directory.Exists(dtoFolderPath))
                {
                    foreach (var dtoClass in parserService.CurrentSolutionClasses.Where(x =>
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

            return dtoEntities.OrderBy(x => x.Name);
        }

        /// <inheritdoc/>
        public CRUDDtoParseResult ParseDtoFile(EntityInfo dtoEntity)
        {
            var result = new CRUDDtoParseResult();

            try
            {
                if (dtoEntity is null)
                    return result;

                foreach (var property in dtoEntity.Properties.OrderBy(x => x.Name))
                {
                    result.DisplayItems.Add(property.Name);
                }

                result.DefaultDisplayItem = dtoEntity.Properties
                    .FirstOrDefault(p => p.Type.StartsWith("string", StringComparison.CurrentCultureIgnoreCase))?.Name;
                result.Success = true;
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on parsing Dto File: {ex.Message}", "Red");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> GenerateAsync(CRUDGenerationRequest request)
        {
            if (CurrentProject is null)
                return false;

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                await fileGeneratorService.GenerateCRUDAsync(new FileGeneratorCrudContext
                {
                    CompanyName = CurrentProject.CompanyName,
                    ProjectName = CurrentProject.Name,
                    DomainName = request.Domain,
                    EntityName = request.CRUDNameSingular,
                    EntityNamePlural = request.CRUDNamePlural,
                    BaseKeyType = request.BaseKeyType,
                    IsTeam = request.IsTeam,
                    Properties = [.. request.DtoEntity.Properties],
                    OptionItems = [.. request.SelectedOptions],
                    HasParent = request.HasParent,
                    ParentName = request.ParentName,
                    ParentNamePlural = request.ParentNamePlural,
                    AncestorTeamName = request.AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrWhiteSpace(request.AncestorTeam),
                    AngularFront = request.BiaFront,
                    GenerateBack = request.IsWebApiSelected,
                    GenerateFront = request.IsFrontSelected,
                    DisplayItemName = request.DisplayItem,
                    TeamTypeId = request.TeamTypeId,
                    TeamRoleId = request.TeamRoleId,
                    UseHubForClient = request.UseHubClient,
                    HasCustomRepository = request.HasCustomRepository,
                    HasReadOnlyMode = request.HasFormReadOnlyMode,
                    CanImport = request.UseImport,
                    IsFixable = request.IsFixable,
                    HasFixableParent = request.HasFixableParent,
                    HasAdvancedFilter = request.UseAdvancedFilter,
                    FormReadOnlyMode = request.FormReadOnlyMode,
                    IsVersioned = request.IsVersioned,
                    IsArchivable = request.IsArchivable,
                    DisplayHistorical = request.DisplayHistorical,
                    UseDomainUrl = request.UseDomainUrl,
                });

                return true;
            }

            if (!zipService.ParseZips(request.ZipFeatureTypeList, CurrentProject, request.BiaFront, settings))
                return false;

            var crudParent = new CrudParent
            {
                Exists = request.HasParent,
                Name = request.ParentName,
                NamePlural = request.ParentNamePlural,
                Domain = request.Domain
            };

            crudService.CrudNames.InitRenameValues(request.CRUDNameSingular, request.CRUDNamePlural);

            var result = crudService.GenerateFiles(
                request.DtoEntity,
                request.ZipFeatureTypeList,
                request.DisplayItem,
                request.SelectedOptions,
                crudParent,
                request.FeatureName,
                request.Domain,
                request.BiaFront);

            consoleWriter.AddMessageLine($"End of '{request.CRUDNameSingular}' generation.", "Blue");

            return result;
        }

        /// <inheritdoc/>
        public void DeleteLastGeneration(CRUDDeletionRequest request)
        {
            try
            {
                var crudParent = new CrudParent
                {
                    Exists = request.HasParent,
                    Domain = request.ParentDomain,
                    Name = request.ParentName,
                    NamePlural = request.ParentNamePlural
                };

                crudService.DeleteLastGeneration(
                    request.ZipFeatureTypeList,
                    CurrentProject,
                    request.History,
                    request.FeatureName,
                    crudParent,
                    request.HistoryOptions);

                DeleteHistoryEntry(request.History);

                consoleWriter.AddMessageLine($"End of '{request.History.EntityNameSingular}' suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on deleting last '{request.History?.EntityNameSingular}' generation: {ex.Message}", "Red");
            }
        }

        /// <inheritdoc/>
        public async Task DeleteAnnotationsAsync(IEnumerable<string> folders)
        {
            try
            {
                await crudService.DeleteBIAToolkitAnnotations(folders.ToList());
                consoleWriter.AddMessageLine($"End of annotations suppression.", "Purple");
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on cleaning annotations for project '{CurrentProject?.Name}': {ex.Message}", "Red");
            }
        }

        /// <inheritdoc/>
        public CRUDGeneration GetHistory(string projectPath)
        {
            return crudHistory;
        }

        /// <inheritdoc/>
        public CRUDGenerationHistory LoadDtoHistory(CRUDGeneration history, string dtoPath)
        {
            if (history?.CRUDGenerationHistory == null)
                return null;

            return history.CRUDGenerationHistory.FirstOrDefault(h => h.Mapping?.Dto == dtoPath);
        }

        /// <inheritdoc/>
        public void UpdateHistory(CRUDGenerationHistory history)
        {
            try
            {
                crudHistory ??= new();

                // Get existing to verify if previous generation for same entity name was already done
                var genFound = crudHistory.CRUDGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    // Remove last generation to replace by new generation
                    crudHistory.CRUDGenerationHistory.Remove(genFound);
                }

                crudHistory.CRUDGenerationHistory.Add(history);

                // Save to file
                var historyDir = Path.GetDirectoryName(crudHistoryFileName);
                if (!Directory.Exists(historyDir))
                {
                    Directory.CreateDirectory(historyDir);
                }
                CommonTools.SerializeToJsonFile(crudHistory, crudHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on CRUD generation history: {ex.Message}", "Red");
            }
        }

        /// <inheritdoc/>
        public void LoadFrontSettings(string biaFront)
        {
            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
                return;

            // Remove existing front settings
            zipFeatureTypeList.RemoveAll(x => x.GenerationType == GenerationType.Front);

            string angularBiaFolderPath = Path.Combine(CurrentProject.Folder, biaFront, Constants.FolderBia);
            string frontSettingsFileName = Path.Combine(CurrentProject.Folder, biaFront, settings.GenerationSettingsFileName);

            if (File.Exists(frontSettingsFileName))
            {
                frontSettingsList.Clear();
                frontSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(frontSettingsFileName));

                // Handle specific version adjustments
                if (CurrentProject.FrameworkVersion == "3.9.0")
                {
                    var featuresToRemove = frontSettingsList.Where(x =>
                        x.Feature == "planes-full-code" ||
                        x.Feature == "aircraft-maintenance-companies");
                    frontSettingsList = frontSettingsList.Except(featuresToRemove).ToList();
                }

                foreach (var setting in frontSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType == FeatureType.Option)
                        continue;

                    var zipFeatureType = new ZipFeatureType(
                        featureType,
                        GenerationType.Front,
                        setting.ZipName,
                        angularBiaFolderPath,
                        setting.Feature,
                        setting.Parents,
                        setting.NeedParent,
                        setting.AdaptPaths,
                        setting.FeatureDomain);

                    zipFeatureTypeList.Add(zipFeatureType);
                }
            }

            crudService.CrudNames = new(backSettingsList, frontSettingsList);
        }

        /// <inheritdoc/>
        public IEnumerable<string> ParseFrontDomains(string biaFront)
        {
            const string suffix = "-option";
            const string domainsPath = @"src\app\domains";
            var foldersName = new List<string>();

            string folderPath = Path.Combine(CurrentProject.Folder, biaFront, domainsPath);
            if (!Directory.Exists(folderPath))
                return foldersName;

            var folders = Directory.GetDirectories(folderPath, $"*{suffix}", SearchOption.AllDirectories).ToList();
            folders.ForEach(f => foldersName.Add(new DirectoryInfo(f).Name.Replace(suffix, "")));

            return foldersName.Select(x => CommonTools.ConvertKebabToPascalCase(x));
        }

        /// <summary>
        /// Gets histories using a specific option.
        /// </summary>
        public List<CRUDGenerationHistory> GetHistoriesUsingOption(string entityName)
        {
            if (crudHistory?.CRUDGenerationHistory == null)
                return [];

            return crudHistory.CRUDGenerationHistory
                .Where(h => h.OptionItems != null && h.OptionItems.Contains(entityName))
                .ToList();
        }

        private void DeleteHistoryEntry(CRUDGenerationHistory history)
        {
            if (crudHistory?.CRUDGenerationHistory == null)
                return;

            crudHistory.CRUDGenerationHistory.Remove(history);

            // Delete in options
            foreach (var optionHistory in crudHistory.CRUDGenerationHistory)
            {
                optionHistory.OptionItems?.Remove(history.EntityNameSingular);
            }

            // Save to file
            CommonTools.SerializeToJsonFile(crudHistory, crudHistoryFileName);
        }
    }
}
