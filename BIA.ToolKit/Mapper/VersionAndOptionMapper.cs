namespace BIA.ToolKit.Mapper
{
    using BIA.ToolKit.ViewModels;
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

            if (mapFrameworkVersion)
            {
                var workTemplate = vm.WorkTemplates.FirstOrDefault(x => x.Version == dto.FrameworkVersion);
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
            dto.Tags = model.FeatureSettings
                .Where(f => f.IsSelected)
                .SelectMany(f => f.Tags)
                .Distinct()
                .ToList();

            dto.Folders = model.FeatureSettings
                .Where(f => f.IsSelected)
                .SelectMany(f => f.FoldersToExcludes)
                .Distinct()
                .ToList();

            // Company Files
            dto.UseCompanyFiles = model.UseCompanyFiles;
            dto.CompanyFileVersion = model.WorkCompanyFile?.Version;
            dto.Profile = model.Profile;
            dto.Options = model.Options?.Where(o => o.IsChecked).Select(o => o.Key).ToList();
        }

    }
}
