namespace BIA.ToolKit.Domain.Model
{
    using System.Collections.Generic;

    public class ProjectParameters
    {
        public ProjectParameters()
        {
            CompanyName = "";
            ProjectName = "";
            VersionAndOption = new VersionAndOption();
            AngularFronts = new List<string>();
        }

        /// <summary>
        /// Gets or sets the Company Name.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the Project Name.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Gets or sets the VersionAndOption parameter.
        /// </summary>
        public VersionAndOption VersionAndOption { get; set; }

        /// <summary>
        /// Gets or sets the angular fronts name.
        /// </summary>
        public List<string> AngularFronts { get; set; }
    }
}
