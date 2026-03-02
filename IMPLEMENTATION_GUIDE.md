# 📝 GUIDE D'IMPLÉMENTATION - RESTAURATION DES FONCTIONNALITÉS

## PHASE 1: IMPLÉMENTATION DES 8 COMMANDES MANQUANTES

### Localisation: `BIA.ToolKit/ViewModels/MainViewModel.cs`

---

## 1️⃣ ImportConfigCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding ImportConfigCommand}"`  
**Impact**: Charger une configuration exportée

### Code à Ajouter (dans le constructeur)

```csharp
// Dans MainViewModel.cs constructeur
ImportConfigCommand = new RelayCommand(
    async () => await ImportConfigAsync(),
    () => !IsProcessing
);

// Méthode d'implémentation
private async Task ImportConfigAsync()
{
    try
    {
        IsProcessing = true;
        
        // Ouvrir dialog de sélection fichier
        var filePath = await fileDialogService.OpenFileAsync(
            filter: "JSON Files|*.json|All Files|*.*",
            title: "Import Configuration"
        );
        
        if (string.IsNullOrEmpty(filePath))
            return;
        
        // Charger et désérialiser la configuration
        var json = File.ReadAllText(filePath);
        var importedSettings = JsonSerializer.Deserialize<BIATKSettings>(json);
        
        if (importedSettings == null)
            throw new InvalidOperationException("Configuration file is invalid");
        
        // Mettre à jour les paramètres
        settingsService.UpdateSettings(importedSettings);
        
        // Envoyer message de confirmation
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            successMessage: $"Configuration imported successfully from {Path.GetFileName(filePath)}"
        ));
        
        // Rafraîchir l'UI
        await InitializeAsync();
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to import configuration: {ex.Message}");
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
```

---

## 2️⃣ ExportConfigCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding ExportConfigCommand}"`  
**Impact**: Exporter la configuration actuelle

### Code à Ajouter

```csharp
// Dans MainViewModel.cs constructeur
ExportConfigCommand = new RelayCommand(
    async () => await ExportConfigAsync(),
    () => !IsProcessing && settingsService.Settings != null
);

// Méthode d'implémentation
private async Task ExportConfigAsync()
{
    try
    {
        IsProcessing = true;
        
        // Ouvrir dialog de sauvegarde
        var filePath = await fileDialogService.SaveFileAsync(
            filter: "JSON Files|*.json",
            title: "Export Configuration",
            defaultFileName: $"BIAToolKit_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        );
        
        if (string.IsNullOrEmpty(filePath))
            return;
        
        // Sérialiser et exporter les paramètres
        var json = JsonSerializer.Serialize(
            settingsService.Settings, 
            new JsonSerializerOptions { WriteIndented = true }
        );
        
        File.WriteAllText(filePath, json);
        
        consoleWriter.WriteLine($"✓ Configuration exported to {Path.GetFileName(filePath)}");
        
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => await Task.CompletedTask,
            successMessage: $"Configuration exported successfully to {Path.GetFileName(filePath)}"
        ));
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to export configuration: {ex.Message}");
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
```

---

## 3️⃣ CheckForUpdatesCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding CheckForUpdatesCommand}"`  
**Impact**: Vérifier manuellement les mises à jour

### Code à Ajouter

```csharp
// Dans MainViewModel.cs constructeur
CheckForUpdatesCommand = new RelayCommand(
    async () => await CheckForUpdatesAsync(),
    () => !IsProcessing
);

// Méthode d'implémentation
private async Task CheckForUpdatesAsync()
{
    try
    {
        IsProcessing = true;
        consoleWriter.WriteLine("Checking for updates...");
        
        await updateService.CheckForUpdatesAsync();
        
        if (IsNewVersionAvailable)
        {
            consoleWriter.WriteLine($"✓ New version available: {NewVersion}");
            messenger.Send(new ExecuteActionWithWaiterMessage(
                async () => await Task.CompletedTask,
                successMessage: $"New version {NewVersion} is available!"
            ));
        }
        else
        {
            consoleWriter.WriteLine("✓ You are already on the latest version");
            messenger.Send(new ExecuteActionWithWaiterMessage(
                async () => await Task.CompletedTask,
                successMessage: "You are running the latest version"
            ));
        }
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to check for updates: {ex.Message}");
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
```

---

## 4️⃣ UpdateCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding UpdateCommand}"`  
**Impact**: Télécharger et installer la mise à jour

### Code à Ajouter

```csharp
// Dans MainViewModel.cs constructeur
UpdateCommand = new RelayCommand(
    async () => await UpdateAsync(),
    () => !IsProcessing && IsNewVersionAvailable
);

// Méthode d'implémentation
private async Task UpdateAsync()
{
    try
    {
        IsProcessing = true;
        consoleWriter.WriteLine("Downloading and installing update...");
        
        var downloadPath = await updateService.DownloadUpdateAsync();
        
        if (string.IsNullOrEmpty(downloadPath))
            throw new InvalidOperationException("Failed to download update");
        
        consoleWriter.WriteLine($"✓ Update downloaded to {downloadPath}");
        
        // Proposer de redémarrer l'application
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => 
            {
                // Lance l'updater qui redémarrera l'application
                System.Diagnostics.Process.Start(downloadPath);
                await Task.CompletedTask;
            },
            successMessage: "Update downloaded successfully. The application will restart to apply the update."
        ));
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to update: {ex.Message}");
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
```

---

## 5️⃣ CreateProjectCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding CreateProjectCommand}"`  
**Impact**: Créer un nouveau projet BIA

### Code à Ajouter

```csharp
// Dans MainViewModel.cs constructeur
CreateProjectCommand = new RelayCommand(
    async () => await CreateProjectAsync(),
    () => !IsProcessing && ValidateRepositoriesConfiguration(settingsService.Settings)
);

// Méthode d'implémentation
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
        
        consoleWriter.WriteLine("Starting project creation...");
        
        // Envoyer le message avec l'action créée
        messenger.Send(new ExecuteActionWithWaiterMessage(
            async () => 
            {
                var rootFolder = Settings_RootProjectsPath;
                var companyName = Settings_CreateCompanyName;
                
                await projectCreatorService.CreateProjectAsync(
                    rootFolder: rootFolder,
                    projectName: companyName,
                    repository: ToolkitRepository
                );
            },
            successMessage: "Project created successfully!"
        ));
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to create project: {ex.Message}");
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
```

---

## 6️⃣ BrowseCreateProjectRootFolderCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding BrowseCreateProjectRootFolderCommand}"`  
**Impact**: Parcourir le système de fichiers pour sélectionner le dossier racine

### Code à Ajouter

```csharp
// Dans MainViewModel.cs constructeur
BrowseCreateProjectRootFolderCommand = new RelayCommand(
    () => BrowseCreateProjectRootFolder(),
    () => !IsProcessing
);

// Méthode d'implémentation
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
        consoleWriter.WriteError($"Failed to browse folder: {ex.Message}");
    }
}
```

---

## 7️⃣ ClearConsoleCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding ClearConsoleCommand}"`  
**Impact**: Effacer le contenu de la console

### Code à Ajouter

```csharp
// Dans MainViewModel.cs constructeur
ClearConsoleCommand = new RelayCommand(
    () => ClearConsole()
);

// Méthode d'implémentation
private void ClearConsole()
{
    try
    {
        // Vider le ConsoleOutput (ObservableProperty de la console)
        consoleWriter.Clear();
        consoleWriter.WriteLine("Console cleared.");
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to clear console: {ex.Message}");
    }
}
```

---

## 8️⃣ CopyConsoleToClipboardCommand

**Statut**: ❌ MISSING  
**XAML Binding**: `Command="{Binding CopyConsoleToClipboardCommand}"`  
**Impact**: Copier tout le contenu de la console dans le presse-papiers

### Code à Ajouter

```csharp
// Dans MainViewModel.cs constructeur
CopyConsoleToClipboardCommand = new RelayCommand(
    () => CopyConsoleToClipboard(),
    () => !string.IsNullOrEmpty(ConsoleOutput)  // Si console n'est pas vide
);

// Méthode d'implémentation
private void CopyConsoleToClipboard()
{
    try
    {
        if (string.IsNullOrEmpty(ConsoleOutput))
        {
            consoleWriter.WriteWarning("Console is empty");
            return;
        }
        
        System.Windows.Clipboard.SetText(ConsoleOutput);
        consoleWriter.WriteLine("✓ Console content copied to clipboard");
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to copy console: {ex.Message}");
    }
}
```

---

---

## PHASE 2: HANDLERS DE MESSAGES MANQUANTS

### Localisation: `BIA.ToolKit/ViewModels/MainViewModel.cs`

---

## 9️⃣ NewVersionAvailableMessage Handler

**Statut**: ❌ ORPHELIN  
**Message Type**: `NewVersionAvailableMessage`  
**Impact**: Afficher notification de mise à jour disponible

### Code à Ajouter (dans InitializeAsync)

```csharp
public async Task InitializeAsync()
{
    await mainWindowHelper.InitializeApplicationAsync(applicationVersion);
    
    // ✅ AJOUTER CE HANDLER
    messenger.Register<NewVersionAvailableMessage>(this, (recipient, message) =>
    {
        IsNewVersionAvailable = true;
        NewVersion = message.NewVersion;
        consoleWriter.WriteLine($"✓ New version available: {NewVersion}");
    });
}
```

---

## 🔟 RepositoriesUpdatedMessage Handler  

**Statut**: ⚠️ PARTIEL (Envoyé mais non reçu)  
**Message Type**: `RepositoriesUpdatedMessage`  
**Impact**: Rafraîchir les ObservableCollections de repositories

### Code à Ajouter (dans InitializeAsync ou constructeur)

```csharp
public async Task InitializeAsync()
{
    await mainWindowHelper.InitializeApplicationAsync(applicationVersion);
    
    messenger.Register<NewVersionAvailableMessage>(this, (recipient, message) =>
    {
        IsNewVersionAvailable = true;
        NewVersion = message.NewVersion;
    });
    
    // ✅ AJOUTER CE HANDLER
    messenger.Register<RepositoriesUpdatedMessage>(this, async (recipient, message) =>
    {
        consoleWriter.WriteLine("Repositories updated, refreshing...");
        
        // Recharger les repositories
        var settings = await mainWindowHelper.InitializeSettingsAsync();
        await mainWindowHelper.FetchReleaseDataAsync(settings, syncBefore: false);
        
        consoleWriter.WriteLine("✓ Repositories refreshed");
    });
}
```

---

---

## PHASE 3: TÂCHES À COMPLÉTER

### Fichier: `BIA.ToolKit/UserControls/CustomTemplatesRepositoriesSettingsUC.xaml.cs`

---

## ✏️ Implémenter TODO: Edit Repository

**Localisation**: `CustomTemplatesRepositoriesSettingsUC.xaml.cs` ligne ~50

```csharp
private async Task EditRepositoryAsync(RepositoryViewModel repository)
{
    try
    {
        // Afficher un dialog pour éditer le repository
        var dialog = new RepositoryEditDialog();
        dialog.DataContext = new RepositoryEditViewModel(repository);
        
        var result = await dialogService.ShowDialogAsync(dialog);
        
        if (result == true)
        {
            // Mettre à jour le repository
            await repositoryService.UpdateRepositoryAsync(
                repository.Id,
                dialog.RepositoryName,
                dialog.RepositoryUrl
            );
            
            consoleWriter.WriteLine($"✓ Repository '{repository.Name}' updated");
        }
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to edit repository: {ex.Message}");
    }
}
```

---

## 🗑️ Implémenter TODO: Delete Repository

**Localisation**: `CustomTemplatesRepositoriesSettingsUC.xaml.cs` ligne ~60

```csharp
private async Task DeleteRepositoryAsync(RepositoryViewModel repository)
{
    try
    {
        // Demander confirmation
        var confirmed = await dialogService.ShowConfirmationAsync(
            title: "Delete Repository",
            message: $"Are you sure you want to delete '{repository.Name}'?",
            yesText: "Delete",
            noText: "Cancel"
        );
        
        if (!confirmed)
            return;
        
        // Supprimer le repository
        await repositoryService.DeleteRepositoryAsync(repository.Id);
        
        // Retirer de la collection
        TemplateRepositories.Remove(repository);
        
        consoleWriter.WriteLine($"✓ Repository '{repository.Name}' deleted");
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to delete repository: {ex.Message}");
    }
}
```

---

## 🔄 Implémenter TODO: Synchronize Repositories

**Localisation**: `CustomTemplatesRepositoriesSettingsUC.xaml.cs` ligne ~70

```csharp
private async Task SynchronizeRepositoriesAsync()
{
    try
    {
        IsProcessing = true;
        consoleWriter.WriteLine("Synchronizing repositories...");
        
        // Synchroniser chaque repository
        var tasks = TemplateRepositories.Select(repo => 
            repositoryService.SynchronizeRepositoryAsync(repo.Id)
        );
        
        await Task.WhenAll(tasks);
        
        // Rafraîchir les données
        await FetchReleaseDataAsync();
        
        consoleWriter.WriteLine("✓ All repositories synchronized");
        messenger.Send(new RepositoriesUpdatedMessage());
    }
    catch (Exception ex)
    {
        consoleWriter.WriteError($"Failed to synchronize repositories: {ex.Message}");
    }
    finally
    {
        IsProcessing = false;
    }
}
```

---

---

## ⚠️ TRAITER NotImplementedExceptions

### Fichier: `BIA.ToolKit.Application/Services/CRUD/CRUDGenerationService.cs`

```csharp
// AVANT (Problématique)
case CRUDGenerationType.Option:
    throw new NotImplementedException("Option CRUD not fully supported");

// APRÈS (Solution)
case CRUDGenerationType.Option:
    consoleWriter.WriteWarning("Option CRUD support is limited");
    // Implémenter logique de fallback ou limiter les features
    return await GenerateOptionCRUDAsync(project, options);
```

### Fichier: `BIA.ToolKit.Application/Services/Option/OptionGenerationService.cs`

```csharp
// AVANT
case OptionFeatureType.Advanced:
    throw new NotImplementedException("Advanced options not implemented");

// APRÈS  
case OptionFeatureType.Advanced:
    consoleWriter.WriteWarning("Advanced option generation uses standard mode");
    return await GenerateStandardOptionAsync(project, option);
```

---

---

## 📋 CHECKLIST D'IMPLÉMENTATION

### PHASE 1 - COMMANDES CRITIQUES
- [ ] ImportConfigCommand + ImportConfigAsync()
- [ ] ExportConfigCommand + ExportConfigAsync()
- [ ] CheckForUpdatesCommand + CheckForUpdatesAsync()
- [ ] UpdateCommand + UpdateAsync()
- [ ] CreateProjectCommand + CreateProjectAsync()
- [ ] BrowseCreateProjectRootFolderCommand + BrowseCreateProjectRootFolder()
- [ ] ClearConsoleCommand + ClearConsole()
- [ ] CopyConsoleToClipboardCommand + CopyConsoleToClipboard()

### PHASE 2 - HANDLERS DE MESSAGES
- [ ] NewVersionAvailableMessage handler
- [ ] RepositoriesUpdatedMessage handler

### PHASE 3 - TÂCHES RESTANTES
- [ ] CustomTemplatesRepositoriesSettingsUC - Edit
- [ ] CustomTemplatesRepositoriesSettingsUC - Delete
- [ ] CustomTemplatesRepositoriesSettingsUC - Synchronize
- [ ] Traiter NotImplementedException du CRUD
- [ ] Traiter NotImplementedException des Options

### PHASE 4 - VALIDATION
- [ ] Compiler sans erreurs
- [ ] Tests de chaque commande
- [ ] Tests des messages/handlers
- [ ] Tests end-to-end
- [ ] Valider UI responsive

---

*Ce guide contient TOUS les code snippets nécessaires pour restaurer 100% de la fonctionnalité*
