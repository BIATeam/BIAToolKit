namespace BIA.ToolKit.ViewModel.Messaging.Messages
{
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.Domain.ModifyProject;

    /// <summary>
    /// Published when the active project changes.
    /// </summary>
    public class ProjectChangedMessage : IMessage
    {
        /// <summary>
        /// Gets or sets the newly selected project. May be <c>null</c> when the user deselects the current project.
        /// </summary>
        public Project? Project { get; set; }
    }
}
