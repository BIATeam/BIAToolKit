namespace BIA.ToolKit.Application.Services.RegenerateFeatures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Application.Services.FileGenerator;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Common;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;
    using BIA.ToolKit.Domain.ProjectAnalysis;
    using Humanizer;

    public class FeatureMigrationGeneratorService(IConsoleWriter consoleWriter)
    {
        private class MutedConsoleWriter : IConsoleWriter
        {
            public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
            {
                return;
            }
        }

        private const int REGENERATE_FEATURES_VERSION_MINIMUM = 500;
        private readonly IConsoleWriter consoleWriter = consoleWriter;

        public static bool IsProjectCompatibleForRegenerateFeatures(Project project)
        {
            if (!string.IsNullOrEmpty(project?.FrameworkVersion))
            {
                string version = project.FrameworkVersion.Replace(".", "");
                if (int.TryParse(version, out int value))
                {
                    return value >= REGENERATE_FEATURES_VERSION_MINIMUM;
                }
            }

            return false;
        }

        /// <summary>
        /// Generates features (CRUD, Option, DTO) into the target project folder (FROM or TO)
        /// using the given framework version's templates.
        /// </summary>
        public async Task GenerateFeaturesAsync(
            Project currentProject,
            string targetFolderPath,
            string targetVersion,
            List<RegenerableEntity> entities,
            CSharpParserService parserService)
        {
            consoleWriter.AddMessageLine($"Feature migration: Generating features for version {targetVersion} into {targetFolderPath}", "pink");

            var targetProject = new Project
            {
                CompanyName = currentProject.CompanyName,
                Name = currentProject.Name,
                FrameworkVersion = targetVersion[1..],
                Folder = targetFolderPath,
                BIAFronts = [.. currentProject.BIAFronts],
            };

            var fileGenerator = new FileGeneratorService(new MutedConsoleWriter());
            await fileGenerator.Init(targetProject, fromUnitTest: false);

            if (!fileGenerator.IsInit)
            {
                consoleWriter.AddMessageLine($"Feature migration: Unable to initialize generator for version {targetVersion}, skipping features.", "orange");
                return;
            }

            foreach (RegenerableEntity entity in entities
                .Where(e => e.CanSelectEntity)
                .OrderBy(e => e.LastGenerationDate))
            {
                await GenerateEntityFeaturesAsync(entity, currentProject, targetProject, fileGenerator, parserService);
            }

            consoleWriter.AddMessageLine($"Feature migration: Completed generating features for version {targetVersion} into {targetFolderPath}", "green");
        }

        private async Task GenerateEntityFeaturesAsync(
            RegenerableEntity entity,
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            _ = new List<Task>();
            if (entity.OptionHistory != null && entity.OptionStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateOptionAsync(entity.OptionHistory, targetProject, fileGenerator);
            }

            if (entity.DtoHistory != null && entity.DtoStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateDtoAsync(entity.DtoHistory, targetProject, fileGenerator);
            }

            if (entity.CrudHistory != null && entity.CrudStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateCrudAsync(entity.CrudHistory, currentProject, targetProject, fileGenerator, parserService);
            }
        }

        private async Task TryGenerateOptionAsync(
            OptionGenerationHistory history,
            Project targetProject,
            FileGeneratorService fileGenerator)
        {
            try
            {
                await fileGenerator.GenerateOptionAsync(new FileGeneratorOptionContext
                {
                    CompanyName = targetProject.CompanyName,
                    ProjectName = targetProject.Name,
                    DomainName = history.Domain,
                    EntityName = history.EntityNameSingular,
                    EntityNamePlural = history.EntityNamePlural,
                    DisplayName = history.DisplayItem,
                    AngularFront = history.BiaFront,
                    UseHubForClient = history.UseHubClient,
                    GenerateBack = true,
                    GenerateFront = true,
                });
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating Option for {history.EntityNameSingular}: {ex.Message}", "orange");
            }
        }

        private async Task TryGenerateDtoAsync(
            DtoGeneration history,
            Project targetProject,
            FileGeneratorService fileGenerator)
        {
            try
            {
                var properties = history.PropertyMappings.Select(pm => new MappingEntityProperty
                {
                    EntityCompositeName = pm.EntityPropertyCompositeName,
                    MappingName = pm.MappingName ?? pm.EntityPropertyCompositeName,
                    IsRequired = pm.IsRequired,
                    MappingDateType = pm.DateType,
                    OptionDisplayProperty = pm.OptionMappingDisplayProperty,
                    OptionIdProperty = pm.OptionMappingIdProperty,
                    OptionEntityIdProperty = pm.OptionMappingEntityIdProperty,
                    IsParent = pm.IsParent,
                    EntityType = !string.IsNullOrEmpty(pm.OptionMappingEntityIdProperty)
                        ? Constants.BiaClassName.CollectionOptionDto
                        : !string.IsNullOrEmpty(pm.OptionMappingIdProperty)
                            ? Constants.BiaClassName.OptionDto
                            : string.Empty,
                }).ToList();

                await fileGenerator.GenerateDtoAsync(new FileGeneratorDtoContext
                {
                    CompanyName = targetProject.CompanyName,
                    ProjectName = targetProject.Name,
                    DomainName = history.Domain,
                    EntityName = history.EntityName,
                    EntityNamePlural = history.EntityName ?? history.EntityName.Pluralize(),
                    BaseKeyType = history.EntityBaseKeyType ?? "int",
                    Properties = properties,
                    IsTeam = history.IsTeam,
                    IsVersioned = history.IsVersioned,
                    IsArchivable = history.IsArchivable,
                    IsFixable = history.IsFixable,
                    AncestorTeamName = history.AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrEmpty(history.AncestorTeam),
                    HasAudit = history.UseDedicatedAudit,
                    GenerateBack = true,
                });
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating DTO for {history.EntityName}: {ex.Message}", "orange");
            }
        }

        private async Task TryGenerateCrudAsync(
            CRUDGenerationHistory history,
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            try
            {
                if (history.Mapping?.Dto == null)
                    return;

                string dtoFilePath = Path.Combine(currentProject.Folder, Constants.FolderDotNet, history.Mapping.Dto);
                ClassInfo classInfo = parserService.CurrentSolutionClasses
                    .FirstOrDefault(c => string.Equals(c.FilePath, dtoFilePath, StringComparison.OrdinalIgnoreCase));

                if (classInfo == null)
                {
                    consoleWriter.AddMessageLine($"Feature migration: DTO class not found for {history.EntityNameSingular}, skipping CRUD.", "orange");
                    return;
                }

                var entityInfo = new EntityInfo(classInfo);

                bool generateBack = history.Generation.Any(g => g.GenerationType == "WebApi");
                bool generateFront = history.Generation.Any(g => g.GenerationType == "Front")
                                     && !string.IsNullOrEmpty(history.BiaFront);

                await fileGenerator.GenerateCRUDAsync(new FileGeneratorCrudContext
                {
                    CompanyName = targetProject.CompanyName,
                    ProjectName = targetProject.Name,
                    DomainName = history.Domain,
                    EntityName = history.EntityNameSingular,
                    EntityNamePlural = history.EntityNamePlural,
                    BaseKeyType = history.EntityBaseKeyType,
                    IsTeam = history.IsTeam,
                    Properties = [.. entityInfo.Properties],
                    OptionItems = history.OptionItems ?? [],
                    HasParent = history.HasParent,
                    ParentName = history.ParentName,
                    ParentNamePlural = history.ParentNamePlural,
                    AncestorTeamName = history.AncestorTeam,
                    HasAncestorTeam = !string.IsNullOrEmpty(history.AncestorTeam),
                    AngularFront = generateFront ? history.BiaFront : null,
                    GenerateBack = generateBack,
                    GenerateFront = generateFront,
                    DisplayItemName = history.DisplayItem,
                    TeamTypeId = history.TeamTypeId,
                    TeamRoleId = history.TeamRoleId,
                    UseHubForClient = history.UseHubClient,
                    HasCustomRepository = history.UseCustomRepository,
                    HasReadOnlyMode = history.HasFormReadOnlyMode,
                    CanImport = history.UseImport,
                    IsFixable = history.IsFixable,
                    HasFixableParent = history.HasFixableParent,
                    HasAdvancedFilter = history.HasAdvancedFilter,
                    FormReadOnlyMode = history.FormReadOnlyMode,
                    IsVersioned = history.IsVersioned,
                    IsArchivable = history.IsArchivable,
                    DisplayHistorical = history.DisplayHistorical,
                    UseDomainUrl = history.UseDomainUrl,
                });
            }
            catch (Exception ex)
            {
                consoleWriter.AddMessageLine($"Feature migration: Error generating CRUD for {history.EntityNameSingular}: {ex.Message}", "orange");
            }
        }
    }
}
