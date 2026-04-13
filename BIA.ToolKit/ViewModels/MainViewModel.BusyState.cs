namespace BIA.ToolKit.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BIA.ToolKit.Application.Helper;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using BIA.ToolKit.Domain;
    using BIA.ToolKit.Domain.Model;
    using BIA.ToolKit.Domain.Settings;

    public partial class MainViewModel
    {
        // --- IsBusy / Waiter ---

        [ObservableProperty]
        private bool isBusy;

        private CancellationTokenSource currentTokenSource;

        public async Task ExecuteWithBusyAsync(Func<CancellationToken, Task> task)
        {
            await semaphore.WaitAsync();
            currentTokenSource = new CancellationTokenSource();
            try
            {
                IsBusy = true;
                await task(currentTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                consoleWriter.AddMessageLine("Operation cancelled by user.", "Yellow");
            }
            catch (Exception ex)
            {
                // Last-resort catch: without this, an unhandled exception in an async void Receive()
                // handler crashes the WPF Dispatcher and terminates the app before any log is written.
                DiagLog.Write($"ExecuteWithBusyAsync: UNHANDLED {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
                try { consoleWriter.AddMessageLine($"Unhandled error: {ex.Message}", "Red"); } catch { }
            }
            finally
            {
                IsBusy = false;
                currentTokenSource?.Dispose();
                currentTokenSource = null;
                semaphore.Release();
            }
        }

        [RelayCommand]
        private void StopAction()
        {
            currentTokenSource?.Cancel();
        }

        // --- Initialization ---

        /// <summary>
        /// Called by the host (App/MainWindow) after the window is shown.
        /// Loads persisted settings, fetches repository releases, then broadcasts
        /// the initial SettingsUpdatedMessage.
        /// </summary>
        public bool IsInitialized { get; private set; }

        public async Task InitAsync()
        {
            await ExecuteWithBusyAsync(async (ct) =>
            {
                BIATKSettings settings = settingsService.Load();
                await GetReleasesData(settings, ct: ct);
                settingsService.NotifyInitialized();

                updateService.SetAppVersion(applicationVersion);

                if (settings.AutoUpdate)
                {
                    await updateService.CheckForUpdatesAsync(ct);
                }

                await Task.Run(() => cSharpParserService.RegisterMSBuild(consoleWriter), ct);
            });

            IsInitialized = true;
        }

        public async Task GetReleasesData(BIATKSettings settings, bool syncBefore = false, CancellationToken ct = default)
        {
            IEnumerable<Task> fillReleasesTasks = settings.TemplateRepositories
                .Concat(settings.CompanyFilesRepositories)
                .Where(r => r.UseRepository)
                .Select(async (r) =>
                {
                    if (syncBefore)
                    {
                        try
                        {
                            if (r is IRepositoryGit repoGit)
                            {
                                consoleWriter.AddMessageLine($"Synchronizing repository {r.Name}...", "pink");
                                await gitService.Synchronize(repoGit, ct);
                                consoleWriter.AddMessageLine($"Synchronized successfully of repository {r.Name}", "green");
                            }
                        }
                        catch (Exception ex)
                        {
                            consoleWriter.AddMessageLine($"Error while synchronizing repository {r.Name} : {ex.Message}", "red");
                        }
                    }

                    try
                    {
                        consoleWriter.AddMessageLine($"Getting releases data for repository {r.Name}...", "pink");
                        await r.FillReleasesAsync(ct);
                        consoleWriter.AddMessageLine($"Releases data got successfully for repository {r.Name}", "green");
                        if (r.UseDownloadedReleases)
                        {
                            consoleWriter.AddMessageLine($"WARNING: Releases data got from downloaded releases for repository {r.Name}", "orange");
                        }
                    }
                    catch (Exception ex)
                    {
                        consoleWriter.AddMessageLine($"Error while getting releases data for repository {r.Name} : {ex.Message}", "red");
                    }
                });
            await Task.WhenAll(fillReleasesTasks);
        }
    }
}
