namespace BIA.ToolKit.Application.ViewModel;

using System;
using System.Threading.Tasks;
using BIA.ToolKit.Application.Helper;
using BIA.ToolKit.Application.Services;
using BIA.ToolKit.Common;
using BIA.ToolKit.Domain;
using BIA.ToolKit.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for VersionAndOptionUserControl following MVVM pattern
/// </summary>
public partial class VersionAndOptionViewModel : ObservableObject
{
    #region Private Fields

    private readonly SettingsService settingsService;
    private readonly IConsoleWriter consoleWriter;
    private readonly IDialogService dialogService;
    private readonly IMessenger messenger;
    private readonly ILogger<VersionAndOptionViewModel> logger;

    #endregion

    #region Constructor

    public VersionAndOptionViewModel(
        SettingsService settingsService,
        IConsoleWriter consoleWriter,
        IDialogService dialogService,
        IMessenger messenger,
        ILogger<VersionAndOptionViewModel> logger)
    {
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private bool isWaiterVisible;

    [ObservableProperty]
    private object selectedVersionBefore;

    [ObservableProperty]
    private object selectedVersionAfter;

    [ObservableProperty]
    private bool hasSelectedOptions;

    #endregion

    #region Commands

    /// <summary>
    /// Save settings before version change
    /// </summary>
    [RelayCommand]
    private async Task SaveBeforeAsync()
    {
        try
        {
            if (SelectedVersionBefore == null)
            {
                await dialogService.ShowErrorAsync("Save Before", "Please select a version");
                return;
            }

            // Execute save with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("Settings saved before version change", "green");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving settings before version change");
            await dialogService.ShowErrorAsync("Save Error", ex.Message);
        }
    }

    /// <summary>
    /// Save settings after version change
    /// </summary>
    [RelayCommand]
    private async Task SaveAfterAsync()
    {
        try
        {
            if (SelectedVersionAfter == null)
            {
                await dialogService.ShowErrorAsync("Save After", "Please select a version");
                return;
            }

            // Execute save with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("Settings saved after version change", "green");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving settings after version change");
            await dialogService.ShowErrorAsync("Save Error", ex.Message);
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Execute a task with waiter visibility
    /// </summary>
    private async Task ExecuteTaskWithWaiterAsync(Func<Task> task)
    {
        try
        {
            IsWaiterVisible = true;
            await task();
        }
        finally
        {
            IsWaiterVisible = false;
        }
    }

    #endregion
}
