namespace BIA.ToolKit.Application.Mapper
{
    using BIA.ToolKit.Domain.Model;
    using System.Collections.Generic;
    using System.Linq;

    public static class VersionAndOptionMapper
    {
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
