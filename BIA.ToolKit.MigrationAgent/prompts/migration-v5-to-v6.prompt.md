---
mode: agent
description: "Migrate a BIA Framework project from V5 to V6. Detects project structure, applies code changes, resolves conflicts, updates dependencies, and verifies the build."
tools:
  - terminal
  - codebase
  - editFiles
---

# BIA Framework Migration: V5 to V6

You are a migration assistant. Your role is to migrate a BIA Framework project from version 5.x to version 6.x by following the phases below **in strict order**. Report progress after each phase and ask for confirmation before proceeding to the next.

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
5. **Validate**: the FrameworkVersion must start with `5.`. If not, STOP and inform the developer.
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
   git checkout -b migration/v5-to-v6
   ```
3. Create a starting marker commit:
   ```
   git commit --allow-empty -m "chore: start BIA Framework migration V5 to V6"
   ```

---

## Phase 3: Apply Migration Patch

1. Read the migration config at `.github/copilot/migration-data/v5-to-v6/migration-config.json`.
2. Read the patch file at `.github/copilot/migration-data/v5-to-v6/migration.patch`.
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
6. Also read `.github/copilot/migration-data/v5-to-v6/files-to-delete.json`. For each listed file (after name substitution), check if it exists and delete it.

---

## Phase 4: Overwrite BIA Library Folders

1. Find all directories matching the pattern `bia-*` in the Angular front folders (e.g., `Angular/src/app/bia-*`, `Angular/src/assets/bia-*`).
2. These folders contain framework library code that must be replaced wholesale from the target version.
3. If BIA folder packages are available in `.github/copilot/migration-data/v5-to-v6/bia-packages/`, copy them over the existing folders.
4. If not available, note this step as **manual**: the developer should use the BIAToolKit GUI (step 5 - "Overwrite BIA") or obtain the V6 BIA folders from the BIA team.

---

## Phase 5: Resolve Rejected Hunks (.rej files)

1. Find all `.rej` files in the project:
   ```
   find . -name "*.rej" -type f
   ```
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
     e. After successfully applying the changes, **delete the `.rej` file**.
3. If a hunk cannot be resolved automatically, insert a `// TODO: BIA Migration - manual review needed` comment at the relevant location and keep the `.rej` file.
4. Report:
   - How many `.rej` files were resolved
   - How many remain for manual review (with file paths)

---

## Phase 6: Update Dependencies

### 6a: .NET Dependencies

1. Read `.github/copilot/migration-data/v5-to-v6/dotnet-dependencies.json`.
2. For each `.csproj` file in the `DotNet/` folder:
   - Update the `<TargetFramework>` if specified in the migration data.
   - Update `<PackageReference>` versions for each listed package.
3. Run `dotnet restore` in the `DotNet/` folder to verify packages resolve.

### 6b: Angular Dependencies

1. Read `.github/copilot/migration-data/v5-to-v6/angular-dependencies.json`.
2. For each Angular front folder detected in Phase 1:
   - Update `package.json` with the new dependency versions.
   - Delete `node_modules/` and `package-lock.json`.
   - Run `npm install`.

---

## Phase 7: Fix C# Usings

1. Find the `.sln` file in the `DotNet/` folder.
2. Run:
   ```
   dotnet format <solution-file> --diagnostics IDE0005
   ```
   This removes unused `using` statements.
3. If `dotnet format` is not available or fails, search for compilation errors related to missing/unused usings and fix them manually.

---

## Phase 8: Post-Migration Cleanup

1. Delete any remaining `.rej` files (those not already handled).
2. Delete `package-lock.json` in all Angular front folders (will be regenerated).
3. Update the `FrameworkVersion` in `Constants.cs` to `"6.0.0"`.
4. Ensure the `.bia/` folder exists at the project root. Create it if missing.
5. Stage and commit all changes:
   ```
   git add -A
   git commit -m "chore: migrate BIA Framework from V5 to V6"
   ```

---

## Phase 9: Verification

1. Build the .NET solution:

   ```
   dotnet build <solution-file>
   ```

   If there are errors, read `.github/copilot/migration-data/v5-to-v6/breaking-changes.md` for guidance and attempt to fix them.

2. Build each Angular front:

   ```
   cd <angular-front-folder> && npm run build
   ```

   Attempt to fix any TypeScript/Angular compilation errors.

3. Report the final status:
   - **Success**: both builds pass, migration complete.
   - **Partial**: list remaining errors with file paths and descriptions.
   - **Failed**: describe what went wrong and suggest manual steps.

4. Remind the developer to:
   - Review all changes in the Git diff
   - Run the application and test key features
   - Optionally clean up migration data: `rm -rf .github/copilot/migration-data/`

---

## Important Notes

- **Never** force-push or rewrite history on shared branches.
- If anything goes wrong, the developer can reset: `git checkout main && git branch -D migration/v5-to-v6`.
- The migration patch was generated from a standard BIA template project. Customizations specific to the developer's project may cause additional `.rej` files -- this is expected.
- When in doubt about a conflict resolution, **ask the developer** rather than guessing.
