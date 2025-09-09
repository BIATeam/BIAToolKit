namespace BIA.ToolKit.Domain.Settings
{
    using System.Collections.Generic;

    public interface IBIATKSettings
    {
        bool AutoUpdate { get;}
        Repository ToolkitRepository { get;}
        IReadOnlyList<Repository> TemplateRepositories { get;}
        IReadOnlyList<Repository> CompanyFilesRepositories { get;}
        string CreateCompanyName { get;}
        string CreateProjectRootProjectsPath { get; }
        string ModifyProjectRootProjectsPath { get; }
        bool UseCompanyFiles { get;}
    }
}