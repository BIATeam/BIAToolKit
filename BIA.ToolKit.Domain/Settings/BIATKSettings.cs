namespace BIA.ToolKit.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class BIATKSettings : IBIATKSettings
    {
        public bool UseCompanyFiles { get; set; }
        [JsonIgnore]
        public IRepository ToolkitRepository { get; set; }
        [JsonIgnore]
        public IReadOnlyList<IRepository> TemplateRepositories { get; set; }
        [JsonIgnore]
        public IReadOnlyList<IRepository> CompanyFilesRepositories { get; set; }
        public string CreateProjectRootProjectsPath { get; set; }
        public string ModifyProjectRootProjectsPath { get; set; }
        public string CreateCompanyName { get; set; }
        public bool AutoUpdate { get; set; }
        public RepositoryUserConfig ToolkitRepositoryConfig { get; set; }
        public List<RepositoryUserConfig> TemplateRepositoriesConfig { get; set; }
        public List<RepositoryUserConfig> CompanyFilesRepositoriesConfig { get; set; }

        public void InitRepositoriesInterfaces()
        {
            ToolkitRepository = ToolkitRepositoryConfig.ToRepository();
            TemplateRepositories = TemplateRepositoriesConfig.Select(x => x.ToRepository()).ToList();
            CompanyFilesRepositories = CompanyFilesRepositoriesConfig.Select(x => x.ToRepository()).ToList();
        }
    }
}
