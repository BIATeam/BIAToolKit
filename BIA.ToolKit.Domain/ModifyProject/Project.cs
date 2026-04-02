namespace BIA.ToolKit.Domain.ModifyProject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Project
    {
        /// The folder of the project.
        public string Folder { get; set; }

        /// The name of the project.
        public string Name { get; set; }

        /// The name of the project.
        public string FrameworkVersion { get; set; }

        /// The Bia framework version of the project.
        public string CompanyName { get; set; }

        /// The BIA front folders of the project.
        public List<string> BIAFronts { get; set; } = new List<string>();

        /// <summary>
        /// Path to the solution of the project
        /// </summary>
        public string SolutionPath { get; set; }

        public IReadOnlyList<string> ProjectFiles { get; private set; }

        public async Task ListProjectFiles()
        {
            await Task.Run(() => ProjectFiles = Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories).ToList());
        }
    }
}
