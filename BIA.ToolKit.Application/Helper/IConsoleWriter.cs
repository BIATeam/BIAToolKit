namespace BIA.ToolKit.Application.Helper
{
    using System;

    public interface IConsoleWriter
    {
        void AddMessageLine(string message, string color = null, bool refreshimediate = true);
        void Clear();
        void CopyToClipboard();

        /// <summary>
        /// Opens a scope tagging subsequent <see cref="AddMessageLine"/> calls
        /// with the given step number. The scope flows through <c>await</c> and
        /// <c>Task.Run</c> via <see cref="System.Threading.AsyncLocal{T}"/>.
        /// Dispose to leave the scope; nested or concurrent scopes are not
        /// expected and behavior is undefined in that case.
        /// </summary>
        /// <param name="number">1-based step number (1..6).</param>
        /// <param name="label">Short label used for diagnostics / tooltip.</param>
        /// <returns>Disposable handle. Dispose to close the scope.</returns>
        IDisposable BeginStep(int number, string label);

        /// <summary>
        /// Re-renders all displayed messages, applying the current per-step
        /// color (whose value lives on the consumer side, e.g. ViewModel).
        /// Called when a step finishes (Running → Done/Warning/Failed) so the
        /// step's batch of stripes update from "Running" blue to the final color.
        /// </summary>
        /// <param name="stepStatusProvider">
        /// Resolver from step number to its current status. Implementations
        /// can call this for each stored message to choose the stripe color.
        /// </param>
        void RefreshStepColors(Func<int, BIA.ToolKit.Domain.Model.MigrationStepStatus?> stepStatusProvider);
    }
}
