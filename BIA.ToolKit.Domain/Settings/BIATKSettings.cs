namespace BIA.ToolKit.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BIATKSettings
    {
        public Repository BIATemplateRepository  { get; set; }
        public bool UseCompanyFiles { get; set; }
        public Repository CompanyFiles { get; set; }
        public List<Repository> CustomRepoTemplates { get; set; }

        public string RootProjectsPath { get; set; }
        public string CreateCompanyName { get; set; }

        public BIATKSettings()
        {
            RootProjectsPath = "D:\\...\\MyRootProjectPath";
            CreateCompanyName = "";
            BIATemplateRepository  = new Repository();
            CustomRepoTemplates = new List<Repository>();
            CompanyFiles = new Repository();
        }
    }
}
