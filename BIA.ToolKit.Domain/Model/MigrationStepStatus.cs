namespace BIA.ToolKit.Domain.Model
{
    /// <summary>
    /// Lifecycle states of a single step in the project migration workflow.
    /// Used both by <c>MigrationStep</c> (ViewModel) and by the Output stripe
    /// renderer to colorize lines emitted during the step's execution.
    /// </summary>
    public enum MigrationStepStatus
    {
        Pending,
        Running,
        Done,
        Warning,
        Failed,
    }
}
