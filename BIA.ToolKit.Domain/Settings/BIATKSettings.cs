namespace BIA.ToolKit.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BIATKSettings : IBIATKSettings
    {
        public bool UseCompanyFiles { get; set; }
        public Repository ToolkitRepository { get; set; }
        public IReadOnlyList<Repository> TemplateRepositories { get; set; }
        public IReadOnlyList<Repository> CompanyFilesRepositories { get; set; }
        public string CreateProjectRootProjectsPath { get; set; }
        public string ModifyProjectRootProjectsPath { get; set; }
        public string CreateCompanyName { get; set; }
        public bool AutoUpdate { get; set; }
    }
}
