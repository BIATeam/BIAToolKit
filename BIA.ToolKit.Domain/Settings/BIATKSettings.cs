namespace BIA.ToolKit.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BIATKSettings : IBIATKSettings
    {
        public IRepositorySettings BIATemplateRepository { get; set; } = new RepositorySettings();
        public bool UseCompanyFiles { get; set; }
        public IRepositorySettings CompanyFilesRepository { get; set; } = new RepositorySettings();
        public IReadOnlyList<IRepositorySettings> CustomRepoTemplates { get; set; } = [];
        public string CreateProjectRootProjectsPath { get; set; }
        public string ModifyProjectRootProjectsPath { get; set; }
        public string CreateCompanyName { get; set; }
        public bool AutoUpdate { get; set; }
        public bool UseLocalReleaseRepository { get; set; }
        public string LocalReleaseRepositoryPath { get; set; }
    }
}
