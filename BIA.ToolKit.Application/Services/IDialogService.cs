namespace BIA.ToolKit.Application.Services
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service interface for managing dialog operations.
    /// Provides type-safe dialog result handling with error support.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a dialog and returns a typed result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="dialogName">Unique identifier for the dialog.</param>
        /// <param name="viewModel">ViewModel for the dialog.</param>
        /// <returns>Dialog result containing success status and typed result.</returns>
        Task<DialogResult<T>> ShowDialogAsync<T>(string dialogName, object viewModel)
            where T : class;

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Dialog message.</param>
        /// <returns>Dialog result (Yes/No/Cancel).</returns>
        Task<DialogResultEnum> ShowConfirmAsync(string title, string message);

        /// <summary>
        /// Shows an information dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Dialog message.</param>
        Task ShowInfoAsync(string title, string message);

        /// <summary>
        /// Shows an error dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Dialog message.</param>
        Task ShowErrorAsync(string title, string message);
    }

    /// <summary>
    /// Generic dialog result container with type safety.
    /// </summary>
    public class DialogResult<T> where T : class
    {
        /// <summary>
        /// Gets or sets whether the dialog completed successfully.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the dialog result.
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// Gets or sets any error message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Dialog result enumeration (renamed to avoid conflict with System.Windows.MessageBoxResult).
    /// </summary>
    public enum DialogResultEnum
    {
        Yes,
        No,
        Cancel
    }
}
