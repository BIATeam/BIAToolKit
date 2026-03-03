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
    using BIA.ToolKit.Domain.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.RegenerateFeatures;

    public class FeatureMigrationGeneratorService
    {
        private readonly IConsoleWriter consoleWriter;

        public FeatureMigrationGeneratorService(IConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
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

            var fileGenerator = new FileGeneratorService(consoleWriter);
            // fromUnitTest=true skips Angular prettier calls (not available in temp migration folders)
            await fileGenerator.Init(targetProject, fromUnitTest: false);

            if (!fileGenerator.IsInit)
            {
                consoleWriter.AddMessageLine($"Feature migration: Unable to initialize generator for version {targetVersion}, skipping features.", "orange");
                return;
            }

            foreach (var entity in entities.Where(e => e.CanSelectEntity))
            {
                await GenerateEntityFeaturesAsync(entity, currentProject, targetProject, fileGenerator, parserService);
            }

            consoleWriter.AddMessageLine("Feature migration: Features generated.", "pink");
        }

        private async Task GenerateEntityFeaturesAsync(
            RegenerableEntity entity,
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            if (entity.OptionHistory != null && entity.OptionStatus == RegenerableFeatureStatus.Ready)
            {
                await TryGenerateOptionAsync(entity.OptionHistory, currentProject, targetProject, fileGenerator, parserService);
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
            Project currentProject,
            Project targetProject,
            FileGeneratorService fileGenerator,
            CSharpParserService parserService)
        {
            try
            {
                string domainFolder = $"{currentProject.CompanyName}.{currentProject.Name}.Domain";
                string entityFilePath = Path.Combine(currentProject.Folder, Constants.FolderDotNet, domainFolder, history.Domain, "Entities", $"{history.EntityNameSingular}.cs");
                var classInfo = parserService.CurrentSolutionClasses
                    .FirstOrDefault(c => string.Equals(c.FilePath, entityFilePath, StringComparison.OrdinalIgnoreCase));

                string baseKeyType = "int";
                bool isTeam = false;
                if (classInfo != null)
                {
                    var entityInfo = new EntityInfo(classInfo);
                    baseKeyType = entityInfo.BaseKeyType ?? "int";
                    isTeam = entityInfo.IsTeam;
                }

                bool generateBack = history.Generation.Any(g => g.GenerationType == "WebApi");
                bool generateFront = history.Generation.Any(g => g.GenerationType == "Front")
                                     && !string.IsNullOrEmpty(history.BiaFront);

                await fileGenerator.GenerateOptionAsync(new FileGeneratorOptionContext
                {
                    CompanyName = targetProject.CompanyName,
                    ProjectName = targetProject.Name,
                    DomainName = history.Domain,
                    EntityName = history.EntityNameSingular,
                    EntityNamePlural = history.EntityNamePlural ?? (history.EntityNameSingular + "s"),
                    BaseKeyType = baseKeyType,
                    DisplayName = history.DisplayItem,
                    AngularFront = generateFront ? history.BiaFront : null,
                    UseHubForClient = history.UseHubClient,
                    GenerateBack = generateBack,
                    GenerateFront = generateFront,
                    IsTeam = isTeam,
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
                    // DtoGeneration history does not store EntityNamePlural; use simple fallback since both FROM and TO use the same value
                    EntityNamePlural = history.EntityName + "s",
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
                    GenerateFront = false,
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
                var classInfo = parserService.CurrentSolutionClasses
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
                    EntityNamePlural = history.EntityNamePlural ?? (history.EntityNameSingular + "s"),
                    BaseKeyType = history.EntityBaseKeyType ?? entityInfo.BaseKeyType ?? "int",
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
