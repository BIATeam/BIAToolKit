namespace BIA.ToolKit.Application.Services
{
    using System.Collections.Generic;
    using BIA.ToolKit.Application.Services.FileGenerator.Contexts;
    using BIA.ToolKit.Application.Models.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject;
    using BIA.ToolKit.Domain.ModifyProject.CRUDGenerator.Settings;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator;
    using BIA.ToolKit.Domain.ModifyProject.DtoGenerator.Settings;
    using BIA.ToolKit.Domain.ProjectAnalysis;
    using Humanizer;

    /// <summary>
    /// Converts generation history records into the <c>FileGeneratorService</c> context objects
    /// used to drive T4 template generation.
    /// <para>
    /// Provides a single authoritative conversion so that the interactive generation UCs (CRUD,
    /// Option, DTO) and the automated regeneration workflow produce identical context objects and,
    /// therefore, identical output.
    /// </para>
    /// </summary>
    public static class FeatureGenerationContextFactory
    {
        /// <summary>
        /// Creates a <see cref="FileGeneratorDtoContext"/> from a <see cref="DtoGeneration"/> history
        /// record and a resolved <see cref="MappingEntityProperty"/> list.
        /// An optional <paramref name="entityInfo"/> is used as a fallback for <c>BaseKeyType</c>
        /// when the value is not stored in the history.
        /// </summary>
        public static FileGeneratorDtoContext CreateDtoContext(
            DtoGeneration history,
            List<MappingEntityProperty> properties,
            Project targetProject,
            EntityInfo entityInfo = null)
        {
            return new FileGeneratorDtoContext
            {
                CompanyName = targetProject.CompanyName,
                ProjectName = targetProject.Name,
                DomainName = history.Domain,
                EntityName = history.EntityName,
                EntityNamePlural = history.EntityNamePlural ?? history.EntityName.Pluralize(),
                BaseKeyType = history.EntityBaseKeyType ?? entityInfo?.BaseKeyType,
                Properties = properties,
                IsTeam = history.IsTeam,
                IsVersioned = history.IsVersioned,
                IsArchivable = history.IsArchivable,
                IsFixable = history.IsFixable,
                AncestorTeamName = history.AncestorTeam,
                HasAncestorTeam = !string.IsNullOrEmpty(history.AncestorTeam),
                HasAudit = history.UseDedicatedAudit,
                GenerateBack = true,
            };
        }

        /// <summary>
        /// Creates a <see cref="FileGeneratorCrudContext"/> from a <see cref="CRUDGenerationHistory"/>
        /// record and a resolved <see cref="EntityInfo"/>.
        /// </summary>
        public static FileGeneratorCrudContext CreateCrudContext(
            CRUDGenerationHistory history,
            EntityInfo entityInfo,
            Project targetProject,
            bool generateBack,
            bool generateFront)
        {
            return new FileGeneratorCrudContext
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
            };
        }

        /// <summary>
        /// Creates a <see cref="FileGeneratorOptionContext"/> from an
        /// <see cref="OptionGenerationHistory"/> record and a resolved <see cref="EntityInfo"/>
        /// (which supplies the <c>BaseKeyType</c> not stored in the history).
        /// </summary>
        public static FileGeneratorOptionContext CreateOptionContext(
            OptionGenerationHistory history,
            EntityInfo entityInfo,
            Project targetProject)
        {
            return new FileGeneratorOptionContext
            {
                CompanyName = targetProject.CompanyName,
                ProjectName = targetProject.Name,
                DomainName = history.Domain,
                EntityName = history.EntityNameSingular,
                EntityNamePlural = history.EntityNamePlural,
                BaseKeyType = entityInfo?.BaseKeyType,
                DisplayName = history.DisplayItem,
                AngularFront = history.BiaFront,
                UseHubForClient = history.UseHubClient,
                GenerateBack = true,
                GenerateFront = true,
            };
        }
    }
}
