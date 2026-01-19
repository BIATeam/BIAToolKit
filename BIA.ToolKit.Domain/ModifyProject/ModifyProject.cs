namespace BIA.ToolKit.Domain.ModifyProject
{
    public class ModifyProject
    {
        public ModifyProject()
        {
            Projects = new List<string>();
            CurrentProject = null;
        }

        #region Properties

        public List<string> Projects { get; set; }


        // The selected project in RootProjectPath
        public Project CurrentProject { get; set; }

        #endregion
    }
}
