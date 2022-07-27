namespace BIA.ToolKit.Domain.ModifyProject
{
    public class ModifyProject
    {
        public ModifyProject()
        {
            RootProjectsPath = "D:\\...\\MyRootProjectPath";
            Projects = new List<string>();
            CurrentProject = null;
        }

        #region Properties
        /// The parrent directory of all projects.
        public string RootProjectsPath { get; set; }

        public List<string> Projects { get; set; }


        // The selected project in RootProjectPath
        public Project? CurrentProject { get; set; }

        #endregion
    }
}
