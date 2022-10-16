namespace BIA.ToolKit.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class CustomRepoTemplate
    {
        /// The folder of the project.
        public string? Name { get; set; }

        /// The name of the project.
        public string? UrlRepo { get; set; }

        /// The name of the project.
        public string? FolderPath { get; set; }

        /// The Bia framework version of the project.
        public bool UseFolder { get; set; }
    }
}
