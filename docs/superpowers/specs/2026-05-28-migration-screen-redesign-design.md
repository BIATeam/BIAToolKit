# Migration screen redesign — Design

**Status**: Approved (brainstorming) — pending implementation plan
**Target version**: V2.14.0
**Branch**: `feature/fixes-V2.14.0`
**Date**: 2026-05-28

## Context

The "Project" sub-tab of `ModifyProjectUC` is the entry point of the
project migration workflow. It currently stacks two large panels
("Original" and "Target") vertically, each embedding the full
`VersionAndOptionUserControl` (498 lines of dense XAML — Framework
Version, Feature Settings checkboxes, Company files with multiple
DMEU/DMNA/… checkboxes, profile dropdown, Company File Version). The
migration action sidebar lives on the right with a "Migrate" button
and a stack of six step-by-step buttons (Generate Only → Open Folder →
Apply Diff → Merge Rejected → Overwrite `\BIA-.*` → Fix Usings).

Two pain points motivated this redesign:

1. **Vertical stacking hides the before/after comparison.** Devs read
   left-to-right; aligning Original and Target side-by-side feels more
   natural for "before / after" inspection.
2. **The six step buttons are visually disconnected from the global
   Output panel.** When a dev clicks "Apply Diff", they watch lines
   scroll in the global output with no clear signal of which lines
   belong to which step, no status feedback on the button itself, and
   no sense of progression toward the next step.

Original was confirmed *editable but rarely* — the dev tweaks Target,
not Original. The primary action remains the migrate buttons; the
side-by-side comparison is supporting context.

## Goals

- **Side-by-side** Original | Target layout for natural before/after
  reading, with internal scroll per panel to absorb the inner UC's
  density.
- **Visual workflow stepper** replacing the vertical stack of step
  buttons: six numbered cells in horizontal sequence, each carrying a
  status badge (`Pending` / `Running` / `Done` / `Warning` / `Failed`),
  with the next-suggested step highlighted.
- **Output color-coded per step**: each line emitted during a step run
  shows a vertical stripe in the step's status color, so a dev
  scrolling the output instantly sees which lines came from which step.
- **No regression** outside the Project tab — `IConsoleWriter` stays
  backward-compatible, the global RichTextBox renderer stays in place,
  and other tabs (CRUD / DTO / Option / Create / Settings) keep
  emitting plain-output as today.

## Non-goals

- No refactor of `VersionAndOptionUserControl`. It is used as-is in
  both Original and Target panels.
- No new "diff highlight" between Original and Target values (was a
  rejected alternative).
- No restructure of the global Output panel into per-tab consoles.
- No filtering UX on the Output (click-a-step-to-filter is a
  documented stretch, deliberately out of scope for V2.14.0).

## Design

### 1. Layout

```
TabItem "Project"
└── ScrollViewer
    └── StackPanel
        ├── Grid (2 columns 1:1)
        │   ├── OriginalCard
        │   │   ├── Header "Original"
        │   │   └── VersionAndOptionUserControl
        │   └── TargetCard
        │       ├── Header "Target"
        │       └── VersionAndOptionUserControl
        ├── MigrateBanner (full width)
        │   ├── Checkbox "Overwrite BIA first"
        │   └── Button "MIGRATE" (prominent, primary)
        └── StepperRow
            ├── Header "Or step by step"
            └── MigrationStepperUC (UniformGrid 6 columns)
```

The legacy right-side sidebar (overwrite checkbox + Migrate button +
six stacked step buttons) disappears — the checkbox and Migrate button
move into the `MigrateBanner`, and the six step buttons are replaced
by the stepper cells. The tab content becomes a single column with no
fixed-width column to reserve.

Each panel uses a `ScrollViewer` so dense `VersionAndOptionUserControl`
content fits at 50 / 50 width on a 1920 px screen. A `MinWidth` on the
content `ScrollViewer` triggers horizontal scrolling on screens below
~1280 px rather than squeezing the panels into illegibility.

### 2. MigrationStepperUC

- One `UniformGrid Columns="6"` containing six `MigrationStepCell`
  controls. Each cell shows the step number, status glyph, and label;
  bound to a `MigrationStep` view-model item.
- Connectors between cells (`───▶`) rendered as static glyphs.
- The next-suggested cell (first `Pending` after a contiguous chain of
  `Done`) gets a primary-colored outline halo.

### 3. MigrationStep state machine

```csharp
public enum MigrationStepStatus { Pending, Running, Done, Warning, Failed }

public partial class MigrationStep : ObservableObject
{
    public int Number { get; init; }
    public string Label { get; init; }
    public IAsyncRelayCommand Command { get; init; }
    [ObservableProperty] private MigrationStepStatus status;
    [ObservableProperty] private string? lastMessage;
    public void Reset() => Status = MigrationStepStatus.Pending;
}
```

Transitions:

| From | Event | To |
|------|-------|----|
| Pending | step click | Running |
| Running | normal return | Done |
| Running | return false (Apply Diff) / partial result (Merge has rejected files) | Warning |
| Running | exception (incl. OperationCanceledException) | Failed |
| any | downstream step click resets it | Pending |
| Failed / Warning | step re-clicked | Running |

Mapping to existing methods:

| Step | Method | Status driver |
|------|--------|---------------|
| 1 Generate Only | `MigrateGenerateOnlyRunAsync` | return >= 0 → Done · throw → Failed |
| 2 Open Folder | `MigrateOpenFolder` | instantaneous → Done after click |
| 3 Apply Diff | `MigrateApplyDiffRunAsync` | true → Done · false → Warning · throw → Failed |
| 4 Merge Rejected | `MigrateMergeRejectedRunAsync` | success → Done · `hasNotDeletedFiles` → Warning · throw → Failed |
| 5 Overwrite `\BIA-.*` | `OverwriteBIAFolderAsync` | success → Done · throw → Failed |
| 6 Fix Usings | `parserService.FixUsings` | success → Done · throw → Failed |

The "Migrate" one-shot button keeps the existing `MigrateRunAsync`
behaviour: it sequentially flips each step Running → Done/Warning/
Failed and stops on the first Failed.

Cancellation propagation (introduced in V2.12.1) already converts a
mid-step abort into `OperationCanceledException` → Failed. No new
plumbing required.

### 4. Output color-coding per step

The global `RichTextBox` in `MainWindow.xaml:515` and the existing
`ConsoleWriter` singleton (`BIA.ToolKit/Helper/ConsoleWriter.cs`) keep
their structure. We add:

```csharp
public interface IConsoleWriter
{
    void AddMessageLine(string message, string color = null, bool refreshImmediate = true);
    IDisposable BeginStep(int number, string label);   // NEW
}
```

Mechanics:

- `BeginStep` sets an `AsyncLocal<int?> currentStep`, returns an
  `IDisposable` that clears it on `Dispose()`. `AsyncLocal` is
  ExecutionContext-flow-aware so `await` and `Task.Run` inside the
  scope keep the value.
- The internal `Message` struct grows an `int? StepNumber`. The
  `AddMessageLine` implementation reads `currentStep.Value` when
  capturing the message.
- Rendering: when `StepNumber != null`, the message line is prefixed
  with a colored `▎ ` (U+258E LEFT VERTICAL BAR), color resolved from
  the step's current status via the existing
  `MapColorForDarkTheme` / `MapColorForLightTheme` helpers.
  Visually, consecutive lines of a step batch align into a continuous
  vertical stripe.
- Theme switching reuses the existing `ReRenderMessages()` path.
- When a step transitions Running → Done/Warning/Failed,
  `ReRenderMessages()` is invoked so the stripes update to the final
  color of the step batch.

Color palette additions (both themes):

| Status | Dark theme | Light theme |
|--------|-----------|-------------|
| Running | `#42A5F5` (blue 400) | `#1565C0` (blue 800) |
| Done | `#4CAF50` (green 500) | `DarkGreen` |
| Warning | `#FFA726` (orange 400) | `#CC7000` |
| Failed | `#EF5350` (red 400) | `DarkRed` |
| Pending | n/a (no output emitted while pending) | n/a |

Usage in the migration view-model:

```csharp
private async Task<int> MigrateGenerateOnlyRunAsync(CancellationToken ct = default)
{
    using var stepScope = consoleWriter.BeginStep(1, "Generate Only");
    // existing code — every AddMessageLine inside this scope auto-tags step 1
}
```

No call-site change inside the helpers; only the six wrapping methods
acquire a `BeginStep` scope.

### 5. Impact

#### Files modified

| File | Nature | Approximate volume |
|------|--------|--------------------|
| `BIA.ToolKit/UserControls/ModifyProjectUC.xaml` | Layout rewrite (side-by-side + Migrate banner + stepper row) | ~150 lines on a 110-line file |
| `BIA.ToolKit/ViewModels/ModifyProjectViewModel.cs` | Add `IReadOnlyList<MigrationStep> Steps`, wrap `MigrateXxxRunAsync` with status transitions and `consoleWriter.BeginStep` scopes | +~120 lines |
| `BIA.ToolKit/Helper/ConsoleWriter.cs` | Add `BeginStep` + `StepNumber` on `Message` + stripe rendering + status color palette | +~80 lines |
| `BIA.ToolKit.Application/Helper/IConsoleWriter.cs` | Add `IDisposable BeginStep(int, string)` to interface | +2 lines |

#### New files

| File | Role |
|------|------|
| `BIA.ToolKit/ViewModels/MigrationStep.cs` | View-model class per step |
| `BIA.ToolKit.Domain/Model/MigrationStepStatus.cs` | Status enum |
| `BIA.ToolKit/UserControls/MigrationStepperUC.xaml(.cs)` | Reusable stepper UC |
| `BIA.ToolKit/Converters/StatusToColorConverter.cs` | Status → Brush (theme-aware) |
| `BIA.ToolKit/Converters/StatusToGlyphConverter.cs` | Status → glyph string |

#### Untouched (stability zone)

- `VersionAndOptionUserControl.xaml(.cs)` — embedded as-is in both
  panels.
- `CSharpParserService.FixUsings`, `ProjectCreatorService`,
  `GitService` — already accept `ct` since V2.12.1; the step wrappers
  feed it through.
- Other tabs (CRUD / DTO / Option / Create / Settings) — they call
  `AddMessageLine` without entering a step scope, so their lines have
  `StepNumber == null` and render identically to today.

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| `AsyncLocal` lost across `Task.Run` inside a step | `AsyncLocal` is `ExecutionContext`-flow-aware. Includes a manual test with a step that spawns `Task.Run`. |
| `ReRenderMessages` perf on large output | Already used for theme toggle; observed acceptable. If it bites in practice, a follow-up V2.14.1 can do incremental re-render of only the changing step batch. |
| Stepper width on small screens (<1280 px) | `MinWidth` on the content + horizontal scroll fallback; cell labels use `TextTrimming="CharacterEllipsis"`. |
| Forgetting to reset downstream step status when an upstream step is re-run | `ModifyProjectViewModel.ResetMigrationStepStates` is extended to reset cascade based on step number. Covered by manual test #6. |
| Stepper status badges illegible in light theme | Colors mapped through the existing dark/light helpers; manual test #7 toggles themes. |

## Test plan (manual UI)

1. **Layout**: window 1920 px → panels at 50/50 readable; window
   1280 px → still readable; window 1024 px → horizontal scroll
   appears, no squeeze.
2. **Stepper happy path**: run each step in order, observe transitions
   Pending → Running → Done with the next-step halo following the
   chain.
3. **Stepper failure**: force an error on step 6 (project without
   a `.sln`) → cell state Failed, stripe in output red.
4. **Mid-step cancel**: click Annuler while step 3 is Running →
   cell becomes Failed, stripes in batch turn red.
5. **One-shot Migrate**: click Migrate; all six steps run in
   succession; output shows six distinct color stripes.
6. **Upstream rerun cascade**: complete steps 1-5 (Done) then re-click
   step 3 → steps 4, 5, 6 reset to Pending; step 3 goes Running.
7. **Theme toggle**: switch dark ↔ light; all badges, stripes, and
   glyphs remain legible (acceptance against WCAG AA contrast on the
   palette table).
8. **Other tabs unaffected**: open CRUD / DTO / Option generators,
   trigger a generation, output has no stripes — identical to V2.13.1.

## Out of scope (deferred follow-ups)

- Click-a-step-to-filter-output (UX nice-to-have, requires non-trivial
  `RichTextBox` work).
- Incremental `ReRenderMessages` (perf optimization only worth it if
  measurable).
- "Diff highlight" between Original and Target values (rejected
  alternative C from brainstorming).
