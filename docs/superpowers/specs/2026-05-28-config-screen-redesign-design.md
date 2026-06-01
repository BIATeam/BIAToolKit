# Config screen redesign — Design

**Status**: Approved (brainstorming) — pending implementation plan
**Target version**: V2.15.0
**Branch**: `feature/fixes-V2.15.0`
**Date**: 2026-05-28

## Context

The Config tab is the entry point for configuring where the ToolKit
finds: (a) its own update binaries, (b) BIA framework templates used
for project creation and migration, and (c) optional company-specific
overlay files (Safran Wiring, DMEU/DMNA branding, etc.).

The current screen has three structural and UX problems:

1. **Inconsistent visual treatment of the three repo categories.**
   Templates and Company Files appear as full-width cards laid out in
   two columns. The Toolkit Repository hides behind a tiny "Toolkit
   Repository Settings" button in the top-right corner. A first-time
   user cannot tell that the three concepts are conceptually parallel.
2. **No at-a-glance update status.** The user cannot see whether the
   ToolKit is up to date or whether a new version is available without
   triggering the manual update check. The window-level "Auto update"
   toggle in the title bar conveys intent ("check periodically") but
   not state ("am I current?").
3. **High cognitive load to onboard.** Users self-installing the
   ToolKit (no IT-driven setup) struggle to understand what each
   category is for, what to put in it, and which source format is
   accepted (Git URL vs SMB path vs Azure DevOps). The empty / partial
   state of the screen offers no guidance.

The primary use case captured during brainstorming: "configuration
done once and never revisited", but currently the screen is *not
self-explanatory enough* for the user setting it up that one time.

## Goals

- Make the Config screen **self-descriptive** so a first-time user
  understands each category, its purpose, and what to put in it
  without external documentation.
- Unify the three repo categories visually so they read as parallel
  concepts.
- Surface ToolKit update status at the top of the screen with one
  glance, including a one-click install action when an update is
  available.
- Provide ready-to-use **Safran defaults** for each category so the
  user can bootstrap with one click instead of typing URLs or paths.
- Show, per repository, the number of detected versions and the
  latest version — the data needed to confirm "this source works and
  is current".

## Non-goals

- No replacement of the existing `RepositoryFormUC` dialog. The
  Add/Edit flow keeps the modal dialog; we only add inline help.
- No multi-source support for the Toolkit Update Source (at most one
  active source, mirrors current behaviour).
- No restructuring of the `UpdateService` itself — the redesign only
  consumes its existing API and adds new state binding.
- No first-run wizard. The screen stays a single-tab view; we just
  enrich it.

## Design

### 1. Overall layout

Single vertical column inside a `ScrollViewer`. Section ordering from
top to bottom:

```
TabItem "Config"
└── ScrollViewer
    └── StackPanel
        ├── UpdateBanner                    (state-driven banner)
        ├── ImportExportToolbar             (existing buttons, demoted)
        ├── Section "Toolkit Update Source"
        ├── Section "Templates Repositories"
        └── Section "Company Files Repositories (optional)"
```

The two-column Templates / Company Files layout disappears. Each
section is full-width with its own UniformGrid for cards (3 cols at
1920px, 2 at 1280px, 1 at 1024px). Toolkit Update Source becomes a
first-class section so the three categories read as parallel concepts.

### 2. Update Banner

Five mutually exclusive states bound to a single
`UpdateBannerState` enum:

| State | Icon | Color | Action |
|-------|------|-------|--------|
| `UpToDate` | ✅ | ValidationSuccess | `[Check now ↻]` |
| `UpdateAvailable` | ⚠ | ValidationWarning | `[Install V2.X.Y →]` (prominent) |
| `Checking` | ⏳ | Primary | `[⏹ Cancel]` |
| `NoSource` | 🔧 | Neutral gray | Anchor link to Toolkit Source section |
| `Failed` | ✗ | ValidationError | `[Retry]` + tooltip with error text |

Banner content per state shows current version, last-check timestamp,
and the state-specific action. State, color, and glyph derive through
converters from `UpdateBannerState`, following the same pattern as
`MigrationStepStatus` in V2.14.0 (re-renders on theme switch through
existing `ReRenderMessages` plumbing).

The window-level "Auto update" toggle in the title bar stays
unchanged — it conveys intent (check periodically) while the banner
conveys state (am I current?).

New properties exposed on `MainViewModel`:

```csharp
public string CurrentVersion { get; }
public string? LatestVersion { get; }
public DateTime? LastCheckedAt { get; }
public UpdateBannerState BannerState { get; }
public string? LastError { get; }
public IAsyncRelayCommand CheckForUpdatesCommand { get; }
public IAsyncRelayCommand InstallUpdateCommand { get; }
```

The `CheckForUpdatesCommand` and `InstallUpdateCommand` wrap the
existing `UpdateService` calls, adding the state transitions.

### 3. Repo card

Unified card across the three categories. Card body (approximately
360×140 px):

```
┌─────────────────────────────────────────────────────────┐
│ ◉  BIATemplate Shared              📁 Folder            │  Toggle + Name + Type
│    \\share.bia.safran\BIAToolKit\Releases\BiaTemplate   │  Clickable source
│                                                         │
│ ✓ 12 versions  ·  Latest: V7.0.3      [⚙] [🔄] [🗑]   │  Status + Actions
└─────────────────────────────────────────────────────────┘
```

| Element | Content | Behaviour |
|---------|---------|-----------|
| Toggle ◉/◯ | Active / inactive | Click flips state; activation triggers auto-sync |
| Name | User-chosen friendly label | Display-only |
| Type badge | 📁 Folder · 🌐 Git · ☁️ Azure | Derived from repo type, read-only |
| Source | URL or path, monospace, middle-ellipsis | Click opens in browser / explorer |
| Status row | "N versions · Latest: VX.Y.Z" | Updated after each sync; tooltip carries last-sync timestamp |
| Edit ⚙ | Opens existing `RepositoryFormUC` in Edit mode | Unchanged |
| Sync 🔄 | Force version detection | Transitions card to `Syncing` then back to `Active` or `Failed` |
| Delete 🗑 | Confirmation then removal | Unchanged |

Card visual states:

- **Active** — full color, primary-tinted border
- **Inactive** — 60% opacity, gray border, muted text
- **Syncing** — status row replaced by `⏳ Fetching versions…` and cancel button
- **Sync failed** — orange border, status row shows `⚠ Sync failed: <short reason>` with tooltip carrying the full error

The dense fields shown in the current screen (`Repository Type` label,
explicit `Synchronized Folder` cache path, separate browse / open
buttons) drop from the card. The technical cache path moves into the
Edit dialog where users who need it can still see it.

### 4. Section headers and empty states

Each section header carries a descriptive title, helper subtitle, and
the `[+ Add]` button. Headers act as the primary discovery surface:
they explain the category in one phrase.

```
🔧  TOOLKIT UPDATE SOURCE
Where BIA Toolkit checks for its own updates. Configure once — the
app auto-updates from this source.

📦  TEMPLATES REPOSITORIES                            [+ Add]
Sources of BIA framework templates used to scaffold new projects and
as baseline for migrations. At least one active source required.

🏢  COMPANY FILES REPOSITORIES (optional)             [+ Add]
Company-specific overrides applied on top of templates when creating
new projects. Leave empty for vanilla BIA projects.
```

A small `ⓘ More` chip next to the subtitle opens a popover with longer
explanation and concrete examples.

Empty states surface a Safran-specific default to remove the "I don't
know what to put" friction:

```
┌─ Empty state pattern ────────────────────────────────────────┐
│                                                              │
│    🔍  No templates repository configured                    │
│                                                              │
│    You can't create or migrate projects until you add at     │
│    least one source.                                          │
│                                                              │
│    ────────────────────────────────────────────────          │
│                                                              │
│    💡  Suggested for Safran users:                           │
│                                                              │
│    📁  BIATemplate Shared                                    │
│        \\share.bia.safran\BIAToolKit\Releases\BiaTemplate    │
│                                                              │
│    [ Use this default ]    [ + Add custom source ]           │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

The defaults are hardcoded constants in the codebase, one per
category:

```csharp
internal static class SafranRepositoryDefaults
{
    public const string ToolkitUpdateSourceName = "BIAToolkit Shared";
    public const string ToolkitUpdateSourcePath =
        @"\\share.bia.safran\BIAToolKit\Releases\BiaToolkit";

    public const string TemplatesSourceName = "BIATemplate Shared";
    public const string TemplatesSourcePath =
        @"\\share.bia.safran\BIAToolKit\Releases\BiaTemplate";

    public const string CompanyFilesSourceName = "BIACompanyFiles Azure";
    public const string CompanyFilesSourceUrl =
        "https://azure.devops.safran/SafranElectricalAndPower/Digital%20Manufacturing/_git/BIACompanyFiles";
}
```

A trailing comment in that class flags that these are Safran-specific
and would need tuning for any other consumer of the ToolKit.
`Use this default` adds the configured repo and activates it in one
click; no dialog to fill.

### 5. Inline help inside the existing Add / Edit dialog

The `RepositoryFormUC` dialog stays but each field grows a small italic
helper line:

| Field | Helper text |
|-------|-------------|
| Name | "Friendly label, e.g. `Safran share`" |
| Repository Type | "Folder = SMB share · Git = remote URL" |
| Path | "Example: `\\share.bia.safran\…` or `https://github.com/…`" |
| Release Regex Pattern | "Default matches `V1.2.3` and `V1.2.3.4`" |

### 6. Section status indicators

To the right of each section header, a single status glyph reflects
whether the section is "OK":

- ✅ green check when ≥ 1 active source in the section
- ⚠ orange triangle when section has 0 active sources and is required
  (Toolkit Source and Templates)
- — neutral dash when section has 0 active sources and is optional
  (Company Files)

This gives the user a triple-section glance: a screen with three ✅ is
"everything fine"; a ⚠ pin-points where setup is missing.

## Impact

### Files modified

| File | Nature | Approximate volume |
|------|--------|--------------------|
| `BIA.ToolKit/MainWindow.xaml` | Replace the Config tab content (the `TabConfig` content block) | ~250 lines new for ~120 lines old |
| `BIA.ToolKit/ViewModels/MainViewModel.cs` (or new partial `MainViewModel.UpdateBanner.cs`) | Expose `UpdateBannerState`, `CurrentVersion`, `LatestVersion`, `LastCheckedAt`, `LastError` + commands | +120 lines |
| `BIA.ToolKit/ViewModels/MainViewModel.Repositories.cs` | Add `LatestVersion`, `VersionCount`, `LastSyncAt`, `LastSyncError`, `SyncStatus` per `RepositoryViewModel`; add `AddDefaultTemplateRepositoryCommand` and peers | +60 lines |
| `BIA.ToolKit/Helper/Converters/UpdateBannerStateToColorConverter.cs` etc. | New status converters (color, glyph, action label) | 3 small files ~30 lines each |

### Files created

| File | Role |
|------|------|
| `BIA.ToolKit.Domain/Model/UpdateBannerState.cs` | enum |
| `BIA.ToolKit.Domain/Model/RepositorySyncStatus.cs` | enum (Idle / Syncing / Failed) |
| `BIA.ToolKit.Common/SafranRepositoryDefaults.cs` | constants for Safran defaults |
| `BIA.ToolKit/UserControls/UpdateBannerUC.xaml(.cs)` | dedicated UC for the banner |
| `BIA.ToolKit/UserControls/RepositoryCardUC.xaml(.cs)` | unified card used by the three sections |
| `BIA.ToolKit/UserControls/RepositorySectionUC.xaml(.cs)` | header + helper + empty-state + UniformGrid of cards |
| `BIA.ToolKit/Converters/UpdateBannerStateToColorConverter.cs` | banner color |
| `BIA.ToolKit/Converters/UpdateBannerStateToGlyphConverter.cs` | banner glyph |
| `BIA.ToolKit/Converters/UpdateBannerStateToActionLabelConverter.cs` | banner button label |
| `BIA.ToolKit/Converters/RepositorySyncStatusToColorConverter.cs` | card border color |
| `BIA.ToolKit/Converters/RepositoryTypeToBadgeConverter.cs` | type icon + label |

### Files untouched

- `BIA.ToolKit/UserControls/RepositoryFormUC.xaml(.cs)` — Add / Edit
  dialog stays, only its helper lines change (small XAML edit inline).
- `BIA.ToolKit.Application/Services/UpdateService.cs` — consumed as
  is; no new method needed beyond the existing
  `CheckForUpdatesAsync(CancellationToken)` and
  `DownloadUpdateAsync(CancellationToken)`.
- The rest of the application (Création / Migration / Génération
  tabs).

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Banner state churns rapidly on `Checking` ↔ `UpToDate` if auto-update fires often | Throttle `CheckForUpdatesCommand` to once per minute when called manually; let the existing auto-update timer drive periodic checks |
| Hardcoded Safran defaults make the codebase company-specific | Centralised in one `SafranRepositoryDefaults` class with a trailing comment flagging that fork would need to tune. Acceptable trade because the ToolKit is already Safran-centric (see `BIACompanyFiles Azure` defaults already shipping). |
| Sync auto-on-activation slows the toggle UX on slow networks | Sync runs async with `Task.Run`, the toggle flips immediately, and the card shows `Syncing` overlay. Cancellable through the existing busy waiter pattern from V2.12.1. |
| Removing the "Synchronized Folder" field from the card surprises power users | Add it back inside the Edit dialog as a read-only field with a "Clear cache" button there. Power-user concern but expert path stays available. |
| `Use this default` adds a duplicate if the user clicks it twice | Check for existing repo with same path before adding. If exists, just activate it. |

## Test plan (manual UI)

1. **First-time empty state** — open Config on a fresh install. Verify
   each section displays its empty state with Safran default and
   `Use this default` works in one click.
2. **Banner up-to-date** — with a recent version installed and a
   working source, banner shows green with `Check now` action.
3. **Banner update available** — bump the version returned by
   `UpdateService` mock, verify the banner turns orange with
   `Install` action.
4. **Banner checking** — click `Check now`, observe the transient
   `⏳ Checking for updates...` state and the cancel button.
5. **Banner failed** — disconnect network, click `Check now`, verify
   the red state with retry button and the error tooltip.
6. **Card sync** — toggle a repo from inactive to active and observe
   the auto-sync; status row updates from no-info to `N versions ·
   Latest: VX.Y.Z`.
7. **Card sync failure** — point a Git repo to an invalid URL,
   toggle on, verify the card lands in `Sync failed` state with
   orange border and tooltip carrying the error.
8. **Section status glyph** — Templates has ≥ 1 active source → ✅;
   disable all → ⚠.
9. **Theme toggle** — switch dark ↔ light, verify the banner colors
   and card borders stay legible (uses the same converter pattern as
   V2.14.0 migration stepper).
10. **Other tabs unaffected** — open Création / Migration / Génération,
    verify no regression in the rest of the application.

## Out of scope (deferred follow-ups)

- Multi-source Toolkit Update Source (i.e. failover between Shared and
  Azure).
- Per-repo version pinning ("only use V7.0.x from this source").
- First-run wizard.
- In-app changelog viewer (the `See changelog` link in
  `UpdateAvailable` banner opens the GitHub release page).
