# Project selector hint & banner separation — Design

**Status**: Approved (brainstorming) — pending implementation plan
**Date**: 2026-07-07

## Context

At startup, before any project is selected, `GenerateUC` and
`ModifyProjectUC` each show a large centered empty-state overlay
("Select a project to begin" + explanatory caption) covering the
disabled tab content.

User feedback: this overlay is confusing. Users read it as a tooltip
belonging to the icon it's centered under, and try to click *on the
overlay itself* rather than on the actual controls that let them pick
a project — the Root Path field / Browse button and the Project
Folder combo box, which live in the shared `ProjectSelectorUC` banner
above the tab content, not in the tab content area.

Separately, the `ProjectSelectorUC` banner (Root Path, Project
Folder, read-only project info) currently sits flush with the page
content below it, with no visual separation, making it easy to miss
that it is a distinct, persistent control strip rather than part of
the page.

## Goals

- Remove the large empty-state overlay from `GenerateUC` and
  `ModifyProjectUC`. When no project is selected, the tab content
  simply stays dimmed/disabled (`Opacity=0.35`, `IsEnabled=False`),
  as already implemented — no replacement text/icon in that area.
- Add a single, unobtrusive hint anchored to the actual controls the
  user must use (Root Path + Project Folder, inside
  `ProjectSelectorUC`), so the guidance sits next to the action
  instead of over unrelated disabled content.
- Visually separate the `ProjectSelectorUC` banner from the page
  content beneath it, so it reads as a persistent top strip rather
  than part of the scrolling page.

## Non-goals

- No change to project-loading logic, `ProjectViewModel`, or the
  `IsProjectSelected` computation.
- No first-run/onboarding "seen once" persistence — the hint is purely
  derived from current state (`IsProjectSelected == false`), it is not
  a dismissible tutorial with saved state.
- No changes to `ModifyProjectViewModel`/`GenerateUC` tab-enablement
  logic beyond removing the overlay markup.

## Design

### 1. Remove the empty-state overlay

In `GenerateUC.xaml` and `ModifyProjectUC.xaml`, delete the `Border`
block commented `<!-- Empty-state overlay: shown only when no project
is selected -->` (icon + "Select a project to begin" + caption), in
both files. The `TabControl` opacity/enable triggers driven by
`IsProjectSelected` are untouched — that dimming is sufficient
feedback that the section is locked.

### 2. Coachmark hint in `ProjectSelectorUC`

`ProjectSelectorUC.xaml`'s root `Grid` (currently a flat 3-column
row: Root Path | Project Folder | Read-only info) gains a second row
that hosts a coachmark-style callout, visible only while
`IsProjectSelected == false` (bound directly to the existing property
exposed on `ProjectViewModel`, the control's `DataContext` — no new
property needed).

Layout:
- Row 0: existing 3-column content, unchanged.
- Row 1: the callout, `Grid.ColumnSpan` restricted to columns 0–1
  (Root Path + Project Folder) — explicitly excluding column 2
  (read-only project info), so it doesn't visually implicate a
  control that isn't part of the selection action. One single callout
  covers both actions (choosing the parent folder and picking the
  project) rather than two separate hints, per explicit request for
  "unique and harmonious" placement.

Callout content: a small upward-pointing arrow (`Path`, primary-brush
fill) flush against the top edge, roughly centered over the boundary
between the two columns it spans, feeding into a rounded banner:
- Icon: `HelpCircleOutline` (MaterialDesign PackIcon), 16×16.
- Text: "Select a project to begin" (bold, same weight as current
  overlay title) followed by a lighter caption: "Choose a parent
  folder, then pick a project from the list above."
- Not hit-test blocking — `IsHitTestVisible="False"` on the whole
  callout, so it never intercepts clicks (unlike a real floating
  `ToolTip`/popup, which risks stacking/positioning issues here; a
  static inline element anchored in the layout is simpler and always
  correctly positioned relative to the two columns it annotates).

Visibility: same `Style.Triggers`/`DataTrigger` pattern already used
for the overlay being removed — `Border.Style` with a `DataTrigger
Binding="{Binding IsProjectSelected}" Value="False"` setting
`Visibility="Visible"` (default `Collapsed`). No new converter
needed.

Styling: reuses the visual language of `Tooltip.Hint` (Card.Background
fill, Primary-brush 1px border, soft drop shadow) defined in
`Themes/Surfaces.xaml`, but as a static `Border`, not a WPF `ToolTip`
— it must remain visible without hover, matching the "always visible
while unselected" requirement.

### 3. Visually separate the banner from page content

In `MainWindow.xaml`, wrap `SharedProjectSelector` in a `Border` using
a new shared style `Surface.Banner` (added to `Themes/Surfaces.xaml`,
alongside `Surface.Section`/`Surface.TintedPanel`):

- `Background`: `MaterialDesign.Brush.Card.Background` (slightly
  distinct from the page's plain background, consistent with other
  surfaces).
- `BorderBrush`: `MaterialDesign.Brush.Card.Border`, `BorderThickness`
  bottom-only (`0,0,0,1`) — a clean separating line rather than a
  boxed card, since the banner spans the full content width and
  isn't a discrete "card".
- `Effect`: `DropShadowEffect` (`BlurRadius=8`, `ShadowDepth=2`,
  `Direction=270` i.e. downward, `Opacity=0.18`, `Color=Black`) —
  same family of values already used in
  `VersionAndOptionUserControl.xaml`, giving a subtle elevation that
  reads as "this strip floats above the content below it".

The margin currently applied inline to `SharedProjectSelector`
(`Margin="10"`) moves to interior padding on the new wrapping
`Border` (`Padding="10"`) so the shadow/border reach the full content
width while the inner controls keep their existing spacing.

## Testing

Manual verification (WPF, no automated UI test harness in this
project):
1. Launch app, navigate to Migration or Generation with no project
   selected: confirm no large centered overlay appears; tabs are
   dimmed as before.
2. Confirm the new callout appears under the Root Path + Project
   Folder columns only (not under the read-only info column), with
   the up-arrow, icon, title and caption legible in both light and
   dark themes.
3. Select a project: callout disappears immediately, tabs re-enable.
4. Confirm the `ProjectSelectorUC` banner now has a visible bottom
   border + drop shadow separating it from the page content in both
   Migration and Generation pages, in both themes.
5. `dotnet-build` passes with no XAML binding errors in output.
