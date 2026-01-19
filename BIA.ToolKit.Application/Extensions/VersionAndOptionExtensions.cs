namespace BIA.ToolKit.Application.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Work;

    public static class VersionAndOptionExtensions
    {
        public static VersionAndOptionDto ToDto(this VersionAndOption versionAndOption)
        {
            var dto = new VersionAndOptionDto
            {
                FrameworkVersion = versionAndOption.WorkTemplate?.Version,
                Tags = versionAndOption.FeatureSettings
                    .Where(f => f.IsSelected)
                    .SelectMany(f => f.Tags)
                    .Distinct()
                    .ToList(),
                Folders = versionAndOption.FeatureSettings
                    .Where(f => f.IsSelected)
                    .SelectMany(f => f.FoldersToExcludes)
                    .Distinct()
                    .ToList(),
                UseCompanyFiles = versionAndOption.UseCompanyFiles,
                Options = versionAndOption.UseCompanyFiles && versionAndOption.Options is not null
                    ? versionAndOption.Options.Select(o => o.Name).ToList()
                    : new List<string>(),
                CompanyFileVersion = versionAndOption.WorkCompanyFile?.Version,
                Profile = versionAndOption.Profile
            };

            return dto;
        }
    }
}
