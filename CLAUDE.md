# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

```bash
# Restore (includes .NET Framework 4.8 packages.config project)
msbuild /t:Restore /p:RestorePackagesConfig=true

# Build
dotnet build

# Publish (release)
dotnet publish BIA.ToolKit/BIA.ToolKit.csproj -c Release -r win-x64 --self-contained false
dotnet publish BIA.ToolKit.Updater/BIA.ToolKit.Updater.csproj -c Release -r win-x64 --self-contained false

# Run tests (xUnit v3, requires BIADemoVersions/ zips extracted)
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~GenerateCrudTest_7_0_0.GenerateAllCrudFiles"

# Format verification
dotnet format style --severity info --verify-no-changes
dotnet format analyzers --severity info --verify-no-changes

# Auto-fix formatting
dotnet format style --severity info
dotnet format analyzers --severity info
```

## Known Build Issues

- After XAML modifications, build may fail with ".g.cs not found" errors. Fix: `rm -rf BIA.ToolKit/obj && dotnet clean && dotnet build`
- `dotnet format style` crashes on IDE0130 at solution level. Use `--exclude-diagnostics IDE0130` if needed.
- Analyzer fixes can introduce new style violations — always re-run style check after running analyzer fixes.

## Architecture

### Solution Structure (Layered)

```
BIA.ToolKit                  → WPF desktop app (.NET 10, entry point)
  ↓
BIA.ToolKit.Application      → Business logic, services, ViewModels (.NET 10)
  ↓
BIA.ToolKit.Application.Templates → T4 code generation templates (.NET Framework 4.8)
BIA.ToolKit.Domain           → Domain models, interfaces (.NET 10)
BIA.ToolKit.Common           → Shared utilities (.NET 10)

BIA.ToolKit.Updater          → Self-update console app (.NET 10)
BIA.ToolKit.Test.Templates   → xUnit v3 generation tests (.NET 10)
```

### The .NET Framework 4.8 Project

**BIA.ToolKit.Application.Templates** targets .NET Framework 4.8, not .NET 10. It uses `packages.config` (not PackageReference). The CI restore uses `msbuild /t:Restore /p:RestorePackagesConfig=true` for this reason.

### Dependency Injection

All services are registered as singletons in `App.xaml.cs` using `Microsoft.Extensions.DependencyInjection`. Most are registered as concrete types (not interfaces), except `IConsoleWriter`.

### Key Services (Application Layer)

- **FileGeneratorService** — Main T4-based code generator for Dto/Option/CRUD features. Uses versioned model providers (4.0.0 through 7.0.0) with Mono.TextTemplating.
- **CSharpParserService** — Opens MSBuild solutions via Roslyn to extract class/entity metadata for generation.
- **ProjectCreatorService** — Creates new projects from BIA templates with namespace renaming, feature selection, and company file overlay.
- **RepositoryService** / **GitService** — Clone/sync repositories and download releases (LibGit2Sharp + Octokit).
- **GenerateCrudService** — Legacy generation service (marker-based extraction from zip archives).
- **UpdateService** — Auto-update via GitHub releases.

### T4 Template System

Templates live in `BIA.ToolKit.Application.Templates/` organized by BIA framework version (`_4_0_0/` through `_7_0_0/`). Each version has a `manifest.json` declaring template files, output path patterns (with `{Project}`, `{Entity}`, `{Domain}` placeholders), and partial insertion markers. Templates receive a `ModelInstance` object from version-specific model providers.

### UI Structure

Main WPF window (`MainWindow.xaml.cs`) orchestrates tabbed UI screens via UserControls: project creation (`VersionAndOptionUserControl`), project modification (`ModifyProjectUC` → `CRUDGeneratorUC`, `DtoGeneratorUC`, `OptionGeneratorUC`, `RegenerateFeaturesUC`), file generation (`GenerateUC`), and settings.

### Project Version Discovery

BIA framework projects contain a `Bia/Constants.cs` file with `FrameworkVersion`. The tool discovers this via `NamesAndVersionResolver` (regex-based) to determine which template set to use. `BiaFrameworkVersion` supports wildcard matching (e.g., `"5.*.*"`).

### Test Architecture

Tests generate code using `FileGeneratorService`, then compare output against reference BIADemo projects (zipped in `BIADemoVersions/`). Test fixtures per version (`_5_0_0`, `_6_0_0`, `_7_0_0`) extract reference zips, create temp output directories, and use DiffPlex for file comparison. Tests are disabled for parallelization within each version collection.

## Branching Model

Gitflow: `main` for releases, `develop` for integration, `feature/`, `release/`, `hotfix/` branches. Version tags use `V` prefix (e.g., `V2.10.2`).
