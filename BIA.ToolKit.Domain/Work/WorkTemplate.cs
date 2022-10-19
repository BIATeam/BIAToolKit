namespace BIA.ToolKit.Domain.Work
{
    using BIA.ToolKit.Domain.Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class WorkTemplate
    {
        public Repository Template { get; private set; }
        public string FolderPath { get; private set; }
        public string Version { get; private set; }
        public string CompanyFilefolder { get; private set; }

        WorkTemplate(Repository template, string version, string folderPath, string companyFilefolder)
        {
            this.Template = template;
            this.Version = version;
            this.FolderPath = folderPath;
            this.CompanyFilefolder = companyFilefolder;
        }
    }
}
