namespace BIA.ToolKit.Application.Services.Option
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
    /// Service implementation for Option generation operations.
    /// Encapsulates business logic extracted from OptionGeneratorViewModel.
    /// </summary>
    public class OptionGenerationService : IOptionGenerationService
    {
        private const string DOTNET_TYPE = "DotNet";
        private const string ANGULAR_TYPE = "Angular";

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
        private OptionGeneration optionHistory;
        private string optionHistoryFileName;

        public Project CurrentProject { get; set; }

        public OptionGenerationService(
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
        public Task<OptionInitializationResult> InitializeAsync(Project project)
        {
            CurrentProject = project;
            zipFeatureTypeList = [];

            var result = new OptionInitializationResult();

            // Get files/folders name
            string dotnetBiaFolderPath = Path.Combine(project.Folder, Constants.FolderDotNet, Constants.FolderBia);
            string backSettingsFileName = Path.Combine(project.Folder, Constants.FolderDotNet, settings.GenerationSettingsFileName);
            optionHistoryFileName = Path.Combine(project.Folder, Constants.FolderBia, settings.OptionGenerationHistoryFileName);

            // Load generation history
            optionHistory = CommonTools.DeserializeJsonFile<OptionGeneration>(optionHistoryFileName);
            result.History = optionHistory;

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                result.ZipFeatureTypeList = zipFeatureTypeList;
                return Task.FromResult(result);
            }

            // Load BIA settings
            if (File.Exists(backSettingsFileName))
            {
                backSettingsList.Clear();
                backSettingsList.AddRange(CommonTools.DeserializeJsonFile<List<FeatureGenerationSettings>>(backSettingsFileName));

                foreach (var setting in backSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType != FeatureType.Option)
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
                        setting.FeatureDomain)
                    {
                        IsChecked = true
                    };
                    zipFeatureTypeList.Add(zipFeatureType);
                }
            }

            result.BackSettings = [.. backSettingsList];
            result.FrontSettings = [.. frontSettingsList];
            result.ZipFeatureTypeList = zipFeatureTypeList;

            crudService.CurrentProject = project;
            crudService.CrudNames = new(backSettingsList, frontSettingsList);

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public IEnumerable<EntityInfo> ListEntities(Project project)
        {
            return parserService.GetDomainEntities(project);
        }

        /// <inheritdoc/>
        public OptionEntityParseResult ParseEntityFile(EntityInfo entity)
        {
            var result = new OptionEntityParseResult();

            try
            {
                if (entity is null)
                    return result;

                // Extract domain from namespace
                var namespaceParts = entity.Namespace.Split('.').ToList();
                var domainIndex = namespaceParts.IndexOf("Domain");
                if (domainIndex != -1)
                {
                    result.Domain = namespaceParts[domainIndex + 1];
                }

                result.EntityNamePlural = entity.NamePluralized;

                if (entity.Properties.Count == 0)
                {
                    result.ErrorMessage = "No properties found on entity file.";
                    return result;
                }

                foreach (var property in entity.Properties.OrderBy(x => x.Name))
                {
                    result.DisplayItems.Add(property.Name);
                }

                // Check history for default display item
                var history = optionHistory?.OptionGenerationHistory?
                    .FirstOrDefault(gh => entity.Name == textParsingService.ExtractClassNameFromFile(gh.Mapping.Entity));
                result.DefaultDisplayItem = history?.DisplayItem;

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error on parsing Entity File: {ex.Message}";
                consoleWriter.AddMessageLine(result.ErrorMessage, "Red");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> GenerateAsync(OptionGenerationRequest request)
        {
            if (CurrentProject is null)
                return false;

            if (fileGeneratorService.IsProjectCompatibleForCrudOrOptionFeature())
            {
                await fileGeneratorService.GenerateOptionAsync(new FileGeneratorOptionContext
                {
                    CompanyName = CurrentProject.CompanyName,
                    ProjectName = CurrentProject.Name,
                    DomainName = request.Domain,
                    EntityName = request.Entity.Name,
                    EntityNamePlural = request.Entity.NamePluralized,
                    BaseKeyType = request.Entity.BaseKeyType,
                    DisplayName = request.DisplayItem,
                    AngularFront = request.BiaFront,
                    GenerateFront = true,
                    GenerateBack = true,
                });

                return true;
            }

            if (!zipService.ParseZips(zipFeatureTypeList, CurrentProject, request.BiaFront, settings))
                return false;

            crudService.CrudNames.InitRenameValues(request.Entity.Name, request.EntityNamePlural);

            var featureName = zipFeatureTypeList.FirstOrDefault(x => x.FeatureType == FeatureType.Option)?.Feature;
            var result = crudService.GenerateFiles(
                request.Entity,
                zipFeatureTypeList,
                request.DisplayItem,
                null,
                null,
                featureName,
                request.Domain,
                request.BiaFront);

            consoleWriter.AddMessageLine($"End of '{request.Entity.Name}' option generation.", "Blue");

            return result;
        }

        /// <inheritdoc/>
        public void DeleteLastGeneration(OptionDeletionRequest request)
        {
            try
            {
                var crudParent = new CrudParent { Domain = request.Domain };

                crudService.DeleteLastGeneration(
                    zipFeatureTypeList,
                    CurrentProject,
                    request.History,
                    FeatureType.Option.ToString(),
                    crudParent,
                    null);

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
        public OptionGeneration GetHistory()
        {
            return optionHistory;
        }

        /// <inheritdoc/>
        public OptionGenerationHistory LoadEntityHistory(string entityPath)
        {
            if (optionHistory?.OptionGenerationHistory == null)
                return null;

            return optionHistory.OptionGenerationHistory.FirstOrDefault(h => h.Mapping?.Entity == entityPath);
        }

        /// <inheritdoc/>
        public void UpdateHistory(OptionGenerationHistory history)
        {
            try
            {
                optionHistory ??= new();

                // Get existing to verify if previous generation for same entity name was already done
                var genFound = optionHistory.OptionGenerationHistory.FirstOrDefault(gen => gen.EntityNameSingular == history.EntityNameSingular);
                if (genFound != null)
                {
                    optionHistory.OptionGenerationHistory.Remove(genFound);
                }

                optionHistory.OptionGenerationHistory.Add(history);

                // Save to file
                var historyDir = Path.GetDirectoryName(optionHistoryFileName);
                if (!Directory.Exists(historyDir))
                {
                    Directory.CreateDirectory(historyDir);
                }
                CommonTools.SerializeToJsonFile(optionHistory, optionHistoryFileName);
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Error on Option generation history: {ex.Message}", "Red");
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

                foreach (var setting in frontSettingsList)
                {
                    var featureType = Enum.Parse<FeatureType>(setting.Type);
                    if (featureType != FeatureType.Option)
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
                        setting.FeatureDomain)
                    {
                        IsChecked = true
                    };

                    zipFeatureTypeList.Add(zipFeatureType);
                }
            }

            crudService.CrudNames = new(backSettingsList, frontSettingsList);
        }

        private void DeleteHistoryEntry(OptionGenerationHistory history)
        {
            if (optionHistory?.OptionGenerationHistory == null)
                return;

            optionHistory.OptionGenerationHistory.Remove(history);
            CommonTools.SerializeToJsonFile(optionHistory, optionHistoryFileName);
        }
    }
}
