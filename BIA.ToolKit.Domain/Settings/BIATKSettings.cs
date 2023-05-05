namespace BIA.ToolKit.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BIATKSettings
    {
        public RepositorySettings BIATemplateRepository  { get; set; }
        public bool UseCompanyFiles { get; set; }
        public RepositorySettings CompanyFiles { get; set; }
        public List<RepositorySettings> CustomRepoTemplates { get; set; }

        public string RootProjectsPath { get; set; }
        public string CreateCompanyName { get; set; }

        public BIATKSettings()
        {
            RootProjectsPath = "D:\\...\\MyRootProjectPath";
            CreateCompanyName = "";
            BIATemplateRepository  = new RepositorySettings();
            CustomRepoTemplates = new List<RepositorySettings>();
            CompanyFiles = new RepositorySettings();
        }
    }
}
