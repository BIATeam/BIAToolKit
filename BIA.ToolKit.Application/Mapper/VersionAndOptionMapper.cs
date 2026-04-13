namespace BIA.ToolKit.Application.Mapper
{
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Work;
    using System.Collections.Generic;
    using System.Linq;

    public static class VersionAndOptionMapper
    {
        public static void DtoToModel(VersionAndOptionDto dto, VersionAndOptionViewModel vm, bool mapCompanyFileVersion, bool mapFrameworkVersion, List<FeatureSetting> originFeatureSettings)
        {
            // Company Files
            vm.UseCompanyFiles = dto.UseCompanyFiles;

            // Feature
            vm.SetFeaturesSelection(dto.Tags, dto.Folders, originFeatureSettings);

            if (dto.HasDefaultTeam)
            {
                FeatureSettingViewModel createDefaultTeamVm = vm.FeatureSettings.FirstOrDefault(f => f.IsCreateDefaultTeam);
                createDefaultTeamVm?.IsSelected = true;
            }

            // Company Files
            if (mapCompanyFileVersion)
            {
                vm.WorkCompanyFile = vm.GetWorkCompanyFile(dto.CompanyFileVersion);
            }

            if (vm.Profiles.Any(p => p == dto.Profile))
            {
                vm.Profile = dto.Profile;
            }

            vm.CheckOptions(dto.Options);

            // Default Team
            vm.DefaultTeamName = dto.DefaultTeamName;
            vm.DefaultTeamNamePlural = dto.DefaultTeamNamePlural;
            vm.DefaultTeamDomainName = dto.DefaultTeamDomainName;

            if (mapFrameworkVersion)
            {
                WorkRepository workTemplate = vm.WorkTemplates.FirstOrDefault(x => x.Version == dto.FrameworkVersion);
                if (workTemplate is not null)
                {
                    vm.WorkTemplate = workTemplate;
                }
            }
        }

        public static void ModelToDto(VersionAndOption model, VersionAndOptionDto dto)
        {
            dto.FrameworkVersion = model.WorkTemplate?.Version;

            // Feature
            dto.Tags = [.. model.FeatureSettings
                .Where(f => f.IsSelected)
                .SelectMany(f => f.Tags)
                .Distinct()];

            dto.Folders = [.. model.FeatureSettings
                .Where(f => f.IsSelected)
                .SelectMany(f => f.FoldersToExcludes)
                .Distinct()];

            // Company Files
            dto.UseCompanyFiles = model.UseCompanyFiles;
            dto.CompanyFileVersion = model.WorkCompanyFile?.Version;
            dto.Profile = model.Profile;
            dto.Options = model.Options?.Where(o => o.IsChecked).Select(o => o.Key).ToList();

            dto.HasDefaultTeam = model.HasDefaultTeam;
            dto.DefaultTeamName = model.DefaultTeamName;
            dto.DefaultTeamDomainName = model.DefaultTeamDomainName;
            dto.DefaultTeamNamePlural = model.DefaultTeamNamePlural;
        }

    }
}
