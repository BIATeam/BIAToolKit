namespace BIA.ToolKit.ViewModels
{
    using System.Windows.Input;
    using BIA.ToolKit.Domain.Model;
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// One cell of the <c>MigrationStepperUC</c>. Carries its number, label,
    /// the command that runs the step, and a status that the visual cell
    /// binds to (driving glyph + color via converters).
    /// </summary>
    public partial class MigrationStep : ObservableObject
    {
        public int Number { get; }
        public string Label { get; }
        public ICommand Command { get; }

        [ObservableProperty]
        private MigrationStepStatus status;

        /// <summary>Last error / warning message, surfaced as tooltip on the cell.</summary>
        [ObservableProperty]
        private string? lastMessage;

        public MigrationStep(int number, string label, ICommand command)
        {
            Number = number;
            Label = label;
            Command = command;
            Status = MigrationStepStatus.Pending;
        }

        /// <summary>Reset the cell to <see cref="MigrationStepStatus.Pending"/> and clear any prior message.</summary>
        public void Reset()
        {
            Status = MigrationStepStatus.Pending;
            LastMessage = null;
        }
    }
}
