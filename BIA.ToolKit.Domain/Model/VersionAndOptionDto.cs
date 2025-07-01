namespace BIA.ToolKit.Domain.Model
{
    using BIA.ToolKit.Domain.Work;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class VersionAndOptionDto
    {
        // Framework version
        public string FrameworkVersion { get; set; }


        // Features settings

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the folders.
        /// </summary>
        public List<string> Folders { get; set; }


        // Company Files

        /// <summary>
        /// Gets or sets a value indicating whether use company files.
        /// </summary>
        public bool UseCompanyFiles { get; set; }

        /// <summary>
        /// Gets or sets the company file version.
        /// </summary>
        public string CompanyFileVersion{ get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the Options.
        /// </summary>
        public List<string> Options { get; set; }
    }
}
