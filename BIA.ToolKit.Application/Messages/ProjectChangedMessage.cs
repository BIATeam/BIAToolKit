using BIA.ToolKit.Domain.ModifyProject;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message sent when the current project changes
    /// </summary>
    public class ProjectChangedMessage
    {
        public Project Project { get; }

        public ProjectChangedMessage(Project project)
        {
            Project = project;
        }
    }
}
