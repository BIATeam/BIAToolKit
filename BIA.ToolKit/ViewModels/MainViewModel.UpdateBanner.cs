namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Messages;
    using BIA.ToolKit.Domain.Model;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using CommunityToolkit.Mvvm.Messaging;

    public partial class MainViewModel
    {
        // --- Update banner (V2.15.0) ---

        [ObservableProperty]
        private UpdateBannerState bannerState = UpdateBannerState.NoSource;

        [ObservableProperty]
        private string latestVersion;

        [ObservableProperty]
        private DateTime? lastCheckedAt;

        [ObservableProperty]
        private string lastError;

        public string CurrentVersion => applicationVersion?.ToString() ?? "unknown";

        /// <summary>
        /// Single-item collection wrapping <see cref="ToolkitRepository"/>
        /// so the Config-tab Toolkit Source section can bind to the same
        /// RepositorySectionUC.Items API as the multi-source sections.
        /// </summary>
        public System.Collections.Generic.IEnumerable<RepositoryViewModel> ToolkitRepositorySingleton
            => ToolkitRepository is null ? [] : new[] { ToolkitRepository };

        partial void OnToolkitRepositoryChanged(RepositoryViewModel value)
        {
            OnPropertyChanged(nameof(ToolkitRepositorySingleton));
        }

        [RelayCommand]
        private async Task CheckForUpdatesBanner()
        {
            BannerState = UpdateBannerState.Checking;
            LastError = null;

            try
            {
                if (settingsService.Settings.ToolkitRepository is null
                    || !settingsService.Settings.ToolkitRepository.UseRepository)
                {
                    BannerState = UpdateBannerState.NoSource;
                    return;
                }

                await updateService.CheckForUpdatesAsync(CancellationToken.None);
                LastCheckedAt = DateTime.Now;

                if (updateService.HasNewVersion)
                {
                    LatestVersion = updateService.NewVersion?.ToString();
                    BannerState = UpdateBannerState.UpdateAvailable;
                }
                else
                {
                    LatestVersion = CurrentVersion;
                    BannerState = UpdateBannerState.UpToDate;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                BannerState = UpdateBannerState.Failed;
            }
        }

        [RelayCommand]
        private void InstallUpdate()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                try
                {
                    await updateService.DownloadUpdateAsync(ct);
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    BannerState = UpdateBannerState.Failed;
                }
            }));
        }
    }
}
