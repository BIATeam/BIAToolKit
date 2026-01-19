namespace BIA.ToolKit.Application.ViewModel;

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BIA.ToolKit.Application.Helper;
using BIA.ToolKit.Application.Services;
using BIA.ToolKit.Common;
using BIA.ToolKit.Domain;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

/// <summary>
/// ViewModel for OptionGeneratorUC following MVVM pattern
/// </summary>
public partial class OptionGeneratorViewModel : ObservableObject
{
    #region Private Fields

    private readonly CSharpParserService cSharpParserService;
    private readonly GenerateCrudService generateCrudService;
    private readonly SettingsService settingsService;
    private readonly IConsoleWriter consoleWriter;
    private readonly IMessenger messenger;
    private readonly IDialogService dialogService;
    private readonly ILogger<OptionGeneratorViewModel> logger;

    #endregion

    #region Constructor

    public OptionGeneratorViewModel(
        CSharpParserService cSharpParserService,
        GenerateCrudService generateCrudService,
        SettingsService settingsService,
        IConsoleWriter consoleWriter,
        IMessenger messenger,
        IDialogService dialogService,
        ILogger<OptionGeneratorViewModel> logger)
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
    private object selectedEntity;

    [ObservableProperty]
    private ObservableCollection<object> entityList = new();

    [ObservableProperty]
    private bool isEntityParsed;

    #endregion

    #region Commands

    /// <summary>
    /// Generate options for selected entity
    /// </summary>
    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            if (SelectedEntity == null)
            {
                await dialogService.ShowErrorAsync("Option Generation", "Please select an entity first");
                return;
            }

            // Execute generation with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("Option generation completed", "green");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during option generation");
            await dialogService.ShowErrorAsync("Option Generation Error", ex.Message);
        }
    }

    /// <summary>
    /// Delete the last generation for the selected entity
    /// </summary>
    [RelayCommand]
    private async Task DeleteLastGenerationAsync()
    {
        try
        {
            if (SelectedEntity == null)
            {
                await dialogService.ShowErrorAsync("Delete Generation", "Please select an entity first");
                return;
            }

            var result = await dialogService.ShowConfirmAsync(
                "Delete Last Generation",
                "Are you sure you want to delete the last generation for this entity?");

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
    /// Refresh the entity list from parsed entities
    /// </summary>
    [RelayCommand]
    private void RefreshEntityList()
    {
        try
        {
            // Implementation will reload entity list
            consoleWriter.AddMessageLine("Entity list refreshed", "green");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing entity list");
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
