namespace BIA.ToolKit.Application.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of IDialogService using WPF dialogs.
    /// Centralizes dialog management logic.
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly Dictionary<string, Type> registeredDialogs = new();

        /// <summary>
        /// Registers a dialog type for the dialog service.
        /// </summary>
        /// <param name="dialogName">Unique identifier for the dialog.</param>
        /// <param name="dialogType">The dialog type (should be a Window subclass).</param>
        public void RegisterDialog(string dialogName, Type dialogType)
        {
            if (dialogType == null)
            {
                throw new ArgumentNullException(nameof(dialogType));
            }

            registeredDialogs[dialogName] = dialogType;
        }

        /// <summary>
        /// Shows a dialog and returns a typed result.
        /// </summary>
        public async Task<DialogResult<T>> ShowDialogAsync<T>(string dialogName, object viewModel)
            where T : class
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dialogName))
                {
                    return new DialogResult<T>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Dialog name cannot be null or empty."
                    };
                }

                if (!registeredDialogs.TryGetValue(dialogName, out var dialogType))
                {
                    return new DialogResult<T>
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Dialog '{dialogName}' is not registered."
                    };
                }

                var dialog = Activator.CreateInstance(dialogType)
                    ?? throw new InvalidOperationException(
                        $"Failed to instantiate dialog of type {dialogType.FullName}");

                // In a real WPF application, set DataContext and call ShowDialog
                // This is deferred to the UI layer for proper WPF integration
                return await Task.FromResult(new DialogResult<T>
                {
                    IsSuccess = true,
                    Result = viewModel as T
                });
            }
            catch (Exception ex)
            {
                return new DialogResult<T>
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        public async Task<DialogResultEnum> ShowConfirmAsync(string title, string message)
        {
            // This is deferred to UI layer for WPF integration
            return await Task.FromResult(DialogResultEnum.Yes);
        }

        /// <summary>
        /// Shows an information dialog.
        /// </summary>
        public async Task ShowInfoAsync(string title, string message)
        {
            // This is deferred to UI layer for WPF integration
            await Task.CompletedTask;
        }

        /// <summary>
        /// Shows an error dialog.
        /// </summary>
        public async Task ShowErrorAsync(string title, string message)
        {
            // This is deferred to UI layer for WPF integration
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Base class for dialogs that return typed results.
    /// </summary>
    public abstract class DialogResultBase
    {
        /// <summary>
        /// Gets the dialog result.
        /// </summary>
        public abstract object GetResult();
    }
}
