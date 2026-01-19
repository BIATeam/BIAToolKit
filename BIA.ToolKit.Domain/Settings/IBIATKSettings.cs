namespace BIA.ToolKit.Domain.Settings
{
    using System.Collections.Generic;

    public interface IBIATKSettings
    {
        bool AutoUpdate { get;}
        IRepository ToolkitRepository { get;}
        IReadOnlyList<IRepository> TemplateRepositories { get;}
        IReadOnlyList<IRepository> CompanyFilesRepositories { get;}
        string CreateCompanyName { get;}
        string CreateProjectRootProjectsPath { get; }
        string ModifyProjectRootProjectsPath { get; }
        bool UseCompanyFiles { get;}
    }
}
