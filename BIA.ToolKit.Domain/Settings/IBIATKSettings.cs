namespace BIA.ToolKit.Domain.Settings
{
    using System.Collections.Generic;

    public interface IBIATKSettings
    {
        bool AutoUpdate { get;}
        IRepositorySettings BIATemplateRepository { get;}
        IRepositorySettings CompanyFilesRepository { get;}
        string CreateCompanyName { get;}
        IReadOnlyList<IRepositorySettings> CustomRepoTemplates { get;}
        string LocalReleaseRepositoryPath { get;}
        string CreateProjectRootProjectsPath { get; }
        string ModifyProjectRootProjectsPath { get; }
        bool UseCompanyFiles { get;}
        bool UseLocalReleaseRepository { get;}
    }
}