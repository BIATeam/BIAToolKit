---
mode: agent
description: "Migrate a BIA Framework project from V6 to V7. Detects project structure, applies code changes, performs IocContainer split with developer code preservation, resolves conflicts, updates dependencies, and verifies the build."
tools:
  - terminal
  - codebase
  - editFiles
---

# BIA Framework Migration: V6 to V7

You are a migration assistant. Your role is to migrate a BIA Framework project from version 6.x to version 7.x by following the phases below **in strict order**. Report progress after each phase and ask for confirmation before proceeding to the next.

> **GOLDEN RULE**: This migration must **evolve** the developer's existing code, NEVER overwrite it. Every custom registration, custom service, custom configuration added by the developer MUST be preserved. When in doubt, keep the developer's code and ask.

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
6. Report your findings:
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

---

## Phase 3: Apply Migration Patch

1. Read the migration config at `.github/copilot/migration-data/v6-to-v7/migration-config.json`.
2. Read the patch file at `.github/copilot/migration-data/v6-to-v7/migration.patch` (if it exists).
3. The patch uses the template naming convention (`TheBIADevCompany` / `BIADemo`). Create an adapted copy with substitutions:
   - Replace `TheBIADevCompany` with the detected **CompanyName** (case-sensitive)
   - Replace `BIADemo` with the detected **ProjectName** (case-sensitive)
   - Replace `thebiadevcompany` with **CompanyName** in lowercase
   - Replace `biademo` with **ProjectName** in lowercase
   - If Angular front folders have custom names (not `Angular`), also replace `Angular` folder references accordingly.
4. Save the adapted patch to a temporary file and apply:
   ```
   git apply --reject --whitespace=fix <adapted-patch-file>
   ```
   Exit code 1 with `.rej` files is **normal** and expected.
5. Check the patch for lines with `deleted file mode`. If those files still exist in the project, list them for the developer to review.
6. Also read `.github/copilot/migration-data/v6-to-v7/files-to-delete.json` (if it exists). For each listed file (after name substitution), check if it exists and delete it.

> **Note**: If no `migration.patch` file is available, skip this phase and proceed. The structural changes (Phase 4) and dependency updates (Phase 6) will handle the core migration.

7. Commit the patch result:
   ```
   git add -A
   git commit -m "chore(migration): apply V6→V7 patch"
   ```
   (Even if there are `.rej` files — they are committed too, Phase 6 will resolve them.)

---

## Phase 4: IocContainer Split (Critical — Preserve Developer Code)

This is the most critical phase of the V6→V7 migration. In V6, `IocContainer.cs` is a single monolithic file containing both BIA framework registrations and developer custom registrations. In V7, it is split into two files using `partial class`:

- **`IocContainer.cs`** (project-level) — Thin orchestrator that delegates to `Bia*` methods. Contains developer-specific registrations.
- **`Bia/IocContainer.cs`** (in a `Bia/` subfolder) — Contains all BIA framework boilerplate. This file is framework-owned and can be generated from the V7 template.

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

#### 4.1: Locate the current IocContainer.cs

Find the file at: `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/IocContainer.cs`

#### 4.2: Read and analyze the developer's current file

Read the entire file. For each method, identify:

- **BIA framework code**: standard registrations that match the V6 template pattern (permissions, user services, BiaIocContainer calls, mapper registrations, DbContext setup, LDAP, SignalR, etc.)
- **Developer custom code**: anything else — extra registrations, extra HttpClient configurations, custom factory registrations, etc.

Use the V6 template reference at `.github/copilot/migration-data/v6-to-v7/IocContainer_V6.cs` to differentiate framework vs custom code. Any line in the developer's file that does NOT appear in the V6 template (after CompanyName/ProjectName substitution) is **developer custom code**.

#### 4.3: Create the `Bia/` subfolder and V7 Bia file

1. Create directory: `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/Bia/`
2. Create directory: `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/Bia/Param/`
3. Read the V7 Bia template at `.github/copilot/migration-data/v6-to-v7/IocContainer_v7_Bia.cs`.
4. Create the file `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/Bia/IocContainer.cs` by copying the template with name substitutions:
   - `TheBIADevCompany` → **CompanyName**
   - `BIADemo` → **ProjectName**
5. This file is **framework-owned** — it is safe to generate it from the template as-is.

#### 4.4: Rewrite the project-level IocContainer.cs

Read the V7 project-level template at `.github/copilot/migration-data/v6-to-v7/IocContainer_V7.cs`.

The new `IocContainer.cs` must:

1. Use `partial class` declaration
2. Use the new method signature: `ConfigureContainer(ParamIocContainer param)` instead of `ConfigureContainer(IServiceCollection collection, IConfiguration configuration, bool isApi, bool isUnitTest = false)`
3. Include the new usings: `BIA.Net.Core.Ioc.Param`, `{CompanyName}.{ProjectName}.Crosscutting.Ioc.Bia.Param`
4. Have thin delegate methods that call `BiaXxx(param)` + `BiaXxxAutoRegister(GetGlobalParamAutoRegister(param))`
5. **INJECT ALL DEVELOPER CUSTOM CODE** identified in step 4.2 into the appropriate methods

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

#### 4.5: Update the .csproj if needed

Ensure the new `Bia/IocContainer.cs` file is included in compilation. In SDK-style `.csproj`, files are included automatically. If the project uses explicit `<Compile Include="...">`, add the new file.

#### 4.6: Verify no custom code was lost

Do a final review:

1. Count the number of custom registrations identified in step 4.2
2. Count the number of custom registrations present in the new `IocContainer.cs`
3. They MUST match. If any are missing, add them.

Report to the developer:

- Number of custom registrations preserved
- List of custom registrations and where they were placed
- Ask for confirmation before proceeding

7. Commit the IocContainer split:
   ```
   git add -A
   git commit -m "chore(migration): IocContainer split V6→V7 (partial class)"
   ```

---

## Phase 5: Overwrite BIA Library Folders

1. Find all directories matching the pattern `bia-*` in the Angular front folders (e.g., `Angular/src/app/bia-*`, `Angular/src/assets/bia-*`).
2. These folders contain framework library code that must be replaced wholesale from the target version.
3. If BIA folder packages are available in `.github/copilot/migration-data/v6-to-v7/bia-packages/`, copy them over the existing folders.
4. If not available, note this step as **manual**: the developer should use the BIAToolKit GUI (step 5 - "Overwrite BIA") or obtain the V7 BIA folders from the BIA team.

---

## Phase 6: Resolve Rejected Hunks (.rej files)

0. **Read migration-fixes**: before resolving any `.rej` file, read ALL files in `.github/copilot/migration-data/v6-to-v7/migration-fixes/`. Each file documents a real migration issue with its exact diff, root cause, and solution. Use these as reference when resolving similar patterns in `.rej` files.

1. Find all `.rej` files in the project:

   ```
   Get-ChildItem -Path . -Filter "*.rej" -Recurse -File
   ```

   (or `find . -name "*.rej" -type f` on Unix)

2. For **each** `.rej` file:
   a. Read the `.rej` file. Each hunk shows:
   - Lines prefixed with `-`: the expected "before" state
   - Lines prefixed with `+`: the desired "after" state
     b. Read the corresponding source file (`.rej` filename without the `.rej` extension).
     c. The source file may differ from the "before" state because the developer customized it. Your task is to **understand the intent** of the change and apply it while **preserving the developer's customizations**.
     d. Common patterns:
   - **Namespace changes**: apply the rename
   - **Method signature changes**: update signature, keep custom parameters
   - **Import/using additions**: add the new imports
   - **Configuration changes**: update to new values
   - **API changes**: adapt the implementation
   - **Parameter object migration**: `(collection, config, ...)` → `(ParamXxx param)` — this is part of the V6→V7 pattern
     e. After successfully applying the changes, **delete the `.rej` file**.

3. If a hunk cannot be resolved automatically, insert a `// TODO: BIA Migration V6→V7 - manual review needed` comment at the relevant location and keep the `.rej` file.

4. Report:
   - How many `.rej` files were resolved
   - How many remain for manual review (with file paths)

5. Commit the .rej resolution results:
   ```
   git add -A
   git commit -m "chore(migration): resolve .rej files (${resolved} resolved, ${remaining} remaining)"
   ```

---

## Phase 7: Update Dependencies

### 7a: .NET Dependencies

1. Read `.github/copilot/migration-data/v6-to-v7/dotnet-dependencies.json` (if it exists).
2. For each `.csproj` file in the `DotNet/` folder:
   - Update the `<TargetFramework>` if specified in the migration data.
   - Update `<PackageReference>` versions for each listed package.
3. Run `dotnet restore` in the `DotNet/` folder to verify packages resolve.

### 7b: Angular Dependencies

1. Read `.github/copilot/migration-data/v6-to-v7/angular-dependencies.json` (if it exists).
2. For each Angular front folder detected in Phase 1:
   - Update `package.json` with the new dependency versions.
   - Delete `node_modules/` and `package-lock.json`.
   - Run `npm install`.

---

## Phase 8: Fix C# Usings

1. Find the `.sln` file in the `DotNet/` folder.
2. Run:
   ```
   dotnet format <solution-file> --diagnostics IDE0005
   ```
   This removes unused `using` statements that may result from the IocContainer split and other refactoring.
3. If `dotnet format` is not available or fails, search for compilation errors related to missing/unused usings and fix them manually.

---

## Phase 9: Post-Migration Cleanup

1. Delete any remaining `.rej` files (those not already handled).
2. Delete `package-lock.json` in all Angular front folders (will be regenerated).
3. Update the `FrameworkVersion` in `Constants.cs` to `"7.0.0"`.
4. Ensure the `.bia/` folder exists at the project root. Create it if missing.
5. Clean up any temporary files created during migration (adapted patches, etc.).
6. Commit dependencies + cleanup:
   ```
   git add -A
   git commit -m "chore(migration): update dependencies and cleanup V6→V7"
   ```

> The next commit (Phase 10) will be the **final agent commit** — the boundary for measuring developer corrections.

---

## Phase 10: Verification and Agent Commit

This phase tries to bring the project to a compilable state, then **commits everything** — including fixes and any remaining issues — so that the developer's subsequent manual corrections can be measured precisely via `git diff`.

### 10.1: Build .NET and attempt fixes

1. Run:
   ```
   dotnet build <solution-file>
   ```
2. If there are errors:
   - Read ALL files in `.github/copilot/migration-data/v6-to-v7/migration-fixes/` — each file documents a real migration issue encountered on other projects, with the exact symptom, diff, and solution. Match build errors against these known patterns.
   - Common V6→V7 errors:
     - Missing `ParamIocContainer` → ensure `using BIA.Net.Core.Ioc.Param;` is present
     - Method signature mismatches in Startup/Program.cs → update callers to `new ParamIocContainer { ... }`
     - Missing `partial` keyword on `IocContainer` class declaration
   - **Attempt to fix each error**. Re-run `dotnet build` after each fix.
   - Repeat until either the build passes or no further progress can be made.
3. For any error you **cannot resolve**: add a comment in the relevant file at the exact location:
   ```csharp
   // TODO: BIA Migration V6→V7 - could not resolve automatically
   // Error: {error message}
   // File: {source file}, Line: {line}
   ```
   Leave the original code intact — do **not** delete or break it. The developer needs to see what was there.

### 10.2: Build Angular and attempt fixes

1. For each Angular front folder:
   ```
   cd <angular-front-folder> && npm run build
   ```
2. Attempt to fix TypeScript/Angular compilation errors.
3. For any error you cannot resolve: add a comment:
   ```typescript
   // TODO: BIA Migration V6→V7 - could not resolve automatically
   // Error: {error message}
   ```
   Leave the original code intact.

### 10.3: Agent commit (boundary for developer diff)

This commit is the **fixed reference point**: everything before it is the agent's work, everything after will be the developer's manual corrections.

1. Stage all changes (build fixes and/or `// TODO` markers):
   ```
   git add -A
   ```
2. Compose the commit message based on the build result:
   - If both builds pass:
     ```
     git commit -m "chore: migrate BIA Framework from V6 to V7 [builds OK]"
     ```
   - If there are remaining issues:
     ```
     git commit -m "chore: migrate BIA Framework from V6 to V7 [partial - manual fixes needed]"
     ```
3. This is the **final agent commit**. Note its hash — it servira de frontière pour les rapports de correctifs.

   The full commit history on the migration branch will look like:

   ```
   chore: migrate BIA Framework from V6 to V7 [...]  ← final agent commit
   chore(migration): update dependencies and cleanup V6→V7
   chore(migration): resolve .rej files (X resolved, Y remaining)
   chore(migration): IocContainer split V6→V7 (partial class)
   chore(migration): apply V6→V7 patch
   chore: start BIA Framework migration V6 to V7
   ```

### 10.4: Handoff to developer

Report clearly:

- ✅ **Resolved automatically**: list what was fixed
- ⚠️ **Requires manual intervention**: list each remaining issue with file path, error message, and location of the `// TODO: BIA Migration` comment
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

Après le handoff (Phase 10), le développeur reste dans le même chat. S'il tape **"Documente mon correctif"** (avec ou sans détail supplémentaire), exécuter la procédure suivante.

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
> Pour contribuer : joindre les fichiers de `.github/copilot/migration-fixes/` dans une issue/PR sur BIAToolKit.
>
> Vous pouvez taper **"Documente mon correctif"** à nouveau pour un autre fix.

### Règles

- **Un rapport par correctif** — proposer de découper si le diff touche plusieurs aspects.
- **Copier le hunk exact** du diff, ne jamais paraphraser.
- Si le cas est déjà dans `migration-fixes/` mais que l'agent l'a raté : le noter (c'est un bug du prompt).

---

## Important Notes

- **GOLDEN RULE REMINDER**: evolve the developer's code, NEVER replace it. Every custom line matters.
- **Never** force-push or rewrite history on shared branches.
- If anything goes wrong, the developer can reset: `git checkout main && git branch -D migration/v6-to-v7`.
- The migration patch was generated from a standard BIA template project. Customizations specific to the developer's project may cause additional `.rej` files — this is expected.
- The IocContainer split (Phase 4) cannot rely on `.rej` files because the transformation is structural (1 file → 2 files). It must be done by intelligent analysis.
- When in doubt about a conflict resolution, **ask the developer** rather than guessing.
- The `// Begin {ProjectName}` / `// End {ProjectName}` markers in code indicate developer-specific sections that MUST be preserved.

```

```
