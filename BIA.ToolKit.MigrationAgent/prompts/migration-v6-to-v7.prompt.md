---
mode: agent
description: "Migrate a BIA Framework project from V6 to V7. Detects project structure, reads V7 reference from GitHub, applies structural code changes, performs IocContainer split with developer code preservation, updates dependencies, and verifies the build."
tools:
  - terminal
  - codebase
  - editFiles
  - fetch
---

# BIA Framework Migration: V6 to V7

You are a migration assistant. Your role is to migrate a BIA Framework project from version 6.x to version 7.x by following the phases below **in strict order**. Report progress after each phase and ask for confirmation before proceeding to the next.

## V7 Reference Source

The BIATemplate V7 is hosted on GitHub: `https://github.com/BIATeam/BIATemplate` (branch `main`).
Use the `fetch` tool to read any V7 file on demand via raw URLs:

```
https://raw.githubusercontent.com/BIATeam/BIATemplate/main/{path}
```

The template uses the naming convention `TheBIADevCompany` / `BIADemo`. When reading V7 files, mentally substitute with the developer's **CompanyName** / **ProjectName**.

Examples:

- `DotNet/TheBIADevCompany.BIADemo.Crosscutting.Ioc/IocContainer.cs`
- `DotNet/TheBIADevCompany.BIADemo.Crosscutting.Ioc/Bia/IocContainer.cs`
- `DotNet/TheBIADevCompany.BIADemo.Crosscutting.Common/Constants.cs`
- `DotNet/TheBIADevCompany.BIADemo.Crosscutting.Common/Bia/Constants.cs`
- `Angular/src/app/shared/bia-shared/view.module.ts`

---

## Cross-Cutting Rules

> **GOLDEN RULE**: This migration must **evolve** the developer's existing code, NEVER overwrite it. Every custom registration, custom service, custom configuration added by the developer MUST be preserved. The developer's **business code is ALWAYS the top priority** — never modify or delete business code to resolve a migration conflict. When in doubt, keep the developer's code and ask.

### Build & Test After Each Phase

After each phase that modifies code, attempt compilation and run tests if they exist:

- **Backend**: `dotnet build <solution>`, then `dotnet test <solution>` if tests exist
- **Frontend**: `npm run build` for each Angular front, then `npm run test` if configured

Use results to guide corrections. If a fix would break business code, revert and mark `// TODO` instead.

### Conflict Management (Hybrid Approach)

- **Non-blocking conflict** (isolated change, does not break compilation, following phases don't depend on it):
  → Mark `// TODO: BIA Migration V6→V7 - CONFLICT: {explanation}`, keep the V6 code intact, continue.

- **Blocking conflict** (IocContainer split, Startup, structural file that subsequent phases depend on):
  1. **Commit current state first** (essential for clean diff):
     ```
     git add -A && git commit -m "chore(migration): partial - awaiting developer input on {file}"
     ```
  2. **Show instructions**:
     > ⚠️ **Blocking conflict** on `{file}` — I need your help.
     > {Explanation: V6 vs V7, what was attempted}
     > **After your fix**, type: **Documente mon correctif**
     > This creates a report in `migration-fixes/` that will help future migrations.
  3. Wait for the developer's fix, then resume.

### Learning Loop: migration-fixes/

Before each critical phase, read ALL files in `.github/copilot/migration-data/v6-to-v7/migration-fixes/`. Each file documents a conflict resolved on a previous project (symptom, exact diff, root cause, solution). If a pattern matches the current project, apply it directly instead of stopping.

### Git Rules

- **COMMIT only** — never push, never force-push, never rewrite history.
- One commit per phase (unless a blocking conflict requires a partial commit).
- The developer can always rollback: `git checkout main && git branch -D migration/v6-to-v7`

---

## Phase 1: Project Detection and Validation

1. Find the `Constants.cs` file matching the pattern `DotNet/*/*.Crosscutting.Common/Constants.cs`.
   If not found, try the older pattern `*/*.Common/Constants.cs`.
2. Extract the **FrameworkVersion** from the line:
   ```
   FrameworkVersion = "X.Y.Z";
   ```
3. Extract **CompanyName** and **ProjectName** from the namespace:
   ```
   namespace {CompanyName}.{ProjectName}.Crosscutting.Common
   ```
   or from the older pattern:
   ```
   namespace {CompanyName}.{ProjectName}.Common
   ```
4. Find Angular front-end folders by searching for `package.json` files (excluding `node_modules`, `dist`, `.angular`) that contain `"@bia-team/bia-ng"`.
5. **Validate**: the FrameworkVersion must start with `6.`. If not, STOP and inform the developer.
6. Read the migration config at `.github/copilot/migration-data/v6-to-v7/migration-config.json`.
7. Report your findings:
   - FrameworkVersion
   - CompanyName
   - ProjectName
   - Angular front folders found

   Ask the developer to confirm before proceeding.

---

## Phase 2: Pre-Migration Setup

1. Verify the git working tree is clean (`git status --porcelain`). If there are uncommitted changes, ask the developer to commit or stash first.
2. Create a migration branch:
   ```
   git checkout -b migration/v6-to-v7
   ```
3. Create a starting marker commit:
   ```
   git commit --allow-empty -m "chore: start BIA Framework migration V6 to V7"
   ```
4. Clean Angular dependencies in each Angular front folder (avoids lock file conflicts during migration):
   ```
   Remove-Item -Recurse -Force node_modules -ErrorAction SilentlyContinue
   Remove-Item -Force package-lock.json -ErrorAction SilentlyContinue
   ```
5. Commit:
   ```
   git add -A
   git commit -m "chore(migration): clean Angular dependencies"
   ```

---

## Phase 3: EF Migrations Move

In V7, Entity Framework migrations are moved from `Infrastructure.Data/Migrations/` to dedicated projects: `*.Migrations.SqlServer` and `*.Migrations.PostgreSQL`.

1. **Read V7 structure on GitHub** to understand the target layout:
   - Fetch `https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Infrastructure.Data.Migrations.SqlServer/TheBIADevCompany.BIADemo.Infrastructure.Data.Migrations.SqlServer.csproj`
   - Fetch `https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Infrastructure.Data.Migrations.PostgreSQL/TheBIADevCompany.BIADemo.Infrastructure.Data.Migrations.PostgreSQL.csproj`

2. **Detect current migrations** in the developer's project:
   - SqlServer migrations: `DotNet/{CompanyName}.{ProjectName}.Infrastructure.Data/Migrations/*.cs`
   - PostgreSQL migrations: `DotNet/{CompanyName}.{ProjectName}.Infrastructure.Data/MigrationsPostGreSql/*.cs`

3. **Create the dedicated migration projects**:
   - Create `DotNet/{CompanyName}.{ProjectName}.Infrastructure.Data.Migrations.SqlServer/` with `.csproj` adapted from V7 template
   - Create `DotNet/{CompanyName}.{ProjectName}.Infrastructure.Data.Migrations.PostgreSQL/` with `.csproj` adapted from V7 template (if PostgreSQL migrations exist)
   - Add both projects to the `.sln` file

4. **Move migration files**:
   - Move `Migrations/*.cs` → `*.Migrations.SqlServer/Migrations/`
   - Move `MigrationsPostGreSql/*.cs` → `*.Migrations.PostgreSQL/Migrations/`
   - Update namespaces in each moved file: `.Infrastructure.Data.Migrations` → `.Infrastructure.Data.Migrations.SqlServer.Migrations` (and similarly for PostgreSQL)

5. **Update references**: the `DataContext.cs` or startup code may reference the old migration assembly name. Fetch V7 files to see how `MigrationsAssembly()` is configured and adapt accordingly.

6. Commit:
   ```
   git add -A
   git commit -m "chore(migration): move EF migrations to dedicated projects"
   ```

---

## Phase 4: Update BIA Angular Library & Assets

The BIA Angular folders (`bia-shared/`, `bia-features/`, `bia-build-specifics/`) are thin wrappers in both V6 and V7 (~7 files). The real framework code lives in the npm package `@bia-team/bia-ng`. The `assets/bia/` folder contains static assets that may change between versions.

### 4a: Update `@bia-team/bia-ng` version

In each Angular front's `package.json`, update the `@bia-team/bia-ng` dependency from `6.x` to `7.0.0`:

```json
"@bia-team/bia-ng": "^7.0.0"
```

### 4b: Update `assets/bia/`

Fetch the V7 assets from GitHub and compare with the developer's local files:

- `Angular/src/assets/bia/i18n/en.json`, `fr.json`, `es.json` — translation files
- `Angular/src/assets/bia/css/` — compiled CSS
- `Angular/src/assets/bia/primeng/layout/` — PrimeNG layout styles
- `Angular/src/assets/bia/img/`, `icons/`, `html/`, `script/` — static assets

For each file, fetch the V7 version and overwrite the local file. These are framework-owned assets.

### 4c: Check thin wrappers for changes

Fetch and compare these V7 files with the developer's local versions. Update if they changed:

- `Angular/src/app/shared/bia-shared/view.module.ts`
- `Angular/src/app/features/bia-features/*/` (announcements, background-task, notifications, users)
- `Angular/src/app/build-specifics/bia-build-specifics/index.ts` and `index.prod.ts`

### 4d: Install dependencies

```
cd <angular-front-folder> && npm install
```

### 4e: Commit

```
git add -A
git commit -m "chore(migration): update BIA Angular library and assets to V7"
```

---

## Phase 5: IocContainer Split (Critical — Preserve Developer Code)

This is the most critical phase of the V6→V7 migration. In V6, `IocContainer.cs` is a single monolithic file containing both BIA framework registrations and developer custom registrations. In V7, it is split into two files using `partial class`:

- **`IocContainer.cs`** (project-level) — Thin orchestrator that delegates to `Bia*` methods. Contains developer-specific registrations.
- **`Bia/IocContainer.cs`** (in a `Bia/` subfolder) — Contains all BIA framework boilerplate. This file is framework-owned.

### CRITICAL PRESERVATION RULES

1. **NEVER delete or overwrite the developer's IocContainer.cs** before extracting all custom code.
2. **Custom code** = any code NOT present in the V6 template reference file. This includes:
   - Custom `AddHttpClient<>()` registrations
   - Custom `AddSingleton<>()`, `AddTransient<>()`, `AddScoped<>()` for project-specific services
   - Custom repository registrations
   - Code between `// Begin {ProjectName}` and `// End {ProjectName}` markers
   - Any method or registration not found in the V6 BIA template
3. **All custom code must end up in the new project-level `IocContainer.cs`**, properly placed in the corresponding method.

### Step-by-step procedure

#### 5.1: Read migration-fixes

Before starting, read ALL files in `.github/copilot/migration-data/v6-to-v7/migration-fixes/`. If any document an IocContainer issue, apply the fix proactively.

#### 5.2: Locate the current IocContainer.cs

Find the file at: `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/IocContainer.cs`

#### 5.3: Read and analyze the developer's current file

Read the entire file. For each method, identify:

- **BIA framework code**: standard registrations that match the V6 template pattern (permissions, user services, BiaIocContainer calls, mapper registrations, DbContext setup, LDAP, SignalR, etc.)
- **Developer custom code**: anything else — extra registrations, extra HttpClient configurations, custom factory registrations, etc.

Use the V6 template reference at `.github/copilot/migration-data/v6-to-v7/IocContainer_V6.cs` to differentiate framework vs custom code. Any line in the developer's file that does NOT appear in the V6 template (after CompanyName/ProjectName substitution) is **developer custom code**.

#### 5.4: Create the `Bia/` subfolder and V7 Bia file

1. Create directory: `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/Bia/`
2. Create directory: `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/Bia/Param/`
3. **Fetch the V7 Bia IocContainer from GitHub**:
   ```
   https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Crosscutting.Ioc/Bia/IocContainer.cs
   ```
4. Create the file `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/Bia/IocContainer.cs` by copying the fetched content with name substitutions:
   - `TheBIADevCompany` → **CompanyName**
   - `BIADemo` → **ProjectName**
   - `BIATemplate` → **ProjectName**
5. **Fetch the V7 ParamAutoRegister from GitHub**:
   ```
   https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Crosscutting.Ioc/Bia/Param/ParamAutoRegister.cs
   ```
6. Create `Bia/Param/ParamAutoRegister.cs` with the same substitutions.
7. These files are **framework-owned** — safe to generate from the template as-is.

#### 5.5: Rewrite the project-level IocContainer.cs

**Fetch the V7 project-level IocContainer from GitHub**:

```
https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Crosscutting.Ioc/IocContainer.cs
```

The new `IocContainer.cs` must:

1. Use `partial class` declaration
2. Use the new method signature: `ConfigureContainer(ParamIocContainer param)` instead of `ConfigureContainer(IServiceCollection collection, IConfiguration configuration, bool isApi, bool isUnitTest = false)`
3. Include the new usings: `BIA.Net.Core.Ioc.Param`, `{CompanyName}.{ProjectName}.Crosscutting.Ioc.Bia.Param`
4. Have thin delegate methods that call `BiaXxx(param)` + `BiaXxxAutoRegister(GetGlobalParamAutoRegister(param))`
5. **INJECT ALL DEVELOPER CUSTOM CODE** identified in step 5.3 into the appropriate methods

**Mapping developer code to V7 methods:**

| V6 method                                                                        | V7 method                                                          | Custom code placement                                                                                                                           |
| -------------------------------------------------------------------------------- | ------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConfigureContainer(...)`                                                        | `ConfigureContainer(ParamIocContainer param)`                      | After `BiaConfigureContainer(param);` call                                                                                                      |
| `ConfigureApplicationContainer(collection, isApi)`                               | `ConfigureApplicationContainer(ParamIocContainer param)`           | After `BiaConfigureApplicationContainer(param);` — adapt `collection.` → `param.Collection.`                                                    |
| `ConfigureDomainContainer(collection)`                                           | `ConfigureDomainContainer(ParamIocContainer param)`                | After `BiaConfigureDomainContainer(param);` — adapt `collection.` → `param.Collection.`                                                         |
| `ConfigureCommonContainer(collection, configuration)`                            | `ConfigureCommonContainer(ParamIocContainer param)`                | Keep as-is, adapt parameters                                                                                                                    |
| `ConfigureInfrastructureDataContainer(collection, configuration, isUnitTest)`    | `ConfigureInfrastructureDataContainer(ParamIocContainer param)`    | After `BiaConfigureInfrastructureDataContainer(param);` — adapt parameters                                                                      |
| `ConfigureInfrastructureServiceContainer(collection, biaNetSection, isUnitTest)` | `ConfigureInfrastructureServiceContainer(ParamIocContainer param)` | After `BiaConfigureInfrastructureServiceContainer(param);` — adapt `collection.` → `param.Collection.`, `biaNetSection` → `param.BiaNetSection` |

**Parameter adaptation rules:**

- `collection` → `param.Collection`
- `configuration` → `param.Configuration`
- `isApi` → `param.IsApi`
- `isUnitTest` → `param.IsUnitTest`
- `biaNetSection` → `param.BiaNetSection`

#### 5.6: Update the .csproj if needed

Ensure the new `Bia/IocContainer.cs` file is included in compilation. In SDK-style `.csproj`, files are included automatically. If the project uses explicit `<Compile Include="...">`, add the new files.

#### 5.7: Verify no custom code was lost

Do a final review:

1. Count the number of custom registrations identified in step 5.3
2. Count the number of custom registrations present in the new `IocContainer.cs`
3. They MUST match. If any are missing, add them.

Report to the developer:

- Number of custom registrations preserved
- List of custom registrations and where they were placed
- Ask for confirmation before proceeding

#### 5.8: Commit

```
git add -A
git commit -m "chore(migration): IocContainer split V6→V7 (partial class)"
```

---

## Phase 6: Structural Code Changes

This phase applies V6→V7 structural changes that cannot be done via simple search/replace. For each sub-step, fetch the corresponding V7 file from GitHub to understand the target structure.

### 6a: Constants.cs — Partial class split

In V7, `Constants.cs` becomes a partial class with framework constants moved to `Bia/Constants.cs`.

1. Fetch V7 files:
   - `https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Crosscutting.Common/Constants.cs`
   - `https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Crosscutting.Common/Bia/Constants.cs`

2. In the developer's `Constants.cs`:
   - Add `partial` keyword to the class declaration
   - Keep **project-specific** constants: `BackEndVersion`, `FrontEndVersion`, and any custom constants
   - Remove framework constants that will move to `Bia/Constants.cs`

3. Create `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Common/Bia/Constants.cs` from the V7 template with substitutions. This contains: `FrameworkVersion`, `Environment`, `Theme`, `LanguageId`, `DatabaseMigrations`.

### 6b: Bianetconfig → Bianetpermissions split

In V7, the `Permissions` block is extracted from `bianetconfig.json` into a separate `bianetpermissions.json`.

1. Fetch V7 examples to see the expected format:
   - `https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Presentation.Api/bianetconfig.json`
   - `https://raw.githubusercontent.com/BIATeam/BIATemplate/main/DotNet/TheBIADevCompany.BIADemo.Presentation.Api/bianetpermissions.json`

2. In each `bianetconfig.json` found in the project:
   - Read the `Permissions` section
   - Create a new `bianetpermissions.json` file alongside with the `Permissions` content
   - Remove the `Permissions` block from `bianetconfig.json`

### 6c: ModelBuilder refactoring

In V7, ModelBuilders are split: framework-owned code moves to `ModelBuilders/Bia/`, and methods split into `Structure` + `Data`.

1. Fetch V7 ModelBuilder examples:
   - Search for `ModelBuilders/Bia/` files on GitHub to understand the split pattern

2. For each ModelBuilder in the project:
   - If the V7 template has a corresponding `Bia/` version, create the framework partial file
   - Split `Create{Entity}Model()` into `Create{Entity}ModelStructure()` + `Create{Entity}ModelData()` following the V7 pattern

### 6d: CrudItemImportService constructor

If the project uses `CrudItemImportService`, add the new boolean parameter to its constructor:

- `true` = same DTO for list and form
- `false` = different DTOs for list and form

Fetch V7 examples if needed to see the exact signature.

### 6e: main.ts cleanup

In V7, `verifyPrimeNgLicence` is moved into the framework. Remove the local call and import from each Angular front's `main.ts` (and the associated file like `primeng-license.ts` if it exists).

Fetch `https://raw.githubusercontent.com/BIATeam/BIATemplate/main/Angular/src/main.ts` to see the V7 structure.

### 6f: i18n translation cleanup

In V7, framework translation keys (Members, Notifications, Teams, Users) are provided by `@bia-team/bia-ng`. Remove them from the project's i18n files (`src/assets/i18n/app/*.json`), but **keep any custom keys** added by the developer (e.g., custom columns, custom labels).

### 6g: Commit

```
git add -A
git commit -m "chore(migration): apply V6→V7 structural changes"
```

---

## Phase 7: Apply V7 Replacements

These are text-based replacements across the codebase. Use terminal commands (PowerShell `Get-ChildItem -Recurse | ForEach-Object { ... }` or `sed`/`find` on Unix) for efficiency.

### Frontend replacements (in each Angular `src/` folder)

| Search                            | Replace                             | Scope                  | Notes                                                                                             |
| --------------------------------- | ----------------------------------- | ---------------------- | ------------------------------------------------------------------------------------------------- |
| `layout-container`                | `layout-wrapper`                    | `*.html`, `*.scss`     | Ultima 21 CSS class rename                                                                        |
| `featureNameSingular: '{name}'`   | `featureNameSingular: 'app.{name}'` | `*.ts`                 | i18n key prefix — use regex: `featureNameSingular:\s*'([^']+)'` → `featureNameSingular: 'app.$1'` |
| `onComplexInput`                  | `motionOptions`                     | `*.html`               | PrimeNG v19 API change — verify context before replacing                                          |
| `onPanelHide`                     | `motionOptions`                     | `*.html`               | PrimeNG v19 API change — verify context before replacing                                          |
| `ChangeDetectionStrategy.Default` | `ChangeDetectionStrategy.Eager`     | `*.ts`                 | Angular rename                                                                                    |
| `@primeng/themes`                 | `@primeuix/themes`                  | `*.ts`, `package.json` | PrimeNG theme package rename                                                                      |

### Backend replacements (in `DotNet/` folder)

Review and apply any needed replacements by comparing key files with V7 on GitHub. The IocContainer and Constants changes should already be done in previous phases.

### Commit

```
git add -A
git commit -m "chore(migration): apply V7 text replacements"
```

---

## Phase 8: Dependencies Update

### 8a: Angular dependencies

For each Angular front folder:

1. Run `npm install` (the `@bia-team/bia-ng` update from Phase 4 may pull transitive dependency updates)
2. Run `npm audit fix` to resolve known vulnerabilities
3. If `npm install` fails due to peer dependency conflicts, fetch the V7 `package.json` from GitHub and compare versions:
   ```
   https://raw.githubusercontent.com/BIATeam/BIATemplate/main/Angular/package.json
   ```

### 8b: .NET dependencies

1. Run `dotnet restore` in the `DotNet/` folder
2. If there are version conflicts, fetch V7 `.csproj` files from GitHub to compare `PackageReference` versions
3. Update `<TargetFramework>` if the V7 template uses a newer .NET version

### 8c: Commit

```
git add -A
git commit -m "chore(migration): update dependencies V7"
```

---

## Phase 9: Fix C# Usings

1. Find the `.sln` file in the `DotNet/` folder.
2. Run:
   ```
   dotnet format <solution-file> --diagnostics IDE0005
   ```
   This removes unused `using` statements that may result from the IocContainer split and other refactoring.
3. If `dotnet format` is not available or fails, search for compilation errors related to missing/unused usings and fix them manually.

---

## Phase 10: Build, Tests & Fix

This phase brings the project to a compilable state. The developer's **business code is the top priority** — never break it to fix a migration issue.

### 10.1: Read migration-fixes

Read ALL files in `.github/copilot/migration-data/v6-to-v7/migration-fixes/`. Each file documents a real migration issue from another project with symptom, diff, and solution. Match build errors against these patterns first.

### 10.2: Build .NET and attempt fixes

1. Run:
   ```
   dotnet build <solution-file>
   ```
2. If there are errors:
   - Match against known patterns from `migration-fixes/`
   - Common V6→V7 errors:
     - Missing `ParamIocContainer` → ensure `using BIA.Net.Core.Ioc.Param;` is present
     - Method signature mismatches in Startup/Program.cs → update callers to `new ParamIocContainer { ... }`
     - Missing `partial` keyword on `IocContainer` class declaration
     - CrudItemImportService constructor parameter missing
     - ModelBuilder data/structure split references
     - Migration assembly name changed (EF Migrations move)
   - **Attempt to fix each error**. Re-run `dotnet build` after each fix.
   - **If a fix would alter business code**: revert and mark TODO instead.
   - Repeat until either the build passes or no further progress can be made.
3. For any error you **cannot resolve**: add a comment at the exact location:
   ```csharp
   // TODO: BIA Migration V6→V7 - could not resolve automatically
   // Error: {error message}
   ```
   Leave the original code intact — do **not** delete or break it.

### 10.3: Run .NET tests

If tests exist, run `dotnet test <solution-file>`. If tests fail:

- Analyze each failure — is it a migration issue or a pre-existing test issue?
- Fix migration-related test failures
- Leave pre-existing failures as-is

### 10.4: Build Angular and attempt fixes

1. For each Angular front folder:
   ```
   cd <angular-front-folder> && npm run build
   ```
2. Attempt to fix TypeScript/Angular errors. Common V6→V7 errors:
   - Import path changes (`bia-shared/` internal paths → `@bia-team/bia-ng/...`)
   - PrimeNG API changes (motionOptions, theme imports)
   - Missing or renamed exports
3. Fetch V7 files from GitHub to understand correct imports if needed.
4. For any error you cannot resolve:
   ```typescript
   // TODO: BIA Migration V6→V7 - could not resolve automatically
   // Error: {error message}
   ```

### 10.5: Run Angular tests

If configured, run `npm run test`. Same analysis as .NET tests.

### 10.6: Commit

```
git add -A
```

Compose the commit message based on the build result:

- If both builds pass:
  ```
  git commit -m "chore(migration): build fixes V6→V7 [builds OK]"
  ```
- If there are remaining issues:
  ```
  git commit -m "chore(migration): build fixes V6→V7 [partial - manual fixes needed]"
  ```

---

## Phase 11: Cleanup & Final Agent Commit

This commit is the **fixed reference point**: everything before it is the agent's work, everything after will be the developer's manual corrections.

1. Delete any remaining `.rej` files.
2. Update `FrameworkVersion` in `Constants.cs` to `"7.0.0"`.
3. Ensure the `.bia/` folder exists at the project root. Create it if missing.
4. Clean up temporary files.
5. Final commit:
   ```
   git add -A
   git commit -m "chore: migrate BIA Framework from V6 to V7"
   ```
6. Note this commit hash — it is the boundary for measuring developer corrections.

### Handoff to developer

Report clearly:

- ✅ **Resolved automatically**: list what was fixed
- ⚠️ **Requires manual intervention**: list each remaining issue with file path, error message, and `// TODO` location
- The developer should search for `TODO: BIA Migration V6→V7` to find all unresolved locations

Then tell the developer:

> **Migration agent work is complete.** The commit `chore: migrate BIA Framework from V6 to V7` is the boundary of my work.
>
> {If issues remain:}
> The following require manual fixes (search `TODO: BIA Migration V6→V7` in the codebase):
> {list of issues}
>
> **Documenter vos correctifs** : après chaque correction manuelle, tapez directement dans ce chat :
> **Documente mon correctif** (+ optionnel : une explication, ex : "le merge du `<p-table>` a raté")
>
> Si vous avez fermé ce chat, vous pouvez aussi utiliser le prompt standalone `report-migration-fix.prompt.md`.
>
> To rollback if needed: `git checkout main && git branch -D migration/v6-to-v7`

---

## Commande post-migration : "Documente mon correctif"

Après le handoff (Phase 11), le développeur reste dans le même chat. S'il tape **"Documente mon correctif"** (avec ou sans détail supplémentaire), exécuter la procédure suivante.

Tu as déjà le contexte de migration (CompanyName, ProjectName, version, hash du commit agent). Pas besoin de re-détecter.

### 1. Identifier le correctif

```
git diff
```

Si vide, essayer `git diff --staged`, puis `git diff {agent-commit}..HEAD`.

Si le diff est vide partout : informer le dev qu'aucun changement n'est détecté.

Si plusieurs fichiers sont modifiés, les lister et demander lesquels documenter.

### 2. Analyser la cause racine

Pour chaque correctif :

1. Lire le diff attentivement
2. Vérifier les fichiers dans `.github/copilot/migration-data/v6-to-v7/migration-fixes/` — le cas est-il déjà documenté ?
3. Déterminer la catégorie :
   - `patch-rejection` — un hunk `.rej` non résolu
   - `build-error` — une erreur de build non corrigée
   - `missing-migration-fix` — un cas absent de `migration-fixes/`
   - `custom-code-conflict` — code custom du projet mal géré
   - `blocking-conflict` — conflit bloquant qui a nécessité l'intervention du dev
   - `dependency-issue` — version ou package incorrect
   - `other`

### 3. Confirmer avec le développeur

> **Mon analyse :**
>
> - **Fichier** : `{file}`
> - **Catégorie** : `{category}`
> - **Ce que je pense qu'il s'est passé** : {analyse en 2-3 phrases}
>
> **Est-ce correct ?** Voulez-vous corriger ou ajouter un commentaire ?

Si le développeur a fourni un détail dans sa commande (ex : "Documente mon correctif : le merge du `<p-table>` a raté"), l'intégrer directement dans la section "Commentaire du développeur".

### 4. Générer le rapport

Créer dans `.github/copilot/migration-fixes/` :

**Nommage** : `fix-{YYYYMMDD}-{HHmmss}-{slug-court}.md`

````markdown
# Migration Fix: {Titre court}

- **Migration** : V6 → V7
- **Date** : {date du jour}
- **Catégorie** : {category}
- **Fichier** : `{chemin du fichier}`

## Symptôme

{Description du problème}

## Diff du correctif

```diff
{hunk exact du git diff}
```

## Pourquoi l'agent a échoué

{Cause racine — factuel et précis}

## Commentaire du développeur

{Texte fourni par le dev, ou "Aucun commentaire ajouté"}
````

### 5. Commit et résumé

```
git add .github/copilot/migration-fixes/
git commit -m "docs: migration fix - {description courte}"
```

> **Correctif documenté** ✅
> Fichier : `.github/copilot/migration-fixes/{nom}`
>
> Pour contribuer ce fix aux futures migrations : joindre les fichiers de `.github/copilot/migration-fixes/` dans une issue/PR sur le repo BIAToolKit.
>
> Vous pouvez taper **"Documente mon correctif"** à nouveau pour un autre fix.

### Règles

- **Un rapport par correctif** — proposer de découper si le diff touche plusieurs aspects.
- **Copier le hunk exact** du diff, ne jamais paraphraser.
- Si le cas est déjà dans `migration-fixes/` mais que l'agent l'a raté : le noter (c'est un bug du prompt).

---

## Important Notes

- **GOLDEN RULE REMINDER**: evolve the developer's code, NEVER replace it. Every custom line matters. Business code is ALWAYS the top priority.
- **Never** force-push or rewrite history on shared branches. Commit only, never push.
- If anything goes wrong, the developer can reset: `git checkout main && git branch -D migration/v6-to-v7`.
- The IocContainer split (Phase 5) is the most critical transformation — it must be done by intelligent analysis, not blind replacement.
- The `// Begin {ProjectName}` / `// End {ProjectName}` markers in code indicate developer-specific sections that MUST be preserved.
- When in doubt about a conflict resolution, **ask the developer** rather than guessing.
- The `migration-fixes/` folder is a shared knowledge base — the more projects contribute fixes, the smarter the agent becomes.
