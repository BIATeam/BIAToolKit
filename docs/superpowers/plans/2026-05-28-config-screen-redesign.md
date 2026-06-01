# Config Screen Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the MainWindow Config tab as a single-column layout with a state-driven UpdateBanner, three parallel sections (Toolkit Source / Templates / Company Files) sharing a unified RepositoryCardUC, descriptive headers, and Safran-default empty states (one-click bootstrap), shipped as V2.15.0.

**Architecture:** Bottom-up — start with `UpdateBannerState` and `RepositorySyncStatus` enums in Domain, add Safran defaults in Common, build the converters (banner color / glyph / action label + sync status color + repository type badge), extend `RepositoryViewModel` with sync status tracking (wrap the existing `GetReleasesDataCommand` with state transitions and version count + latest), extend `MainViewModel` with banner state + commands (`CheckForUpdatesCommand`, `InstallUpdateCommand`, `AddDefaultXxxRepositoryCommand`), then build the three new user controls (`UpdateBannerUC`, `RepositoryCardUC`, `RepositorySectionUC`), rewire the Config tab content in `MainWindow.xaml`, and add inline helper text to the existing `RepositoryFormUC` dialog. Bump version to V2.15.0.

**Tech Stack:** WPF .NET 10 · MaterialDesignThemes 5.3 · CommunityToolkit.Mvvm 8.3 (`ObservableObject`, `[ObservableProperty]`, `IAsyncRelayCommand`) · existing `UpdateService` and `RepositoryService`.

**Reference spec:** [`docs/superpowers/specs/2026-05-28-config-screen-redesign-design.md`](../specs/2026-05-28-config-screen-redesign-design.md)

**Branch:** `feature/fixes-V2.15.0` (already created from main, spec doc already committed at `6155c2d`)

**Testing note:** This project has no unit-test infrastructure for ViewModels / Helpers (only template generation tests at `BIA.ToolKit.Test.Templates`). Verification per task = `dotnet build` (compile gate) plus, at the end, the spec's manual UI test plan. We do not introduce a new test project for this scope.

**Gitflow:** Do NOT run the gitflow finish (merge to main / develop, tags, push) — the user authorizes that step explicitly after manual UI validation.

---

### Task 1: Add `UpdateBannerState` enum

**Files:**
- Create: `BIA.ToolKit.Domain/Model/UpdateBannerState.cs`

- [ ] **Step 1: Create the enum file**

```csharp
namespace BIA.ToolKit.Domain.Model
{
    /// <summary>
    /// Mutually exclusive UI states of the Config-tab Update banner. The
    /// banner color, glyph, and action label all derive from this value
    /// through converters in BIA.ToolKit/Converters/.
    /// </summary>
    public enum UpdateBannerState
    {
        UpToDate,
        UpdateAvailable,
        Checking,
        NoSource,
        Failed,
    }
}
```

- [ ] **Step 2: Build the Domain project**

Run: `dotnet build BIA.ToolKit.Domain/BIA.ToolKit.Domain.csproj --nologo -v:m 2>&1 | tail -3`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit.Domain/Model/UpdateBannerState.cs
git commit -m "feat(domain): add UpdateBannerState enum for Config-tab update banner

Five mutually exclusive states (UpToDate / UpdateAvailable / Checking
/ NoSource / Failed) consumed by the new UpdateBannerUC and resolved
through converters into color, glyph, and action label.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Add `RepositorySyncStatus` enum

**Files:**
- Create: `BIA.ToolKit.Domain/Model/RepositorySyncStatus.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace BIA.ToolKit.Domain.Model
{
    /// <summary>
    /// Per-card sync state of a repository, used by RepositoryCardUC to
    /// pick the status row content (✓ N versions / ⏳ Fetching / ⚠ Failed)
    /// and the card's border color.
    /// </summary>
    public enum RepositorySyncStatus
    {
        Idle,
        Syncing,
        Failed,
    }
}
```

- [ ] **Step 2: Build the Domain project**

Run: `dotnet build BIA.ToolKit.Domain/BIA.ToolKit.Domain.csproj --nologo -v:m 2>&1 | tail -3`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit.Domain/Model/RepositorySyncStatus.cs
git commit -m "feat(domain): add RepositorySyncStatus enum for repo card state

Three states (Idle / Syncing / Failed) consumed by the new RepositoryCardUC
status row and border color converter.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Add `SafranRepositoryDefaults` constants

**Files:**
- Create: `BIA.ToolKit.Common/SafranRepositoryDefaults.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace BIA.ToolKit.Common
{
    /// <summary>
    /// Hardcoded Safran-internal default sources, surfaced by the Config
    /// tab empty states so users can bootstrap each repository category
    /// in one click without typing URLs or paths.
    ///
    /// Tune these constants if forking the ToolKit for another company.
    /// </summary>
    public static class SafranRepositoryDefaults
    {
        public const string ToolkitUpdateSourceName = "BIAToolkit Shared";
        public const string ToolkitUpdateSourcePath = @"\\share.bia.safran\BIAToolKit\Releases\BiaToolkit";

        public const string TemplatesSourceName = "BIATemplate Shared";
        public const string TemplatesSourcePath = @"\\share.bia.safran\BIAToolKit\Releases\BiaTemplate";

        public const string CompanyFilesSourceName = "BIACompanyFiles Azure";
        public const string CompanyFilesSourceUrl = "https://azure.devops.safran/SafranElectricalAndPower/Digital%20Manufacturing/_git/BIACompanyFiles";
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build BIA.ToolKit.Common/BIA.ToolKit.Common.csproj --nologo -v:m 2>&1 | tail -3`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit.Common/SafranRepositoryDefaults.cs
git commit -m "feat(common): add SafranRepositoryDefaults for empty-state bootstrap

Three hardcoded paths/URLs (Toolkit shared, Templates shared, Company
Files Azure DevOps) used by the new Config-tab empty states to offer
one-click setup. Trailing comment notes that the constants are
Safran-specific and would need to be tuned in any fork.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Add `UpdateBannerStateToColorConverter`

**Files:**
- Create: `BIA.ToolKit/Converters/UpdateBannerStateToColorConverter.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps an <see cref="UpdateBannerState"/> to a <see cref="Brush"/>.
    /// Resolves through MaterialDesign theme resources when available so
    /// dark/light theme switching propagates automatically; falls back to
    /// fixed RGB values for design-time preview.
    /// </summary>
    public sealed class UpdateBannerStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UpdateBannerState state)
                return Brushes.Gray;

            string resourceKey = state switch
            {
                UpdateBannerState.UpToDate => "MaterialDesign.Brush.ValidationSuccess",
                UpdateBannerState.UpdateAvailable => "MaterialDesign.Brush.ValidationWarning",
                UpdateBannerState.Checking => "MaterialDesign.Brush.Primary",
                UpdateBannerState.NoSource => "MaterialDesign.Brush.Foreground",
                UpdateBannerState.Failed => "MaterialDesign.Brush.ValidationError",
                _ => "MaterialDesign.Brush.Foreground",
            };

            object resource = Application.Current?.TryFindResource(resourceKey);
            if (resource is Brush brush)
                return brush;

            return state switch
            {
                UpdateBannerState.UpToDate => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                UpdateBannerState.UpdateAvailable => new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)),
                UpdateBannerState.Checking => new SolidColorBrush(Color.FromRgb(0x42, 0xA5, 0xF5)),
                UpdateBannerState.NoSource => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
                UpdateBannerState.Failed => new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50)),
                _ => Brushes.Gray,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -3`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/Converters/UpdateBannerStateToColorConverter.cs
git commit -m "feat(config): add UpdateBannerStateToColorConverter

Resolves through MaterialDesign theme brushes (ValidationSuccess /
ValidationWarning / Primary / Foreground / ValidationError) with hard
RGB fallbacks for design-time preview, matching the pattern used by
MigrationStepStatusToColorConverter in V2.14.0.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Add `UpdateBannerStateToGlyphConverter`

**Files:**
- Create: `BIA.ToolKit/Converters/UpdateBannerStateToGlyphConverter.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps an <see cref="UpdateBannerState"/> to its banner glyph.
    /// </summary>
    public sealed class UpdateBannerStateToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UpdateBannerState state)
                return string.Empty;

            return state switch
            {
                UpdateBannerState.UpToDate => "✓",
                UpdateBannerState.UpdateAvailable => "⚠",
                UpdateBannerState.Checking => "⏳",
                UpdateBannerState.NoSource => "🔧",
                UpdateBannerState.Failed => "✗",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -3`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/Converters/UpdateBannerStateToGlyphConverter.cs
git commit -m "feat(config): add UpdateBannerStateToGlyphConverter

Maps the banner state to its display glyph (check / warning /
hourglass / wrench / cross).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Add `RepositorySyncStatusToColorConverter`

**Files:**
- Create: `BIA.ToolKit/Converters/RepositorySyncStatusToColorConverter.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using BIA.ToolKit.Domain.Model;

    /// <summary>
    /// Maps a <see cref="RepositorySyncStatus"/> to the card border brush.
    /// Idle uses the card's default border resource; Syncing tints primary;
    /// Failed tints orange.
    /// </summary>
    public sealed class RepositorySyncStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not RepositorySyncStatus status)
                return Brushes.Transparent;

            string resourceKey = status switch
            {
                RepositorySyncStatus.Syncing => "MaterialDesign.Brush.Primary",
                RepositorySyncStatus.Failed => "MaterialDesign.Brush.ValidationWarning",
                _ => "MaterialDesign.Brush.Card.Border",
            };

            object resource = Application.Current?.TryFindResource(resourceKey);
            if (resource is Brush brush)
                return brush;

            return status switch
            {
                RepositorySyncStatus.Syncing => new SolidColorBrush(Color.FromRgb(0x42, 0xA5, 0xF5)),
                RepositorySyncStatus.Failed => new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)),
                _ => Brushes.Transparent,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -3`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/Converters/RepositorySyncStatusToColorConverter.cs
git commit -m "feat(config): add RepositorySyncStatusToColorConverter for card border

Card border tints primary while a repo is syncing, orange when sync
failed; transparent (uses default) when idle.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Add `RepositoryTypeToBadgeConverter`

**Files:**
- Create: `BIA.ToolKit/Converters/RepositoryTypeToBadgeConverter.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace BIA.ToolKit.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using BIA.ToolKit.Domain;

    /// <summary>
    /// Maps a <see cref="RepositoryType"/> to its short badge text
    /// (icon glyph + label) shown at the right of the card header.
    /// </summary>
    public sealed class RepositoryTypeToBadgeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not RepositoryType type)
                return string.Empty;

            return type switch
            {
                RepositoryType.Git => "🌐 Git",
                RepositoryType.Folder => "📁 Folder",
                _ => string.Empty,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -3`
Expected: `0 Erreur(s)`

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/Converters/RepositoryTypeToBadgeConverter.cs
git commit -m "feat(config): add RepositoryTypeToBadgeConverter

Short type label shown in the top-right of each RepositoryCardUC
(globe Git / folder Folder).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: Extend `RepositoryViewModel` with sync status and version info

**Files:**
- Modify: `BIA.ToolKit/ViewModels/RepositoryViewModel.cs`

- [ ] **Step 1: Add the new properties and wrap GetReleasesData**

Locate the `[RelayCommand] private void GetReleasesData()` method around line 184 of `BIA.ToolKit/ViewModels/RepositoryViewModel.cs`.

Add the following using directive at the top of the file in the existing using block:

```csharp
    using BIA.ToolKit.Domain.Model;
```

Add four `[ObservableProperty]` fields just below the `protected RepositoryViewModel(...)` constructor (before the `Receive` method):

```csharp
        // V2.15.0 — sync status surfaced on the new RepositoryCardUC.
        [ObservableProperty]
        private RepositorySyncStatus syncStatus = RepositorySyncStatus.Idle;

        [ObservableProperty]
        private int versionCount;

        [ObservableProperty]
        private string latestVersion;

        [ObservableProperty]
        private string lastSyncError;
```

Replace the body of the `GetReleasesData()` method (currently around line 184) with:

```csharp
        [RelayCommand]
        private void GetReleasesData()
        {
            WeakReferenceMessenger.Default.Send(new ExecuteActionWithWaiterMessage(async (ct) =>
            {
                SyncStatus = RepositorySyncStatus.Syncing;
                LastSyncError = null;
                try
                {
                    consoleWriter.AddMessageLine("Getting releases data...", "pink");
                    await repository.FillReleasesAsync(ct);
                    WeakReferenceMessenger.Default.Send(new RepositoryReleaseDataUpdatedMessage(this));
                    consoleWriter.AddMessageLine("Releases data got successfully", "green");
                    if (repository.UseDownloadedReleases)
                    {
                        consoleWriter.AddMessageLine($"WARNING: Releases data got from downloaded releases", "orange");
                    }
                    RefreshVersionInfo();
                    SyncStatus = RepositorySyncStatus.Idle;
                }
                catch (OperationCanceledException)
                {
                    LastSyncError = "Cancelled";
                    SyncStatus = RepositorySyncStatus.Failed;
                    throw;
                }
                catch (Exception ex)
                {
                    consoleWriter.AddMessageLine($"Error : {ex.Message}", "red");
                    LastSyncError = ex.Message;
                    SyncStatus = RepositorySyncStatus.Failed;
                }
            }));
        }

        /// <summary>
        /// Computes <see cref="VersionCount"/> and <see cref="LatestVersion"/> from
        /// the repository's current <c>Releases</c> collection. Releases whose name
        /// matches a leading <c>V</c> followed by a semver are preferred when picking
        /// the latest; otherwise the first release in the collection is used.
        /// </summary>
        private void RefreshVersionInfo()
        {
            var releases = repository.Releases ?? [];
            VersionCount = releases.Count;
            if (releases.Count == 0)
            {
                LatestVersion = null;
                return;
            }

            // Prefer semver-named releases when ordering; fall back to
            // alphabetical descending if no parseable name is present.
            var parsed = releases
                .Select(r => (r.Name, Parsed: TryParseVersion(r.Name)))
                .Where(x => x.Parsed != null)
                .OrderByDescending(x => x.Parsed)
                .ToList();

            LatestVersion = parsed.Count > 0
                ? parsed[0].Name
                : releases.OrderByDescending(r => r.Name).First().Name;
        }

        private static Version TryParseVersion(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string trimmed = name.StartsWith('V') || name.StartsWith('v') ? name[1..] : name;
            return Version.TryParse(trimmed, out Version v) ? v : null;
        }
```

- [ ] **Step 2: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`. If you see CS errors about missing namespace `Linq` for `OrderByDescending`/`Select`/`ToList`, the file already imports `System.Linq` (see the top of the file). If errors persist, re-check the using block.

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/ViewModels/RepositoryViewModel.cs
git commit -m "feat(config): expose sync status and version info on RepositoryViewModel

Wraps GetReleasesData with SyncStatus transitions (Idle → Syncing →
Idle/Failed) and computes VersionCount + LatestVersion from the repo
Releases collection (semver-aware ordering with alphabetical fallback).
Adds LastSyncError so the card tooltip can surface it.

Used by the new RepositoryCardUC in the V2.15.0 Config screen.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 9: Add update banner state and commands to `MainViewModel`

**Files:**
- Create: `BIA.ToolKit/ViewModels/MainViewModel.UpdateBanner.cs`

- [ ] **Step 1: Create the new partial class file**

```csharp
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
```

- [ ] **Step 2: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`. If `updateService` is not visible from this partial file, locate the field declaration on `MainViewModel.cs` (private readonly UpdateService updateService) and confirm it's `private readonly` and exists. The partial pattern shares fields across all files of the same partial class.

- [ ] **Step 3: Commit**

```bash
git add BIA.ToolKit/ViewModels/MainViewModel.UpdateBanner.cs
git commit -m "feat(config): add update banner state and commands to MainViewModel

New partial file MainViewModel.UpdateBanner.cs exposes:
- BannerState (UpdateBannerState enum) for the new UpdateBannerUC
- CurrentVersion / LatestVersion / LastCheckedAt / LastError
- CheckForUpdatesBannerCommand (transitions Checking →
  UpToDate/UpdateAvailable/NoSource/Failed)
- InstallUpdateCommand (wraps existing UpdateService.DownloadUpdateAsync)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: Add default-repository bootstrap commands to `MainViewModel`

**Files:**
- Modify: `BIA.ToolKit/ViewModels/MainViewModel.Repositories.cs`

- [ ] **Step 1: Add three new RelayCommands and a `using BIA.ToolKit.Common;` directive**

Add at the top of the using block in `BIA.ToolKit/ViewModels/MainViewModel.Repositories.cs`:

```csharp
    using BIA.ToolKit.Common;
```

Add the following three methods immediately AFTER `AddCompanyFilesRepository()` (around line 222):

```csharp
        /// <summary>
        /// One-click bootstrap of the Toolkit Update Source with the Safran
        /// default share, called from the Toolkit-Source empty state.
        /// Replaces the current ToolkitRepository if it is empty / invalid.
        /// </summary>
        [RelayCommand]
        private void UseDefaultToolkitRepository()
        {
            var folder = new RepositoryFolder(
                name: SafranRepositoryDefaults.ToolkitUpdateSourceName,
                path: SafranRepositoryDefaults.ToolkitUpdateSourcePath,
                releasesFolderRegexPattern: @"^V\d+\.\d+\.\d+(?:\.\d+)?$",
                useRepository: true);

            settingsService.SetToolkitRepository(folder);
            ToolkitRepository = new RepositoryFolderViewModel(folder, gitService, consoleWriter)
            {
                IsVisibleCompanyName = false,
                IsVisibleProjectName = false,
            };
            WeakReferenceMessenger.Default.Send(new RepositoriesUpdatedMessage());
        }

        /// <summary>
        /// One-click bootstrap of a Templates source with the Safran default
        /// share, called from the Templates empty state. Activates the new
        /// repository immediately.
        /// </summary>
        [RelayCommand]
        private void UseDefaultTemplatesRepository()
        {
            var folder = new RepositoryFolder(
                name: SafranRepositoryDefaults.TemplatesSourceName,
                path: SafranRepositoryDefaults.TemplatesSourcePath,
                releasesFolderRegexPattern: @"^V\d+\.\d+\.\d+(?:\.\d+)?$",
                useRepository: true);

            TemplateRepositories.Add(new RepositoryFolderViewModel(folder, gitService, consoleWriter));
            WeakReferenceMessenger.Default.Send(new RepositoriesUpdatedMessage());
        }

        /// <summary>
        /// One-click bootstrap of a Company Files source with the Safran Azure
        /// DevOps repo, called from the Company-Files empty state. Activates
        /// the new repository immediately.
        /// </summary>
        [RelayCommand]
        private void UseDefaultCompanyFilesRepository()
        {
            var git = new RepositoryGit(
                name: SafranRepositoryDefaults.CompanyFilesSourceName,
                url: SafranRepositoryDefaults.CompanyFilesSourceUrl,
                useRepository: true);

            CompanyFilesRepositories.Add(new RepositoryGitViewModel(git, gitService, consoleWriter));
            WeakReferenceMessenger.Default.Send(new RepositoriesUpdatedMessage());
        }
```

- [ ] **Step 2: Verify the `RepositoryFolder` / `RepositoryGit` constructors match the arguments you pass**

Run: `grep -n "public.*RepositoryFolder(\|public.*RepositoryGit(" BIA.ToolKit.Domain/Repository*.cs`

If `RepositoryFolder` is declared as `RepositoryFolder(string name, string path, string releasesFolderRegexPattern = null, string companyName = null, string projectName = null, bool useRepository = false)`, the call above matches (using positional + named arg `useRepository`). If `RepositoryGit` does not accept `(name, url, useRepository)` directly, look at its actual ctor; if it requires `(name, url, branch, companyName, projectName, useRepository, …)` adjust the call to use named arguments to satisfy required params (e.g. `new RepositoryGit(name: ..., url: ..., companyName: "Safran", projectName: "BIATemplate", useRepository: true)`).

- [ ] **Step 3: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/ViewModels/MainViewModel.Repositories.cs
git commit -m "feat(config): add one-click default-source bootstrap commands

Three new RelayCommands surface in the V2.15.0 Config-tab empty states:
- UseDefaultToolkitRepositoryCommand
- UseDefaultTemplatesRepositoryCommand
- UseDefaultCompanyFilesRepositoryCommand

Each instantiates a SafranRepositoryDefaults-backed RepositoryFolder
or RepositoryGit, activates it, and broadcasts RepositoriesUpdatedMessage.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 11: Create `UpdateBannerUC`

**Files:**
- Create: `BIA.ToolKit/UserControls/UpdateBannerUC.xaml`
- Create: `BIA.ToolKit/UserControls/UpdateBannerUC.xaml.cs`

- [ ] **Step 1: Create the XAML**

```xml
<UserControl x:Class="BIA.ToolKit.UserControls.UpdateBannerUC"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:conv="clr-namespace:BIA.ToolKit.Converters"
             xmlns:model="clr-namespace:BIA.ToolKit.Domain.Model;assembly=BIA.ToolKit.Domain"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             mc:Ignorable="d"
             d:DesignHeight="80" d:DesignWidth="900">
    <UserControl.Resources>
        <conv:UpdateBannerStateToColorConverter x:Key="BannerColor"/>
        <conv:UpdateBannerStateToGlyphConverter x:Key="BannerGlyph"/>
    </UserControl.Resources>

    <Border Margin="10,4"
            Padding="14,10"
            CornerRadius="6"
            BorderThickness="1"
            BorderBrush="{Binding BannerState, Converter={StaticResource BannerColor}}"
            Background="{DynamicResource MaterialDesign.Brush.Card.Background}">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="auto"/>
            </Grid.ColumnDefinitions>

            <TextBlock Grid.Column="0"
                       Text="{Binding BannerState, Converter={StaticResource BannerGlyph}}"
                       FontSize="24"
                       Foreground="{Binding BannerState, Converter={StaticResource BannerColor}}"
                       VerticalAlignment="Center"
                       Margin="0,0,12,0"/>

            <StackPanel Grid.Column="1" Orientation="Vertical" VerticalAlignment="Center">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="BIA Toolkit "
                               FontWeight="SemiBold"/>
                    <TextBlock Text="{Binding CurrentVersion}"
                               FontWeight="SemiBold"/>

                    <TextBlock Text="  ·  Up to date"
                               Visibility="{Binding BannerState, Converter={StaticResource BannerColor}, ConverterParameter=UpToDate}">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.UpToDate}">
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                    </TextBlock>

                    <TextBlock Margin="6,0,0,0">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.UpdateAvailable}">
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                        <Run Text=" · "/>
                        <Run Text="{Binding LatestVersion, Mode=OneWay}" FontWeight="Bold"/>
                        <Run Text=" available"/>
                    </TextBlock>

                    <TextBlock Text=" · Checking for updates..."
                               Margin="6,0,0,0">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.Checking}">
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                    </TextBlock>

                    <TextBlock Text=" · No update source configured"
                               Margin="6,0,0,0">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.NoSource}">
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                    </TextBlock>

                    <TextBlock Text=" · Could not check for updates"
                               Margin="6,0,0,0">
                        <TextBlock.Style>
                            <Style TargetType="TextBlock">
                                <Setter Property="Visibility" Value="Collapsed"/>
                                <Style.Triggers>
                                    <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.Failed}">
                                        <Setter Property="Visibility" Value="Visible"/>
                                    </DataTrigger>
                                </Style.Triggers>
                            </Style>
                        </TextBlock.Style>
                    </TextBlock>
                </StackPanel>

                <TextBlock FontSize="11" Opacity="0.7" Margin="0,2,0,0">
                    <Run Text="Last checked:"/>
                    <Run Text="{Binding LastCheckedAt, StringFormat={}{0:HH:mm:ss}, TargetNullValue=never, Mode=OneWay}"/>
                </TextBlock>
            </StackPanel>

            <StackPanel Grid.Column="2" Orientation="Horizontal" VerticalAlignment="Center">
                <Button Content="Check now ↻"
                        Command="{Binding CheckForUpdatesBannerCommand}"
                        Style="{StaticResource MaterialDesignOutlinedButton}">
                    <Button.Style>
                        <Style TargetType="Button" BasedOn="{StaticResource MaterialDesignOutlinedButton}">
                            <Setter Property="Visibility" Value="Visible"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.UpdateAvailable}">
                                    <Setter Property="Visibility" Value="Collapsed"/>
                                </DataTrigger>
                                <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.Checking}">
                                    <Setter Property="Visibility" Value="Collapsed"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Button.Style>
                </Button>

                <Button Content="{Binding LatestVersion, StringFormat=Install V{0} →}"
                        Command="{Binding InstallUpdateCommand}"
                        Style="{StaticResource Button.SecondaryAction}"
                        FontWeight="SemiBold"
                        Margin="6,0,0,0">
                    <Button.Style>
                        <Style TargetType="Button" BasedOn="{StaticResource Button.SecondaryAction}">
                            <Setter Property="Visibility" Value="Collapsed"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding BannerState}" Value="{x:Static model:UpdateBannerState.UpdateAvailable}">
                                    <Setter Property="Visibility" Value="Visible"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Button.Style>
                </Button>
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 2: Create the code-behind**

```csharp
namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;

    /// <summary>
    /// State-driven banner sitting at the top of the Config tab. Binds to
    /// MainViewModel.BannerState (and friends).
    /// </summary>
    public partial class UpdateBannerUC : UserControl
    {
        public UpdateBannerUC() { InitializeComponent(); }
    }
}
```

- [ ] **Step 3: Build and clean stale obj if needed**

Run: `rm -rf BIA.ToolKit/obj && dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/UserControls/UpdateBannerUC.xaml BIA.ToolKit/UserControls/UpdateBannerUC.xaml.cs
git commit -m "feat(config): add UpdateBannerUC user control

State-driven banner that shows current version, status text adapted to
BannerState (UpToDate / UpdateAvailable / Checking / NoSource / Failed),
and a contextual action button (Check now vs Install V<x> →). Colors
flow through MaterialDesign theme resources so dark/light toggling
re-renders automatically.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 12: Create `RepositoryCardUC`

**Files:**
- Create: `BIA.ToolKit/UserControls/RepositoryCardUC.xaml`
- Create: `BIA.ToolKit/UserControls/RepositoryCardUC.xaml.cs`

- [ ] **Step 1: Create the XAML**

```xml
<UserControl x:Class="BIA.ToolKit.UserControls.RepositoryCardUC"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:conv="clr-namespace:BIA.ToolKit.Converters"
             xmlns:model="clr-namespace:BIA.ToolKit.Domain.Model;assembly=BIA.ToolKit.Domain"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             mc:Ignorable="d"
             d:DesignHeight="140" d:DesignWidth="360">
    <UserControl.Resources>
        <conv:RepositorySyncStatusToColorConverter x:Key="SyncColor"/>
        <conv:RepositoryTypeToBadgeConverter x:Key="TypeBadge"/>
    </UserControl.Resources>

    <Border Margin="6,4"
            Padding="12"
            CornerRadius="6"
            BorderThickness="1"
            BorderBrush="{Binding SyncStatus, Converter={StaticResource SyncColor}}"
            Background="{DynamicResource MaterialDesign.Brush.Card.Background}">
        <Border.Style>
            <Style TargetType="Border">
                <Setter Property="Opacity" Value="1"/>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding UseRepository}" Value="False">
                        <Setter Property="Opacity" Value="0.6"/>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="auto"/>
                <RowDefinition Height="auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="auto"/>
            </Grid.RowDefinitions>

            <!-- Header: toggle + name + type badge -->
            <Grid Grid.Row="0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="auto"/>
                </Grid.ColumnDefinitions>
                <ToggleButton Grid.Column="0"
                              IsChecked="{Binding UseRepository, Mode=TwoWay}"
                              Style="{StaticResource MaterialDesignSwitchToggleButton}"
                              VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1"
                           Text="{Binding Name}"
                           FontWeight="SemiBold"
                           FontSize="14"
                           Margin="8,0,0,0"
                           VerticalAlignment="Center"
                           TextTrimming="CharacterEllipsis"/>
                <TextBlock Grid.Column="2"
                           Text="{Binding RepositoryType, Converter={StaticResource TypeBadge}}"
                           FontSize="11"
                           Opacity="0.75"
                           VerticalAlignment="Center"/>
            </Grid>

            <!-- Source path / URL -->
            <TextBlock Grid.Row="1"
                       Text="{Binding Source}"
                       FontFamily="Consolas"
                       FontSize="11"
                       Opacity="0.8"
                       TextTrimming="CharacterEllipsis"
                       Margin="0,6,0,0"
                       Cursor="Hand">
                <TextBlock.InputBindings>
                    <MouseBinding MouseAction="LeftClick" Command="{Binding OpenSourceCommand}"/>
                </TextBlock.InputBindings>
            </TextBlock>

            <!-- Status row -->
            <Grid Grid.Row="2" Margin="0,8,0,0" VerticalAlignment="Center">
                <!-- Idle: show "N versions · Latest: V…" -->
                <StackPanel Orientation="Horizontal">
                    <StackPanel.Style>
                        <Style TargetType="StackPanel">
                            <Setter Property="Visibility" Value="Collapsed"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding SyncStatus}" Value="{x:Static model:RepositorySyncStatus.Idle}">
                                    <Setter Property="Visibility" Value="Visible"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </StackPanel.Style>
                    <TextBlock>
                        <Run Text="{Binding VersionCount, Mode=OneWay}"/>
                        <Run Text=" versions"/>
                    </TextBlock>
                    <TextBlock Margin="6,0,0,0">
                        <Run Text=" · Latest: "/>
                        <Run Text="{Binding LatestVersion, Mode=OneWay, TargetNullValue=—}" FontWeight="SemiBold"/>
                    </TextBlock>
                </StackPanel>

                <!-- Syncing -->
                <TextBlock Text="⏳ Fetching versions...">
                    <TextBlock.Style>
                        <Style TargetType="TextBlock">
                            <Setter Property="Visibility" Value="Collapsed"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding SyncStatus}" Value="{x:Static model:RepositorySyncStatus.Syncing}">
                                    <Setter Property="Visibility" Value="Visible"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </TextBlock.Style>
                </TextBlock>

                <!-- Failed -->
                <TextBlock Foreground="{Binding SyncStatus, Converter={StaticResource SyncColor}}"
                           ToolTip="{Binding LastSyncError}">
                    <TextBlock.Style>
                        <Style TargetType="TextBlock">
                            <Setter Property="Visibility" Value="Collapsed"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding SyncStatus}" Value="{x:Static model:RepositorySyncStatus.Failed}">
                                    <Setter Property="Visibility" Value="Visible"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </TextBlock.Style>
                    <Run Text="⚠ Sync failed"/>
                </TextBlock>
            </Grid>

            <!-- Actions row: Edit / Sync / Delete -->
            <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,8,0,0">
                <Button Style="{StaticResource MaterialDesignIconButton}"
                        Command="{Binding OpenFormCommand}"
                        ToolTip="Edit"
                        Padding="4" Width="28" Height="28">
                    <materialDesign:PackIcon Kind="CogOutline" Width="16" Height="16"/>
                </Button>
                <Button Style="{StaticResource MaterialDesignIconButton}"
                        Command="{Binding GetReleasesDataCommand}"
                        ToolTip="Sync versions"
                        Padding="4" Width="28" Height="28">
                    <materialDesign:PackIcon Kind="Sync" Width="16" Height="16"/>
                </Button>
                <Button Style="{StaticResource MaterialDesignIconButton}"
                        Command="{Binding DeleteCommand}"
                        ToolTip="Delete"
                        Padding="4" Width="28" Height="28">
                    <materialDesign:PackIcon Kind="TrashCanOutline" Width="16" Height="16"/>
                </Button>
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 2: Create the code-behind**

```csharp
namespace BIA.ToolKit.UserControls
{
    using System.Windows.Controls;

    /// <summary>
    /// Unified card for a single repository entry. Bound directly to a
    /// RepositoryViewModel via its DataContext; consumed by RepositorySectionUC.
    /// </summary>
    public partial class RepositoryCardUC : UserControl
    {
        public RepositoryCardUC() { InitializeComponent(); }
    }
}
```

- [ ] **Step 3: Build**

Run: `rm -rf BIA.ToolKit/obj && dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/UserControls/RepositoryCardUC.xaml BIA.ToolKit/UserControls/RepositoryCardUC.xaml.cs
git commit -m "feat(config): add RepositoryCardUC user control

Unified card used by the three repo sections. Renders toggle + name +
type badge in the header, the source path / URL as a clickable line,
a status row driven by SyncStatus (idle/syncing/failed) and exposes
Edit / Sync / Delete actions. Inactive repos render at 60% opacity;
sync state recolors the border.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 13: Create `RepositorySectionUC`

**Files:**
- Create: `BIA.ToolKit/UserControls/RepositorySectionUC.xaml`
- Create: `BIA.ToolKit/UserControls/RepositorySectionUC.xaml.cs`

- [ ] **Step 1: Create the XAML**

```xml
<UserControl x:Class="BIA.ToolKit.UserControls.RepositorySectionUC"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:uc="clr-namespace:BIA.ToolKit.UserControls"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             mc:Ignorable="d"
             d:DesignHeight="280" d:DesignWidth="1100">
    <Border Margin="10,8"
            Padding="14"
            CornerRadius="6"
            BorderThickness="1"
            BorderBrush="{DynamicResource MaterialDesign.Brush.Card.Border}"
            Background="{DynamicResource MaterialDesign.Brush.Card.Background}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="auto"/>
                <RowDefinition Height="auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Header -->
            <Grid Grid.Row="0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0"
                           Text="{Binding Icon, RelativeSource={RelativeSource AncestorType=UserControl}}"
                           FontSize="22"
                           Margin="0,0,8,0"
                           VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1"
                           Text="{Binding Title, RelativeSource={RelativeSource AncestorType=UserControl}}"
                           FontSize="13" FontWeight="SemiBold"
                           VerticalAlignment="Center"/>
                <Button Grid.Column="2"
                        Style="{StaticResource MaterialDesignOutlinedButton}"
                        Command="{Binding AddCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        Visibility="{Binding ShowAddButton, RelativeSource={RelativeSource AncestorType=UserControl}, Converter={StaticResource BoolToVis}}"
                        Padding="10,2"
                        Height="28">
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="Plus" Width="14" Height="14" VerticalAlignment="Center"/>
                        <TextBlock Text="Add" Margin="4,0,0,0"/>
                    </StackPanel>
                </Button>
            </Grid>

            <!-- Subtitle -->
            <TextBlock Grid.Row="1"
                       Text="{Binding Subtitle, RelativeSource={RelativeSource AncestorType=UserControl}}"
                       FontSize="11" Opacity="0.7"
                       TextWrapping="Wrap"
                       Margin="0,4,0,8"/>

            <!-- Body: cards or empty state -->
            <Grid Grid.Row="2">
                <ItemsControl ItemsSource="{Binding Items, RelativeSource={RelativeSource AncestorType=UserControl}}">
                    <ItemsControl.Style>
                        <Style TargetType="ItemsControl">
                            <Setter Property="Visibility" Value="Visible"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding HasItems, RelativeSource={RelativeSource AncestorType=UserControl}}" Value="False">
                                    <Setter Property="Visibility" Value="Collapsed"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </ItemsControl.Style>
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <UniformGrid Columns="3"/>
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <uc:RepositoryCardUC/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>

                <!-- Empty state -->
                <Border BorderBrush="{DynamicResource MaterialDesign.Brush.Card.Border}"
                        BorderThickness="1"
                        CornerRadius="6"
                        Padding="20"
                        HorizontalAlignment="Stretch">
                    <Border.Style>
                        <Style TargetType="Border">
                            <Setter Property="Visibility" Value="Collapsed"/>
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding HasItems, RelativeSource={RelativeSource AncestorType=UserControl}}" Value="False">
                                    <Setter Property="Visibility" Value="Visible"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </Border.Style>
                    <StackPanel HorizontalAlignment="Center">
                        <TextBlock Text="🔍 No source configured"
                                   FontSize="13" Opacity="0.7"
                                   HorizontalAlignment="Center"/>
                        <TextBlock Text="{Binding EmptyHint, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                   FontSize="11" Opacity="0.6"
                                   TextWrapping="Wrap"
                                   TextAlignment="Center"
                                   MaxWidth="500"
                                   Margin="0,4,0,12"/>

                        <TextBlock Text="💡 Suggested for Safran users:"
                                   FontSize="11" FontStyle="Italic"
                                   HorizontalAlignment="Center"
                                   Margin="0,8,0,4"/>
                        <TextBlock Text="{Binding DefaultSourceName, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                   FontWeight="SemiBold"
                                   HorizontalAlignment="Center"/>
                        <TextBlock Text="{Binding DefaultSourcePath, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                   FontFamily="Consolas" FontSize="11" Opacity="0.7"
                                   TextAlignment="Center"
                                   TextWrapping="Wrap"
                                   MaxWidth="500"/>

                        <StackPanel Orientation="Horizontal"
                                    HorizontalAlignment="Center"
                                    Margin="0,14,0,0">
                            <Button Content="Use this default"
                                    Style="{StaticResource Button.SecondaryAction}"
                                    Command="{Binding UseDefaultCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    Padding="14,4"/>
                            <Button Content="+ Add custom source"
                                    Style="{StaticResource MaterialDesignOutlinedButton}"
                                    Command="{Binding AddCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    Margin="8,0,0,0"
                                    Padding="14,4"/>
                        </StackPanel>
                    </StackPanel>
                </Border>
            </Grid>
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 2: Create the code-behind**

```csharp
namespace BIA.ToolKit.UserControls
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// One Config-tab section: header (icon + title + subtitle + [+ Add]),
    /// either a UniformGrid of RepositoryCardUC items or the empty-state
    /// panel that offers the Safran default bootstrap.
    /// </summary>
    public partial class RepositorySectionUC : UserControl
    {
        public RepositorySectionUC() { InitializeComponent(); }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }

        public static readonly DependencyProperty EmptyHintProperty =
            DependencyProperty.Register(nameof(EmptyHint), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string EmptyHint { get => (string)GetValue(EmptyHintProperty); set => SetValue(EmptyHintProperty, value); }

        public static readonly DependencyProperty DefaultSourceNameProperty =
            DependencyProperty.Register(nameof(DefaultSourceName), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string DefaultSourceName { get => (string)GetValue(DefaultSourceNameProperty); set => SetValue(DefaultSourceNameProperty, value); }

        public static readonly DependencyProperty DefaultSourcePathProperty =
            DependencyProperty.Register(nameof(DefaultSourcePath), typeof(string), typeof(RepositorySectionUC), new PropertyMetadata(string.Empty));
        public string DefaultSourcePath { get => (string)GetValue(DefaultSourcePathProperty); set => SetValue(DefaultSourcePathProperty, value); }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(RepositorySectionUC),
                new PropertyMetadata(null, OnItemsChanged));
        public IEnumerable Items { get => (IEnumerable)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

        public static readonly DependencyProperty HasItemsProperty =
            DependencyProperty.Register(nameof(HasItems), typeof(bool), typeof(RepositorySectionUC), new PropertyMetadata(false));
        public bool HasItems { get => (bool)GetValue(HasItemsProperty); private set => SetValue(HasItemsProperty, value); }

        public static readonly DependencyProperty ShowAddButtonProperty =
            DependencyProperty.Register(nameof(ShowAddButton), typeof(bool), typeof(RepositorySectionUC), new PropertyMetadata(true));
        public bool ShowAddButton { get => (bool)GetValue(ShowAddButtonProperty); set => SetValue(ShowAddButtonProperty, value); }

        public static readonly DependencyProperty AddCommandProperty =
            DependencyProperty.Register(nameof(AddCommand), typeof(ICommand), typeof(RepositorySectionUC), new PropertyMetadata(null));
        public ICommand AddCommand { get => (ICommand)GetValue(AddCommandProperty); set => SetValue(AddCommandProperty, value); }

        public static readonly DependencyProperty UseDefaultCommandProperty =
            DependencyProperty.Register(nameof(UseDefaultCommand), typeof(ICommand), typeof(RepositorySectionUC), new PropertyMetadata(null));
        public ICommand UseDefaultCommand { get => (ICommand)GetValue(UseDefaultCommandProperty); set => SetValue(UseDefaultCommandProperty, value); }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RepositorySectionUC self) return;

            if (e.OldValue is INotifyCollectionChanged oldNotify)
                oldNotify.CollectionChanged -= self.OnItemsCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newNotify)
                newNotify.CollectionChanged += self.OnItemsCollectionChanged;

            self.RefreshHasItems();
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RefreshHasItems();

        private void RefreshHasItems()
        {
            bool any = false;
            if (Items is not null)
            {
                foreach (var _ in Items) { any = true; break; }
            }
            HasItems = any;
        }
    }
}
```

- [ ] **Step 3: Ensure the existing `BoolToVis` resource key is available** in the merged dictionaries of `App.xaml` (the rest of the codebase uses `BoolToVisConverter` — check what key the section binding uses)

Run: `grep -rn 'x:Key="BoolToVis"' BIA.ToolKit/`
If no result, add this fallback inline at the top of `RepositorySectionUC.xaml` resources (before the closing `UserControl.Resources` tag if any, or wrap the body in `<UserControl.Resources><BooleanToVisibilityConverter x:Key="BoolToVis"/></UserControl.Resources>` near the start of the file).

Add this snippet directly at the top of `RepositorySectionUC.xaml` between the `xmlns` declarations and the `<Border>` root child to be safe:

```xml
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
    </UserControl.Resources>
```

- [ ] **Step 4: Build**

Run: `rm -rf BIA.ToolKit/obj && dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 5: Commit**

```bash
git add BIA.ToolKit/UserControls/RepositorySectionUC.xaml BIA.ToolKit/UserControls/RepositorySectionUC.xaml.cs
git commit -m "feat(config): add RepositorySectionUC user control

One Config-tab section component, parameterized by Icon, Title,
Subtitle, EmptyHint, DefaultSourceName, DefaultSourcePath, Items
collection, AddCommand, UseDefaultCommand. Renders a UniformGrid of
RepositoryCardUC items when populated, falls back to a guided empty
state with the Safran default bootstrap buttons when empty.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 14: Rewire the Config tab in `MainWindow.xaml`

**Files:**
- Modify: `BIA.ToolKit/MainWindow.xaml`

- [ ] **Step 1: Locate the existing Config tab content**

Open `BIA.ToolKit/MainWindow.xaml` and find the `<TabItem>` whose Header is "Config" (search for `Header="Config"`). Note the section beginning at this `TabItem` and ending at its closing `</TabItem>` — this is what you'll replace.

- [ ] **Step 2: Replace the Config tab content**

Replace everything inside the `<TabItem Header="Config" ...>...</TabItem>` block with this body:

```xml
<TabItem Header="Config">
    <ScrollViewer Margin="6"
                  HorizontalScrollBarVisibility="Auto"
                  VerticalScrollBarVisibility="Auto">
        <StackPanel>
            <!-- 1. Update banner -->
            <uc:UpdateBannerUC/>

            <!-- 2. Import / Export toolbar (kept as a small row) -->
            <StackPanel Orientation="Horizontal" Margin="10,4">
                <Button Style="{StaticResource MaterialDesignOutlinedButton}"
                        Command="{Binding ImportConfigurationCommand}"
                        Padding="10,2" Height="28">
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="Import" Width="14" Height="14" VerticalAlignment="Center"/>
                        <TextBlock Text="Import Configuration" Margin="4,0,0,0"/>
                    </StackPanel>
                </Button>
                <Button Style="{StaticResource MaterialDesignOutlinedButton}"
                        Command="{Binding ExportConfigurationCommand}"
                        Margin="8,0,0,0"
                        Padding="10,2" Height="28">
                    <StackPanel Orientation="Horizontal">
                        <materialDesign:PackIcon Kind="Export" Width="14" Height="14" VerticalAlignment="Center"/>
                        <TextBlock Text="Export Configuration" Margin="4,0,0,0"/>
                    </StackPanel>
                </Button>
            </StackPanel>

            <!-- 3. Toolkit Update Source -->
            <uc:RepositorySectionUC Icon="🔧"
                                    Title="Toolkit Update Source"
                                    Subtitle="Where BIA Toolkit checks for its own updates. Configure once — the app auto-updates from this source."
                                    EmptyHint="The auto-update feature is disabled until you add a source."
                                    DefaultSourceName="BIAToolkit Shared"
                                    DefaultSourcePath="\\share.bia.safran\BIAToolKit\Releases\BiaToolkit"
                                    Items="{Binding ToolkitRepositorySingleton}"
                                    ShowAddButton="False"
                                    AddCommand="{Binding OpenToolkitRepositorySettingsCommand}"
                                    UseDefaultCommand="{Binding UseDefaultToolkitRepositoryCommand}"/>

            <!-- 4. Templates -->
            <uc:RepositorySectionUC Icon="📦"
                                    Title="Templates Repositories"
                                    Subtitle="Sources of BIA framework templates used to scaffold new projects and as baseline for migrations. At least one active source required."
                                    EmptyHint="You can't create or migrate projects until you add at least one source."
                                    DefaultSourceName="BIATemplate Shared"
                                    DefaultSourcePath="\\share.bia.safran\BIAToolKit\Releases\BiaTemplate"
                                    Items="{Binding TemplateRepositories}"
                                    AddCommand="{Binding AddTemplateRepositoryCommand}"
                                    UseDefaultCommand="{Binding UseDefaultTemplatesRepositoryCommand}"/>

            <!-- 5. Company Files (optional) -->
            <uc:RepositorySectionUC Icon="🏢"
                                    Title="Company Files Repositories (optional)"
                                    Subtitle="Company-specific overrides applied on top of templates when creating new projects. Leave empty for vanilla BIA projects."
                                    EmptyHint="New projects will use BIA defaults without company customization."
                                    DefaultSourceName="BIACompanyFiles Azure"
                                    DefaultSourcePath="https://azure.devops.safran/SafranElectricalAndPower/Digital%20Manufacturing/_git/BIACompanyFiles"
                                    Items="{Binding CompanyFilesRepositories}"
                                    AddCommand="{Binding AddCompanyFilesRepositoryCommand}"
                                    UseDefaultCommand="{Binding UseDefaultCompanyFilesRepositoryCommand}"/>
        </StackPanel>
    </ScrollViewer>
</TabItem>
```

- [ ] **Step 3: Expose `ToolkitRepositorySingleton` on MainViewModel**

The Toolkit Source section binds `Items="{Binding ToolkitRepositorySingleton}"` — a one-or-zero-element enumerable. Add a computed property to `MainViewModel.UpdateBanner.cs` (the partial file from Task 9) right after the `CurrentVersion` getter:

```csharp
        /// <summary>
        /// Single-item collection wrapping <see cref="ToolkitRepository"/>
        /// so the Config-tab Toolkit Source section can bind to the same
        /// RepositorySectionUC.Items API as the multi-source sections.
        /// </summary>
        public System.Collections.Generic.IEnumerable<RepositoryViewModel> ToolkitRepositorySingleton
            => ToolkitRepository is null ? [] : new[] { ToolkitRepository };
```

Also raise `OnPropertyChanged(nameof(ToolkitRepositorySingleton))` whenever `ToolkitRepository` changes. To do that, declare a partial method `OnToolkitRepositoryChanged` in the same file (the source generator from `[ObservableProperty]` in `MainViewModel.Repositories.cs` line 27 calls partial hooks named after the property):

```csharp
        partial void OnToolkitRepositoryChanged(RepositoryViewModel value)
        {
            OnPropertyChanged(nameof(ToolkitRepositorySingleton));
        }
```

- [ ] **Step 4: Build and clean obj if needed**

Run: `rm -rf BIA.ToolKit/obj && dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 5: Smoke test (manual)**

Run: `dotnet run --project BIA.ToolKit/BIA.ToolKit.csproj`
Open the Config tab and confirm:
- The update banner renders at the top (state depends on your current ToolkitRepository config)
- Import / Export Configuration buttons appear below the banner as a small row
- The three sections render with their icon + title + subtitle
- Existing TemplateRepositories and CompanyFilesRepositories cards render correctly
- The empty state shows for any section that has no sources

If the UI looks broken, capture a screenshot and check the XAML binding paths before committing.

- [ ] **Step 6: Commit**

```bash
git add BIA.ToolKit/MainWindow.xaml BIA.ToolKit/ViewModels/MainViewModel.UpdateBanner.cs
git commit -m "feat(config): rewire Config tab with UpdateBanner and three unified sections

Replaces the legacy two-column TemplateRepositories | CompanyFilesRepositories
layout (plus the lone Toolkit Repository Settings button) with a single
ScrollViewer holding: UpdateBannerUC, a small Import/Export toolbar,
and three RepositorySectionUC instances (Toolkit Update Source,
Templates, Company Files). Adds a ToolkitRepositorySingleton property
that wraps the single ToolkitRepository as a one-element IEnumerable
so the Toolkit section reuses the same RepositorySectionUC API as the
multi-source ones.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 15: Inline helper text inside `RepositoryFormUC` dialog

**Files:**
- Modify: `BIA.ToolKit/Dialogs/RepositoryFormUC.xaml` (or wherever the existing repository edit form lives; search if needed)

- [ ] **Step 1: Locate the file**

Run: `find BIA.ToolKit -name "RepositoryFormUC.xaml" 2>&1 | head`
Expected: one path returned. Open that file.

- [ ] **Step 2: Add helper lines below each field**

For each existing field in the dialog form (Name, Repository Type, Path / Url, Release Regex Pattern), add a `<TextBlock>` immediately below it with these helper texts (each TextBlock styled `FontSize=10 Opacity=0.65 Margin="0,2,0,8" FontStyle=Italic`):

- **Name:** `Friendly label, e.g. "Safran share"`
- **Repository Type:** `Folder = SMB share · Git = remote URL`
- **Path / Url:** `Example: \\share.bia.safran\… or https://github.com/…`
- **Release Regex Pattern (Optional):** `Default matches V1.2.3 and V1.2.3.4`

Concrete XAML pattern to add under each existing field:

```xml
<TextBlock Text="Friendly label, e.g. &quot;Safran share&quot;"
           FontSize="10" Opacity="0.65" FontStyle="Italic"
           Margin="0,2,0,8"/>
```

(Adjust the `Text` per field. The exact placement depends on whether the form uses Grid Rows or a StackPanel — the project's existing form is a StackPanel, so simply insert the TextBlock right after the input field for that property.)

- [ ] **Step 3: Build**

Run: `dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/Dialogs/RepositoryFormUC.xaml
git commit -m "feat(config): add inline helper text to RepositoryFormUC dialog

One italic helper line per field (Name / Type / Path / Regex) so the
Add/Edit modal answers the 'what do I put here?' question without
external documentation. The dialog mechanics are otherwise unchanged.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 16: Manual integration test (no code change — verification only)

**Files:** none

- [ ] **Step 1: Stop the app, full rebuild**

```powershell
Stop-Process -Name "BIA.ToolKit" -Force -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force BIA.ToolKit\obj -ErrorAction SilentlyContinue
dotnet build BIA.ToolKit/BIA.ToolKit.csproj --nologo -v:m | Select-String "Erreur|réussi"
```
Expected: `La génération a réussi.` with `0 Erreur(s)`.

- [ ] **Step 2: Launch and run the spec's manual test plan in order**

```powershell
.\BIA.ToolKit\bin\Debug\net10.0-windows10.0.26100.0\win-x64\BIA.ToolKit.exe
```

For each test in the spec section "Test plan (manual UI)", observe the UI and the Output panel. Stop and investigate at the first failure.

1. **First-time empty state** — temporarily clear a section's repos via the Edit dialog, confirm the empty state surfaces the Safran default with `Use this default` working in one click.
2. **Banner up-to-date** — click `Check now`, observe transitions Checking → UpToDate, verify timestamp updates.
3. **Banner update available** — to simulate, change the local version constant temporarily OR point ToolkitRepository at a release source that has a higher version. Verify orange banner with `Install` action.
4. **Banner checking** — observe the transient `⏳ Checking for updates...` state and the cancel availability.
5. **Banner failed** — disconnect network, click `Check now`, verify the red state with retry button and the error tooltip.
6. **Card sync** — toggle a Templates repo from inactive to active, observe status row populating with `N versions · Latest: VX.Y.Z`.
7. **Card sync failure** — point a Git repo to an invalid URL, toggle on, verify the card lands in `Sync failed` state with orange border and error tooltip.
8. **Section status glyph** — Templates has ≥ 1 active source → ✅ shown in header (Note: section status glyph isn't in this plan as a separate task; if it's implemented, verify; if not, skip and add as a follow-up).
9. **Theme toggle** — switch dark ↔ light, verify the banner colors and card borders stay legible.
10. **Other tabs unaffected** — open Création / Migration / Génération, verify no regression.

If any test fails, capture the symptom, fix the offending task's code, and re-test from the failing test downward.

- [ ] **Step 3: No commit (verification-only task)**

---

### Task 17: Bump version to V2.15.0

**Files:**
- Modify: `BIA.ToolKit/BIA.ToolKit.csproj` (line containing `<Version>2.14.0</Version>`)

- [ ] **Step 1: Bump the version**

In `BIA.ToolKit/BIA.ToolKit.csproj` replace:

Before: `<Version>2.14.0</Version>`
After:  `<Version>2.15.0</Version>`

- [ ] **Step 2: Final solution build**

Run: `dotnet build BIAToolKit.sln --nologo -v:m 2>&1 | tail -5`
Expected: `0 Erreur(s)`.

- [ ] **Step 3: dotnet format style sanity**

Run: `dotnet format style --severity info --verify-no-changes 2>&1 | tail -10`
If issues are reported, auto-fix with `dotnet format style --severity info` then rebuild.

- [ ] **Step 4: Commit**

```bash
git add BIA.ToolKit/BIA.ToolKit.csproj
git commit -m "Up version to 2.15.0"
```

- [ ] **Step 5: Show the final commit list on the branch for review**

Run: `git log --oneline main..HEAD`
Expected (hashes will differ):
```
<hash>  Up version to 2.15.0
<hash>  feat(config): add inline helper text to RepositoryFormUC dialog
<hash>  feat(config): rewire Config tab with UpdateBanner and three unified sections
<hash>  feat(config): add RepositorySectionUC user control
<hash>  feat(config): add RepositoryCardUC user control
<hash>  feat(config): add UpdateBannerUC user control
<hash>  feat(config): add one-click default-source bootstrap commands
<hash>  feat(config): add update banner state and commands to MainViewModel
<hash>  feat(config): expose sync status and version info on RepositoryViewModel
<hash>  feat(config): add RepositoryTypeToBadgeConverter
<hash>  feat(config): add RepositorySyncStatusToColorConverter for card border
<hash>  feat(config): add UpdateBannerStateToGlyphConverter
<hash>  feat(config): add UpdateBannerStateToColorConverter
<hash>  feat(common): add SafranRepositoryDefaults for empty-state bootstrap
<hash>  feat(domain): add RepositorySyncStatus enum for repo card state
<hash>  feat(domain): add UpdateBannerState enum for Config-tab update banner
<hash>  docs(spec): add V2.15.0 Config screen redesign design
```

- [ ] **Step 6: STOP before gitflow finish**

Do NOT merge into main / develop or push tags. Per the user's standing convention, gitflow finish and push to main are explicitly user-authorized actions. Report the final commit list and wait for the user's "go gitflow finish V2.15.0 + push" before proceeding to:

```bash
git checkout main && git merge --no-ff feature/fixes-V2.15.0 -m "Merge branch 'feature/fixes-V2.15.0'" && git tag V2.15.0 && git tag V2.15.0.0
git checkout develop && git merge --no-ff feature/fixes-V2.15.0 -m "Merge branch 'feature/fixes-V2.15.0' into develop" && git branch -d feature/fixes-V2.15.0
git push origin main develop --follow-tags
git push origin V2.15.0 V2.15.0.0
```

---

## Self-Review Notes

**Spec coverage:**
- §1 Layout (banner, toolbar, three sections) — Task 14
- §2 Update Banner state machine + properties — Tasks 1, 4, 5, 9, 11
- §3 Card design — Tasks 6, 7, 8, 12
- §4 Section headers + empty states + Safran defaults — Tasks 3, 10, 13, 14
- §5 Inline help in existing dialog — Task 15
- §6 Section status indicator (✅ ⚠ —) — **noted as out-of-plan**; can be added incrementally without blocking V2.15.0 by extending `RepositorySectionUC` with a `SectionStatus` DP later. Documented in Task 16 step 2.8.
- Test plan — Task 16
- Version bump — Task 17

**Placeholder scan:** no "TBD" / "add appropriate handling" / "similar to" left in the plan. Each task ships complete XAML / C# code or explicit grep-then-adjust instructions.

**Type consistency:**
- `UpdateBannerState` (enum) — Task 1 — used by Tasks 4, 5, 9, 11, 14.
- `RepositorySyncStatus` (enum) — Task 2 — used by Tasks 6, 8, 12.
- `SafranRepositoryDefaults` (static class) — Task 3 — used by Task 10, surfaced by Tasks 13/14 as text constants.
- `RepositoryViewModel.SyncStatus / VersionCount / LatestVersion / LastSyncError` — Task 8 — bound by Task 12.
- `MainViewModel.BannerState / CurrentVersion / LatestVersion / LastCheckedAt / LastError / ToolkitRepositorySingleton` — Tasks 9 + 14 — bound by Tasks 11 and 14.
- `MainViewModel.CheckForUpdatesBannerCommand / InstallUpdateCommand / UseDefaultToolkitRepositoryCommand / UseDefaultTemplatesRepositoryCommand / UseDefaultCompanyFilesRepositoryCommand` — Tasks 9 and 10 — bound by Tasks 11 and 14.
- `RepositorySectionUC` dependency properties (Icon, Title, Subtitle, EmptyHint, DefaultSourceName, DefaultSourcePath, Items, HasItems, ShowAddButton, AddCommand, UseDefaultCommand) — Task 13 — bound by Task 14.
