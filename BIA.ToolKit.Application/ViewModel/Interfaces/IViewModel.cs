namespace BIA.ToolKit.Application.ViewModel.Interfaces
{
    /// <summary>
    /// Defines lifecycle methods for ViewModels.
    /// </summary>
    public interface IViewModel
    {
        /// <summary>
        /// Called when the associated view is loaded.
        /// Subscribe to messages and initialize state here.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called when the associated view is unloaded.
        /// Unsubscribe from messages and release resources here.
        /// </summary>
        void Cleanup();
    }
}
