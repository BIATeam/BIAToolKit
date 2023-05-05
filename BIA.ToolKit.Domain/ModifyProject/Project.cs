﻿namespace BIA.ToolKit.Domain.ModifyProject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Project
    {
        /// The folder of the project.
        public string? Folder { get; set; }

        /// The name of the project.
        public string? Name { get; set; }

        /// The name of the project.
        public string? FrameworkVersion { get; set; }

        /// The Bia framework version of the project.
        public string? CompanyName { get; set; }

        /// The folder of the project.
        public string? BIAFronts { get; set; }
    }
}
