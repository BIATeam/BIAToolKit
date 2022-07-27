namespace BIA.ToolKit.Domain.Main
{
    public class Main
    {
        public Main()
        {
            RootProjectsPath = "D:\\...\\MyRootProjectPath";
        }

        #region Properties
        /// The parrent directory of all projects.
        public string RootProjectsPath { get; set; }

        #endregion
    }
}
