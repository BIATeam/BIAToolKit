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

        public string CurrentVersion =>
            applicationVersion is null
                ? "unknown"
                : $"{applicationVersion.Major}.{applicationVersion.Minor}.{applicationVersion.Build}";

        /// <summary>
        /// Computes the banner state at startup (or whenever settings reload)
        /// without firing an actual network check. NoSource when no Toolkit
        /// repository is configured or it's disabled; otherwise UpToDate as
        /// the initial assumption — user can click Check now to verify.
        /// </summary>
        public void RefreshBannerStateFromSettings()
        {
            var repo = settingsService.Settings.ToolkitRepository;
            BannerState = (repo is null || !repo.UseRepository)
                ? UpdateBannerState.NoSource
                : UpdateBannerState.UpToDate;
        }

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
