namespace BIA.ToolKit.Application.Mapper
{
    using BIA.ToolKit.Application.ViewModel;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Work;
    using System.Linq;

    public static class VersionAndOptionMapper
    {
        public static void DtoToModel(VersionAndOptionDto dto, VersionAndOptionViewModel vm, bool mapCompanyFileVersion)
        {
            // Company Files
            vm.UseCompanyFiles = dto.UseCompanyFiles;
            if (mapCompanyFileVersion)
            {
                vm.WorkCompanyFile = vm.WorkCompanyFiles?.FirstOrDefault(w => w.Version == dto.CompanyFileVersion);
            }

            if (vm.Profiles.Any(p => p == dto.Profile))
            {
                vm.Profile = dto.Profile;
            }

            vm.CheckOptions(dto.Options);
        }

        public static void ModelToDto(VersionAndOption model, VersionAndOptionDto dto)
        {
            dto.FrameworkVersion = model.WorkTemplate?.Version;

            // Company Files
            dto.UseCompanyFiles = model.UseCompanyFiles;
            dto.CompanyFileVersion = model.WorkCompanyFile?.Version;
            dto.Profile = model.Profile;
            dto.Options = model.Options?.Where(o => o.IsChecked).Select(o => o.Key).ToList();
        }

    }
}
