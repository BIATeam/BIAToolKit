namespace BIA.ToolKit.Application.ViewModel
{
    using System;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class MainViewModel
    {
        [ObservableProperty]
        private bool updateAvailable;

        [ObservableProperty]
        private bool isCheckUpdateEnabled = true;

        private async Task OnNewVersionAvailable()
        {
            UpdateAvailable = true;
            IsCheckUpdateEnabled = false;
            await PromptAndDownloadUpdate();
        }

        [RelayCommand]
        private async Task CheckForUpdate()
        {
            await ExecuteWithBusyAsync(async (ct) => await updateService.CheckForUpdatesAsync(ct));
        }

        [RelayCommand]
        private async Task PromptAndDownloadUpdate()
        {
            try
            {
                bool confirmed = dialogService.Confirm(
                    $"A new version ({updateService.NewVersion}) of BIAToolKit is available.\nInstall now?",
                    "Update available");

                if (confirmed)
                {
                    await ExecuteWithBusyAsync(async (ct) => await updateService.DownloadUpdateAsync(ct));
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowMessage($"Update failure : {ex.Message}", "Update failure");
            }
        }
    }
}
