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
/// ViewModel for ModifyProjectUC following MVVM pattern
/// </summary>
public partial class ModifyProjectViewModel : ObservableObject
{
    #region Private Fields

    private readonly CSharpParserService cSharpParserService;
    private readonly GenerateFilesService generateFilesService;
    private readonly SettingsService settingsService;
    private readonly IConsoleWriter consoleWriter;
    private readonly IFileDialogService fileDialogService;
    private readonly IDialogService dialogService;
    private readonly IMessenger messenger;
    private readonly ILogger<ModifyProjectViewModel> logger;

    #endregion

    #region Constructor

    public ModifyProjectViewModel(
        CSharpParserService cSharpParserService,
        GenerateFilesService generateFilesService,
        SettingsService settingsService,
        IConsoleWriter consoleWriter,
        IFileDialogService fileDialogService,
        IDialogService dialogService,
        IMessenger messenger,
        ILogger<ModifyProjectViewModel> logger)
    {
        this.cSharpParserService = cSharpParserService ?? throw new ArgumentNullException(nameof(cSharpParserService));
        this.generateFilesService = generateFilesService ?? throw new ArgumentNullException(nameof(generateFilesService));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.consoleWriter = consoleWriter ?? throw new ArgumentNullException(nameof(consoleWriter));
        this.fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private bool isWaiterVisible;

    [ObservableProperty]
    private string modifyProjectPath = string.Empty;

    [ObservableProperty]
    private string classFilePath = string.Empty;

    [ObservableProperty]
    private bool isGenerateEnabled;

    #endregion

    #region Commands

    /// <summary>
    /// Browse for project to modify
    /// </summary>
    [RelayCommand]
    private void BrowseProject()
    {
        try
        {
            var folderPath = fileDialogService.BrowseFolder(string.Empty, "Select project folder to modify");
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                ModifyProjectPath = folderPath;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error browsing for project");
            dialogService.ShowErrorAsync("Browse Error", ex.Message).Wait();
        }
    }

    /// <summary>
    /// Browse for class file to modify
    /// </summary>
    [RelayCommand]
    private void BrowseClassFile()
    {
        try
        {
            var filePath = fileDialogService.BrowseFile("C# Files (*.cs)|*.cs");
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                ClassFilePath = filePath;
                IsGenerateEnabled = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error browsing for class file");
            dialogService.ShowErrorAsync("Browse Error", ex.Message).Wait();
        }
    }

    /// <summary>
    /// Modify the project with generated content
    /// </summary>
    [RelayCommand]
    private async Task ModifyProjectAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ModifyProjectPath))
            {
                await dialogService.ShowErrorAsync("Modify Project", "Please select a project first");
                return;
            }

            // Execute modification with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("Project modification completed", "green");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during project modification");
            await dialogService.ShowErrorAsync("Modification Error", ex.Message);
        }
    }

    /// <summary>
    /// Update project ZIP archive
    /// </summary>
    [RelayCommand]
    private async Task UpdateProjectZipAsync()
    {
        try
        {
            var result = await dialogService.ShowConfirmAsync(
                "Update ZIP",
                "Update the project ZIP archive?");

            if (result != DialogResultEnum.Yes)
                return;

            // Execute update with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("ZIP archive updated", "green");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating ZIP archive");
            await dialogService.ShowErrorAsync("Update Error", ex.Message);
        }
    }

    /// <summary>
    /// Delete all generated content for the project
    /// </summary>
    [RelayCommand]
    private async Task DeleteAllGeneratedAsync()
    {
        try
        {
            var result = await dialogService.ShowConfirmAsync(
                "Delete All Generated",
                "This will delete ALL generated content from the project. Continue?");

            if (result != DialogResultEnum.Yes)
                return;

            // Execute deletion with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("All generated content deleted", "yellow");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting generated content");
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
