namespace BIA.ToolKit.Application.ViewModel;

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BIA.ToolKit.Application.Helper;
using BIA.ToolKit.Application.Messages;
using BIA.ToolKit.Application.Services;
using BIA.ToolKit.Common;
using BIA.ToolKit.Domain;
using BIA.ToolKit.Domain.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for CRUDGeneratorUC following MVVM pattern
/// </summary>
public partial class CRUDGeneratorViewModel : ObservableObject
{
    #region Private Fields

    private readonly CSharpParserService cSharpParserService;
    private readonly GenerateCrudService generateCrudService;
    private readonly SettingsService settingsService;
    private readonly IConsoleWriter consoleWriter;
    private readonly IMessenger messenger;
    private readonly IDialogService dialogService;
    private readonly ILogger<CRUDGeneratorViewModel> logger;

    #endregion

    #region Constructor

    public CRUDGeneratorViewModel(
        CSharpParserService cSharpParserService,
        GenerateCrudService generateCrudService,
        SettingsService settingsService,
        IConsoleWriter consoleWriter,
        IMessenger messenger,
        IDialogService dialogService,
        ILogger<CRUDGeneratorViewModel> logger)
    {
        this.cSharpParserService = cSharpParserService ?? throw new ArgumentNullException(nameof(cSharpParserService));
        this.generateCrudService = generateCrudService ?? throw new ArgumentNullException(nameof(generateCrudService));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
        this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private bool isWaiterVisible;

    [ObservableProperty]
    private object selectedDto;

    [ObservableProperty]
    private ObservableCollection<object> dtoList = new();

    [ObservableProperty]
    private bool isEntityParsed;

    #endregion

    #region Commands

    /// <summary>
    /// Generate CRUD operations for selected DTO
    /// </summary>
    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            if (SelectedDto == null)
            {
                await dialogService.ShowErrorAsync("CRUD Generation", "Please select a DTO first");
                return;
            }

            // Execute generation with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("CRUD generation completed", "green");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during CRUD generation");
            await dialogService.ShowErrorAsync("CRUD Generation Error", ex.Message);
        }
    }

    /// <summary>
    /// Delete the last generation for the selected DTO
    /// </summary>
    [RelayCommand]
    private async Task DeleteLastGenerationAsync()
    {
        try
        {
            if (SelectedDto == null)
            {
                await dialogService.ShowErrorAsync("Delete Generation", "Please select a DTO first");
                return;
            }

            var result = await dialogService.ShowConfirmAsync(
                "Delete Last Generation",
                "Are you sure you want to delete the last generation for this DTO?");

            if (result != DialogResultEnum.Yes)
                return;

            // Execute deletion with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("Last generation deleted", "yellow");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during deletion");
            await dialogService.ShowErrorAsync("Deletion Error", ex.Message);
        }
    }

    /// <summary>
    /// Refresh the DTO list from parsed entities
    /// </summary>
    [RelayCommand]
    private void RefreshDtoList()
    {
        try
        {
            // Implementation will reload DTO list
            consoleWriter.AddMessageLine("DTO list refreshed", "green");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing DTO list");
        }
    }

    /// <summary>
    /// Delete all BIA Toolkit annotations from the current project
    /// </summary>
    [RelayCommand]
    private async Task DeleteBIAToolkitAnnotationsAsync()
    {
        try
        {
            var result = await dialogService.ShowConfirmAsync(
                "Delete BIA Toolkit Annotations",
                "This will remove all BIA Toolkit annotations from your DTOs. Continue?");

            if (result != DialogResultEnum.Yes)
                return;

            // Execute deletion with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("BIA Toolkit annotations deleted", "yellow");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting BIA Toolkit annotations");
            await dialogService.ShowErrorAsync("Deletion Error", ex.Message);
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
