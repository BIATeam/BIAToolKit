# Contrast harmonization & design system — BIA.ToolKit

**Date:** 2026-04-21
**Branch target:** `feature/kiro-poc` (same branch as the CRUD V2 refonte)
**Scope:** WCAG AA contrast compliance across BIA.ToolKit screens + centralized theme tokens (typography / buttons / surfaces)
**Explicitly out of scope:** palette redesign, automated contrast lint, Windows high-contrast mode

## Context

BIA.ToolKit is a WPF .NET 10 desktop app using MaterialDesignThemes 5.3.1, with two themes (light / dark) toggled at runtime via `MainWindow.ApplyTheme`. Multiple screens render text with insufficient contrast against their background, most visibly the primary CTAs ("Create" on the project-creation screen, "Generate" on the DTO / CRUD / Option generators, "Migrate", "Regenerate"): they all render `Foreground="White"` on a Lime `#CDDC39` background in dark mode — ratio ~1.2:1, far below the WCAG AA 4.5:1 requirement for body text.

The existing codebase is mostly clean: colors flow from `DynamicResource MaterialDesign.Brush.*`, which `PaletteHelper.SetPrimaryColor/SetSecondaryColor` refresh on theme switch. Only five hardcoded colors exist and they are justified (orange DEV-mode indicators, amber warning tooltips). The real issues are:

1. **Hardcoded `Foreground="White"`** on CTA buttons overrides MDIX's theme-aware on-color.
2. **Ad-hoc `Opacity` values** (0.5 → 0.75) scattered across UserControls on informative text — visibly inconsistent and several failing WCAG AA.
3. **Duplicated styles** (`SectionCardStyle`, `FeatureTileStyle`, `CardFooterPanelStyle`) redefined per UserControl → every new screen reinvents its own variant.

## Goal

- Every text / background pair in the audited screens meets **WCAG AA** (≥ 4.5:1 for normal text, ≥ 3:1 for text ≥ 18pt or 14pt bold) in **both themes**.
- A centralized set of reusable styles so future screens inherit correct contrast by default.
- Identity preserved: no palette change. Green/Lime in dark, Green/Teal in light stay as-is.

## Architecture

Three new `ResourceDictionary` files, merged into `App.xaml`:

```
BIA.ToolKit/Themes/
  ├── Typography.xaml   — Text.Body / Text.Caption / Text.Subtle / Text.Heading
  ├── Buttons.xaml      — Button.PrimaryAction / SecondaryAction / Ghost / Warning / IconCompact
  └── Surfaces.xaml     — Surface.Card / Section / Footer / Tile
```

**No separate `ColorTokens.xaml`:** colors already come from `MaterialDesign.Brush.*` which auto-refresh via `PaletteHelper`. The fix for CTAs is to use MDIX 5.x's **on-color brushes** (`MaterialDesign.Brush.Primary.Foreground`, `MaterialDesign.Brush.Secondary.Foreground`) which MDIX computes to guarantee readable contrast on primary/secondary-tinted surfaces.

**Fallback plan** if those brushes are absent or unreliable in 5.3.1: define explicit `App.OnPrimaryBrush` / `App.OnSecondaryBrush` in `App.xaml` and update them from `ApplyTheme` at runtime (the exact same mechanism that already sets `Primary` / `Secondary` colors).

## Style catalog

### Typography.xaml

Each style documents its intended role and minimum font size. Opacities track Material Design 3 emphasis levels, chosen so ratios stay above WCAG AA thresholds on the theme background:

| Style | Opacity | Min font size | Use for |
|---|---|---|---|
| `Text.Body` | 1.0 | any | Body content, form values, list items |
| `Text.Heading` | 1.0 | ≥ 18pt, SemiBold | Section titles |
| `Text.Caption` | 0.87 | any (≥ 10pt in practice) | Secondary info, footer labels, hints, small labels |
| `Text.Subtle` | 0.70 | **≥ 14pt required** — 0.70 on < 14pt fails WCAG AA | Non-essential info on large text only |

All styles use `Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"`.

### Buttons.xaml

| Style | Base | Foreground | Use for |
|---|---|---|---|
| `Button.PrimaryAction` | `MaterialDesignRaisedButton` | `...Primary.Foreground` (or fallback `App.OnPrimaryBrush`) | Main CTA per screen |
| `Button.SecondaryAction` | `MaterialDesignRaisedSecondaryButton` | `...Secondary.Foreground` (or fallback) | **Current CTAs (Create, Generate×3, Migrate, Regenerate)** — replaces hardcoded White |
| `Button.Ghost` | `MaterialDesignFlatButton` | Primary | Tertiary actions |
| `Button.Warning` | custom, `#FF9800` bg | Black (`#000000`) — verified ratio ~10:1 | DEV mode, destructive confirm |
| `Button.IconCompact` | `MaterialDesignIconButton` | Primary | Small icon buttons (edit, refresh) |

### Surfaces.xaml

Consolidates the currently duplicated `SectionCardStyle` / `FeatureTileStyle` / `CardFooterPanelStyle` defined in each UserControl. Migration is **opportunistic**: only files touched by the contrast fix migrate in this PR; others migrate later when they are next edited.

## Audit scope

Screens to verify for WCAG AA compliance:

- `MainWindow.xaml` — title bar, version badge, DEV diagnostics
- `VersionAndOptionUserControl.xaml` — partially fixed in commit `63241fc`; verify only
- `ModifyProjectUC.xaml` — tab bar, description zone
- `CRUDGeneratorUC.xaml` — field help, feature descriptions, tooltips
- `DtoGeneratorUC.xaml` — "Source type" / "Mapping type" labels, empty states
- `OptionGeneratorUC.xaml` — recently refactored; spot-check
- `GenerateUC.xaml` — empty states, overlays
- `RegenerateFeaturesUC.xaml` — feature list
- `CompanyFilesDialogUC.xaml` — section subtitles
- `LogDetailUC.xaml` — already uses `.Brush.Foreground`; verify

For each screen, the implementation plan will produce a short ratio table:

| UI element | Text color | Background | Ratio before (dark / light) | Ratio after | Action |

## Known failures to fix

Identified during the audit agent pass:

| # | File | Element | Issue | Estimated ratio (dark) | Action |
|---|---|---|---|---|---|
| 1 | 6× CTAs | `Foreground="White"` on `MaterialDesignRaisedSecondaryButton` | Lime `#CDDC39` bg + white text | ~1.2:1 | Migrate to `Button.SecondaryAction` |
| 2 | GenerateUC.xaml | Info text (l. 59, 75, 91, 128) | `Opacity="0.6–0.7"` | ~3.2:1 | `Text.Caption` (`Text.Subtle` only if actual size ≥ 14pt) |
| 3 | ModifyProjectUC.xaml | Tab meta text (l. 103) | `Opacity="0.7"` | ~3.2:1 | `Text.Caption` unless the element is ≥ 14pt |
| 4 | DtoGeneratorUC.xaml | "Source type" / "Mapping type" labels (l. 549, 551, `FontSize="10"`) | `Opacity="0.5–0.65"` | < 3:1 | `Text.Caption` (keeps FontSize=10) |
| 5 | CRUDGeneratorUC.xaml | Field help (l. 877, 881, 1071, 1095) | `Opacity="0.65, 0.75"` | ~3.5:1 | `Text.Caption` |
| 6 | CompanyFilesDialogUC.xaml | Section subtitle | `Opacity="0.75"` | borderline | `Text.Caption` |
| 7 | MainWindow.xaml | Version badge (l. 471, `FontSize="11"`) | `Opacity="0.6"` | ~3.2:1 | `Text.Caption` (11pt keeps opacity 0.87 — `Text.Subtle` requires ≥ 14pt) |

## Execution plan

One branch (`feature/kiro-poc`), three commits:

**Commit 1 — `feat(theme): add centralized typography, button, and surface styles`**
- Create `BIA.ToolKit/Themes/Typography.xaml`, `Buttons.xaml`, `Surfaces.xaml`.
- Merge into `App.xaml` `Application.Resources.ResourceDictionary.MergedDictionaries`.
- **Verify** `MaterialDesign.Brush.Secondary.Foreground` exists and auto-refreshes when `SetSecondaryColor` is called. If not, implement the `App.OnPrimaryBrush` / `App.OnSecondaryBrush` fallback driven by `ApplyTheme`.
- No existing XAML modified. Build must stay green.

**Commit 2 — `fix(a11y): CTA buttons use on-color foreground (WCAG AA)`**
- Replace the 6 hardcoded-white CTA foregrounds by `Style="{StaticResource Button.SecondaryAction}"`.
- Manual test: toggle light ↔ dark. Verify readability on each of the 6 buttons.
- Highest visible impact commit.

**Commit 3 — `fix(a11y): migrate informative text to centralized typography styles`**
- Apply `Text.Caption` / `Text.Subtle` on the 6 failing cases (#2 to #7) above.
- Opportunistically migrate `Surface.Section` / `Surface.Card` where the same file is being edited.
- Each edited file gets a ratio-before/after table recorded in the implementation plan.

## Risks & mitigations

1. **`...Secondary.Foreground` missing in MDIX 5.3.1 or does not refresh on `SetSecondaryColor`.**
   → Verified in Commit 1 before any migration. Fallback is the explicit `App.OnSecondaryBrush` pattern updated by `ApplyTheme`.
2. **Opacity bumps may subtly change visual weight of certain labels.**
   → Addressed by manual visual pass after Commit 3; if a label reads too heavy, fall back to `Text.Subtle` (0.70) at ≥ 14pt.
3. **Consolidating duplicated card styles risks layout regression on migrated files.**
   → Migrate only files being edited in Commit 3; keep duplicates elsewhere untouched.

## Verification

- Manual theme toggle (light ↔ dark) — CTAs and audited screens render correctly both ways.
- Spot-check ratios on the 7 listed failures using the Windows Color & Contrast Analyzer or the ratio formula (L1+0.05) / (L2+0.05).
- `dotnet build BIA.ToolKit/BIA.ToolKit.csproj` stays green on every commit.
- If `.g.cs` errors appear after XAML edits: `rm -rf BIA.ToolKit/obj && dotnet clean && dotnet build` (known build quirk).

## Explicitly out of scope

- Palette redesign (confirmed by user)
- Automated contrast lint / CI rule
- Windows high-contrast mode support
- Full migration of all XAML files to centralized `Surfaces.xaml` (opportunistic only)
- Font family / icon set changes
