# ⚡ QUICK START - IMPLÉMENTATION IMMEDIATE

**Objectif**: Débuter l'implémentation des correctifs de manière structurée  
**Durée cible**: 30 minutes pour la première wave de tests  
**Prérequis**: Avoir lu AUDIT_MIGRATION_COMPLET.md

---

## 🎯 PHASE 1: SETUP (5 min)

### 1. Ouvrir le projet dans Visual Studio

```powershell
cd c:\sources\Github\BIAToolKit
start BIAToolKit.sln
```

### 2. Localiser le fichier cible

**Fichier**: [BIA.ToolKit/ViewModels/MainViewModel.cs](BIA.ToolKit/ViewModels/MainViewModel.cs)

---

## 🚀 PHASE 2: AJOUTER LES DÉPENDANCES REQUISES (3 min)

Vérifier que ces `using` existent déjà dans MainViewModel.cs:

```csharp
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using BIA.ToolKit.Application.Messages;
using BIA.ToolKit.Common;
// ... autres usings existants
```

Si manquants, ajouter à la section `using` du fichier.

---

## ✍️ PHASE 3: IMPLÉMENTER LES 8 COMMANDES (15 min)

### Étape 1: Trouver la section Constructeur

Chercher dans MainViewModel.cs:
```csharp
public MainViewModel(
    SettingsService settingsService,
    UpdateService updateService,
    // ... autres dépendances
)
{
    // Corps du constructeur
}
```

### Étape 2: Ajouter les 8 RelayCommand instantiations

**À la FIN du constructeur (avant la fermeture `}`), ajouter:**

```csharp
// ========== COMMANDES À AJOUTER ==========
// PHASE 1: Commandes Critiques

ImportConfigCommand = new RelayCommand(
    async () => await ImportConfigAsync(),
    () => !IsProcessing
);

ExportConfigCommand = new RelayCommand(
    async () => await ExportConfigAsync(),
    () => !IsProcessing && settingsService.Settings != null
);

CreateProjectCommand = new RelayCommand(
    async () => await CreateProjectAsync(),
    () => !IsProcessing && ValidateRepositoriesConfiguration(settingsService.Settings)
);

UpdateCommand = new RelayCommand(
    async () => await UpdateAsync(),
    () => !IsProcessing && IsNewVersionAvailable
);

CheckForUpdatesCommand = new RelayCommand(
    async () => await CheckForUpdatesAsync(),
    () => !IsProcessing
);

BrowseCreateProjectRootFolderCommand = new RelayCommand(
    () => BrowseCreateProjectRootFolder(),
    () => !IsProcessing
);

ClearConsoleCommand = new RelayCommand(
    () => ClearConsole()
);

CopyConsoleToClipboardCommand = new RelayCommand(
    () => CopyConsoleToClipboard(),
    () => !string.IsNullOrEmpty(ConsoleOutput)
);
// ========== FIN COMMANDES ==========
```

### Étape 3: Ajouter les 8 Méthodes Implémentation

**À la FIN de la classe MainViewModel (avant la fermeture finale `}`), ajouter:**

```csharp
// ========== IMPLÉMENTATIONS DE COMMANDES ==========

private async Task ImportConfigAsync()
{
    try
    {
        IsProcessing = true;
        
        var filePath = await fileDialogService.OpenFileAsync(
            filter: "JSON Files|*.json|All Files|*.*",
            title: "Import Configuration"
        );
        
        if (string.IsNullOrEmpty(filePath))
            return;
        
        var json = File.ReadAllText(filePath);
        var importedSettings = JsonSerializer.Deserialize<BIATKSettings>(json);
        
        if (importedSettings == null)
            throw new InvalidOperationException("Configuration file is invalid");
        
        settingsService.UpdateSettings(importedSettings);
        
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            successMessage: $"Configuration imported from {Path.GetFileName(filePath)}"
        ));
        
        await InitializeAsync();
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Import failed: {ex.Message}");
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            errorMessage: $"Import failed: {ex.Message}"
        ));
    }
    finally
    {
        IsProcessing = false;
    }
}

private async Task ExportConfigAsync()
{
    try
    {
        IsProcessing = true;
        
        var filePath = await fileDialogService.SaveFileAsync(
            filter: "JSON Files|*.json",
            title: "Export Configuration",
            defaultFileName: $"BIAToolKit_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        );
        
        if (string.IsNullOrEmpty(filePath))
            return;
        
        var json = JsonSerializer.Serialize(
            settingsService.Settings, 
            new JsonSerializerOptions { WriteIndented = true }
        );
        
        File.WriteAllText(filePath, json);
        
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            successMessage: $"Configuration exported to {Path.GetFileName(filePath)}"
        ));
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Export failed: {ex.Message}");
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            errorMessage: $"Export failed: {ex.Message}"
        ));
    }
    finally
    {
        IsProcessing = false;
    }
}

private async Task CreateProjectAsync()
{
    try
    {
        IsProcessing = true;
        
        if (!ValidateRepositoriesConfiguration(settingsService.Settings))
        {
            consoleWriter.WriteError("Repository configuration is invalid");
            return;
        }
        
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => 
            {
                await projectCreatorService.CreateProjectAsync(
                    rootFolder: Settings_RootProjectsPath,
                    projectName: Settings_CreateCompanyName,
                    repository: ToolkitRepository
                );
            },
            successMessage: "Project created successfully!"
        ));
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Project creation failed: {ex.Message}");
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            errorMessage: $"Project creation failed: {ex.Message}"
        ));
    }
    finally
    {
        IsProcessing = false;
    }
}

private async Task CheckForUpdatesAsync()
{
    try
    {
        IsProcessing = true;
        consoleWriter.WriteLine("Checking for updates...");
        
        await updateService.CheckForUpdatesAsync();
        
        if (IsNewVersionAvailable)
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(
                async () => await Task.CompletedTask,
                successMessage: $"New version {NewVersion} is available!"
            ));
        }
        else
        {
            messenger.Send(new ExecuteActionWithWaiterMessage(
                async () => await Task.CompletedTask,
                successMessage: "You are running the latest version"
            ));
        }
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Update check failed: {ex.Message}");
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            errorMessage: $"Update check failed: {ex.Message}"
        ));
    }
    finally
    {
        IsProcessing = false;
    }
}

private async Task UpdateAsync()
{
    try
    {
        IsProcessing = true;
        consoleWriter.WriteLine("Downloading and installing update...");
        
        var downloadPath = await updateService.DownloadUpdateAsync();
        
        if (string.IsNullOrEmpty(downloadPath))
            throw new InvalidOperationException("Failed to download update");
        
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => 
            {
                System.Diagnostics.Process.Start(downloadPath);
                await Task.CompletedTask;
            },
            successMessage: "Update downloaded. The application will restart."
        ));
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Update failed: {ex.Message}");
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            errorMessage: $"Update failed: {ex.Message}"
        ));
    }
    finally
    {
        IsProcessing = false;
    }
}

private void BrowseCreateProjectRootFolder()
{
    try
    {
        var selectedFolder = fileDialogService.BrowseFolderAsync(
            title: "Select Project Root Folder",
            initialPath: Settings_RootProjectsPath
        ).Result;
        
        if (!string.IsNullOrEmpty(selectedFolder))
        {
            Settings_RootProjectsPath = selectedFolder;
            settingsService.Settings.RootProjectsPath = selectedFolder;
            consoleWriter.WriteLine($"Root folder set to: {selectedFolder}");
        }
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Browse failed: {ex.Message}");
    }
}

private void ClearConsole()
{
    try
    {
        consoleWriter.Clear();
        consoleWriter.WriteLine("Console cleared.");
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Clear failed: {ex.Message}");
    }
}

private void CopyConsoleToClipboard()
{
    try
    {
        if (string.IsNullOrEmpty(ConsoleOutput))
        {
            consoleWriter.WriteWarning("Console is empty");
            return;
        }
        
        Clipboard.SetText(ConsoleOutput);
        consoleWriter.WriteLine("✓ Console content copied to clipboard");
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Copy failed: {ex.Message}");
    }
}

// ========== FIN IMPLÉMENTATIONS ==========
```

---

## 📝 PHASE 4: AJOUTER LES MESSAGE HANDLERS (5 min)

Localiser la méthode `InitializeAsync()` dans MainViewModel.cs:

```csharp
public async Task InitializeAsync()
{
    await mainWindowHelper.InitializeApplicationAsync(applicationVersion);
    
    // ← AJOUTER ICI
}
```

Remplacer par:

```csharp
public async Task InitializeAsync()
{
    await mainWindowHelper.InitializeApplicationAsync(applicationVersion);
    
    // ========== MESSAGE HANDLERS ==========
    
    messenger.Register<NewVersionAvailableMessage>(this, (recipient, message) =>
    {
        IsNewVersionAvailable = true;
        NewVersion = message.NewVersion;
        consoleWriter.WriteLine($"✓ New version available: {NewVersion}");
    });
    
    messenger.Register<RepositoriesUpdatedMessage>(this, async (recipient, message) =>
    {
        consoleWriter.WriteLine("Repositories updated, refreshing...");
        
        var settings = await mainWindowHelper.InitializeSettingsAsync();
        await mainWindowHelper.FetchReleaseDataAsync(settings, syncBefore: false);
        
        consoleWriter.WriteLine("✓ Repositories refreshed");
    });
    
    // ========== FIN HANDLERS ==========
}
```

---

## ✅ PHASE 5: COMPILATION & TEST (5 min)

### Compiler le projet

```powershell
# Build la solution
cd c:\sources\Github\BIAToolKit
dotnet build BIAToolKit.sln
```

### Vérifier les erreurs de compilation

- ✅ **Aucune erreur critique**: Passer à tests
- ❌ **Erreurs manquantes de dépendances**: Vérifier les using/injection
- ❌ **Erreurs de références**: Vérifier interfaces implémentées

### Test manuel

1. Lancer l'application
2. Vérifier que les boutons répondent aux clics
3. Tester ImportConfigCommand (créer un fichier JSON d'export d'abord)
4. Tester ExportConfigCommand (créer le fichier)
5. Tester CheckForUpdatesCommand
6. Tester ClearConsoleCommand
7. Tester CopyConsoleToClipboardCommand

---

## 🐛 TROUBLESHOOTING RAPIDE

### Erreur: "Type or namespace name 'IRelayCommand' not found"

**Cause**: Missing `using CommunityToolkit.Mvvm.Input;`  
**Solution**: Ajouter le using en haut du fichier

### Erreur: "Name 'fileDialogService' does not exist"

**Cause**: `IFileDialogService` n'est pas injecté  
**Solution**: Vérifier que le constructeur le reçoit et le stock

### Erreur: "IsProcessing does not exist"

**Cause**: Property manquante dans la classe  
**Solution**: Ajouter `[ObservableProperty] private bool isProcessing;` dans la classe

### Erreur: "ConsoleOutput does not exist"

**Cause**: Property manquante  
**Solution**: Vérifier que consoleWriter expose cette propriété

### Les commandes ne s'exécutent pas quand on clique

**Cause 1**: Binding XAML incorrect  
**Solution**: Vérifier que MainWindow.xaml a bien `Command="{Binding CommandName}"`

**Cause 2**: Propriété de commande est null  
**Solution**: Vérifier que la RelayCommand est bien instantiée dans le constructeur

---

## 📊 CHECKLIST APRÈS IMPLÉMENTATION

- [ ] Fichier compile sans erreurs critiques
- [ ] Les 8 commandes sont instantiées (debug → verify dans Locals)
- [ ] ImportConfigCommand fonctionne
- [ ] ExportConfigCommand fonctionne
- [ ] CreateProjectCommand fonctionne (ou au moins s'exécute)
- [ ] UpdateCommand fonctionne
- [ ] CheckForUpdatesCommand fonctionne
- [ ] BrowseCreateProjectRootFolderCommand fonctionne
- [ ] ClearConsoleCommand fonctionne
- [ ] CopyConsoleToClipboardCommand fonctionne
- [ ] NewVersionAvailableMessage est traité
- [ ] RepositoriesUpdatedMessage est traité
- [ ] Application démarre sans crash
- [ ] UI responsive (pas de freeze)

---

## 🎯 PROCHAINE ÉTAPE

Une fois cette PHASE 1 validée:

1. ✅ PHASE 1 (ce document) - Commandes + Handlers
2. → PHASE 2 - Compléter les TODOs dans CustomTemplatesRepositoriesSettingsUC
3. → PHASE 3 - Gérer les NotImplementedExceptions
4. → PHASE 4 - Tests complets end-to-end

---

## 📞 SUPPORT

En cas de problème lors de l'implémentation:

1. Consulter [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) pour les détails
2. Consulter [AUDIT_MIGRATION_COMPLET.md](AUDIT_MIGRATION_COMPLET.md) pour le contexte
3. Vérifier que toutes les dépendances sont injectées dans le constructeur
4. Vérifier les typos dans les noms de propriétés

---

**Durée estimée totale**: 30 minutes  
**Gain**: Application 80% opérationnelle après cette phase
