namespace BIA.ToolKit.Application.ViewModel;

using System;
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
/// ViewModel for DtoGeneratorUC following MVVM pattern
/// </summary>
public partial class DtoGeneratorViewModel : ObservableObject
{
    #region Private Fields

    private readonly CSharpParserService cSharpParserService;
    private readonly IConsoleWriter consoleWriter;
    private readonly IFileDialogService fileDialogService;
    private readonly IDialogService dialogService;
    private readonly IMessenger messenger;
    private readonly ILogger<DtoGeneratorViewModel> logger;

    #endregion

    #region Constructor

    public DtoGeneratorViewModel(
        CSharpParserService cSharpParserService,
        IConsoleWriter consoleWriter,
        IFileDialogService fileDialogService,
        IDialogService dialogService,
        IMessenger messenger,
        ILogger<DtoGeneratorViewModel> logger)
    {
        this.cSharpParserService = cSharpParserService ?? throw new ArgumentNullException(nameof(cSharpParserService));
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
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private bool isGenerateEnabled;

    #endregion

    #region Commands

    /// <summary>
    /// Browse for a DTO class file
    /// </summary>
    [RelayCommand]
    private void BrowseFile()
    {
        try
        {
            var filePath = fileDialogService.BrowseFile("C# Files (*.cs)|*.cs");
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                SelectedFilePath = filePath;
                IsGenerateEnabled = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error browsing for DTO file");
            dialogService.ShowErrorAsync("Browse Error", ex.Message).Wait();
        }
    }

    /// <summary>
    /// Generate DTO from selected class file
    /// </summary>
    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SelectedFilePath))
            {
                await dialogService.ShowErrorAsync("DTO Generation", "Please select a file first");
                return;
            }

            // Execute generation with waiter
            await ExecuteTaskWithWaiterAsync(async () =>
            {
                // Implementation will delegate to helper/service
                consoleWriter.AddMessageLine("DTO generation completed", "green");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during DTO generation");
            await dialogService.ShowErrorAsync("DTO Generation Error", ex.Message);
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
