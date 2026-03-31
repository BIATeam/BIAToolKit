# Guide Tech Lead - Bonnes Pratiques MVVM

## Vue d'ensemble

Ce document définit les bonnes pratiques et patterns à suivre lors de la migration MVVM vers CommunityToolkit.Mvvm. Tous les développeurs doivent suivre ces guidelines pour garantir la cohérence et la qualité du code.

## Pattern ViewModelLocator (OBLIGATOIRE)

### Principe

Le ViewModelLocator est le pattern standard pour résoudre les ViewModels via le container DI. Il permet:
- ✅ Injection de dépendances automatique
- ✅ Testabilité maximale (mock facile)
- ✅ Pas de `new` dans le code-behind
- ✅ Lifecycle géré par le DI container

### Implémentation

#### 1. Ajouter le ViewModel au ViewModelLocator

**Fichier:** `BIA.ToolKit/Infrastructure/ViewModelLocator.cs`

```csharp
public class ViewModelLocator
{
    public LogDetailViewModel LogDetailViewModel => App.GetService<LogDetailViewModel>();
    public VersionAndOptionViewModel VersionAndOptionViewModel => App.GetService<VersionAndOptionViewModel>();
    // Ajouter chaque nouveau ViewModel ici
}
```

#### 2. Enregistrer le ViewModel dans le DI Container

**Fichier:** `BIA.ToolKit/App.xaml.cs`

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Services...
    
    // ViewModels
    services.AddTransient<LogDetailViewModel>();      // Pour les dialogs (nouvelle instance à chaque fois)
    services.AddSingleton<VersionAndOptionViewModel>(); // Pour les UserControls (instance unique)
}
```

**Règle de choix:**
- `AddTransient`: Pour les dialogs/windows (nouvelle instance à chaque ouverture)
- `AddSingleton`: Pour les UserControls principaux (instance unique partagée)

#### 3. Configurer le DataContext dans le XAML

**Fichier:** `*.xaml`

```xml
<UserControl ...
             DataContext="{Binding Source={StaticResource Locator}, Path=VersionAndOptionViewModel}">
    <!-- Contenu -->
</UserControl>
```

**❌ NE PAS FAIRE:**
```xml
<UserControl.DataContext>
    <local:VersionAndOptionViewModel />  <!-- Crée une instance sans DI -->
</UserControl.DataContext>
```

#### 4. Utiliser le DataContext dans le Code-Behind

**Fichier:** `*.xaml.cs`

```csharp
public partial class VersionAndOptionUserControl : UserControl
{
    public VersionAndOptionUserControl()
    {
        InitializeComponent();
        // Pas de vm = new ViewModel() ici!
    }

    public void Inject(...)
    {
        if (DataContext is VersionAndOptionViewModel vm)
        {
            vm.Inject(...);
        }
    }

    public void SomePublicMethod()
    {
        if (DataContext is VersionAndOptionViewModel vm)
        {
            vm.DoSomething();
        }
    }
}
```

**❌ NE PAS FAIRE:**
```csharp
public VersionAndOptionViewModel vm;  // Champ public exposé

public VersionAndOptionUserControl()
{
    InitializeComponent();
    vm = (VersionAndOptionViewModel)base.DataContext;  // Cast direct
}
```

## Pattern Injection de Dépendances

### Principe

Les ViewModels reçoivent leurs dépendances via une méthode `Inject()` appelée après la construction. Cela permet:
- ✅ Construction sans paramètres (requis pour XAML)
- ✅ Injection tardive des services
- ✅ Flexibilité pour les tests

### Implémentation

```csharp
public partial class VersionAndOptionViewModel : ObservableObject
{
    private RepositoryService repositoryService;
    private SettingsService settingsService;
    private GitService gitService;
    private IConsoleWriter consoleWriter;
    private UIEventBroker eventBroker;

    public VersionAndOptionViewModel()
    {
        // Construction minimale
    }

    public void Inject(RepositoryService repositoryService, SettingsService settingsService, 
                       GitService gitService, IConsoleWriter consoleWriter, UIEventBroker eventBroker)
    {
        this.repositoryService = repositoryService;
        this.settingsService = settingsService;
        this.gitService = gitService;
        this.consoleWriter = consoleWriter;
        this.eventBroker = eventBroker;

        // S'abonner aux events ici
        eventBroker.OnSettingsUpdated += OnSettingsUpdated;
    }
}
```

## Pattern Extraction de Business Logic

### Classification de la Logique

**Business Logic (DOIT être dans le ViewModel):**
- ✅ Manipulation de données (calculs, transformations, validations)
- ✅ Appels à des services ou repositories
- ✅ Orchestration de workflows métier
- ✅ Gestion d'état métier
- ✅ Event broker handlers

**UI Logic (PEUT rester dans le code-behind):**
- ✅ Gestion du focus clavier
- ✅ Animations et transitions visuelles
- ✅ Manipulation directe du VisualTree
- ✅ Gestion des états visuels (VisualStateManager)
- ✅ Drag & drop UI (positions, curseurs)

### Exemple de Migration

**❌ AVANT (Business Logic dans code-behind):**
```csharp
// VersionAndOptionUserControl.xaml.cs
private void RefreshConfiguration()
{
    var listWorkTemplates = new List<WorkRepository>();
    foreach (var repository in settingsService.Settings.TemplateRepositories)
    {
        AddTemplatesVersion(listWorkTemplates, repository);
    }
    vm.WorkTemplates = new ObservableCollection<WorkRepository>(listWorkTemplates);
}
```

**✅ APRÈS (Business Logic dans ViewModel):**
```csharp
// VersionAndOptionViewModel.cs
public void RefreshConfiguration()
{
    var listWorkTemplates = new List<WorkRepository>();
    foreach (var repository in settingsService.Settings.TemplateRepositories)
    {
        AddTemplatesVersion(listWorkTemplates, repository);
    }
    WorkTemplates = new ObservableCollection<WorkRepository>(listWorkTemplates);
}

// VersionAndOptionUserControl.xaml.cs - VIDE ou délégation simple
```

## Pattern Event Handlers → Commands

### Principe

Les event handlers XAML doivent être convertis en commandes pour respecter le pattern MVVM.

### Implémentation

**❌ AVANT (Event Handler):**
```xml
<ComboBox SelectionChanged="FrameworkVersion_SelectionChanged" />
```

```csharp
// Code-behind
private void FrameworkVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    // Business logic ici
}
```

**✅ APRÈS (Command):**
```xml
<ComboBox>
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="SelectionChanged">
            <i:InvokeCommandAction Command="{Binding OnFrameworkVersionSelectionChangedCommand}" />
        </i:EventTrigger>
    </i:Interaction.Triggers>
</ComboBox>
```

```csharp
// ViewModel
[RelayCommand]
private void OnFrameworkVersionSelectionChanged()
{
    // Business logic ici
}
```

**Note:** Nécessite `xmlns:i="http://schemas.microsoft.com/xaml/behaviors"` dans le XAML.

## Pattern Extension Method pour UserControl avec ViewModel

### Principe

Pour simplifier l'accès typé au ViewModel depuis le code-behind, nous utilisons une extension method qui encapsule le cast du DataContext. Cela évite la répétition du pattern `if (DataContext is ViewModel vm)` et fournit un accès fortement typé.

### Implémentation

**Fichier:** `BIA.ToolKit/Infrastructure/UserControlBase.cs`

```csharp
public static class UserControlViewModelHelper
{
    /// <summary>
    /// Gets the strongly-typed ViewModel from DataContext
    /// Returns null if DataContext is not of type TViewModel
    /// </summary>
    public static TViewModel GetViewModel<TViewModel>(this UserControl control)
        where TViewModel : ObservableObject
    {
        return control.DataContext as TViewModel;
    }
}
```

### Utilisation dans le Code-Behind

```csharp
public partial class VersionAndOptionUserControl : UserControl
{
    /// <summary>
    /// Strongly-typed ViewModel accessor using extension method
    /// </summary>
    public VersionAndOptionViewModel ViewModel => this.GetViewModel<VersionAndOptionViewModel>();

    public VersionAndOptionUserControl()
    {
        InitializeComponent();
    }

    public void Inject(...)
    {
        ViewModel?.Inject(...);
    }

    // API publique pour compatibilité
    public void SelectVersion(string version)
    {
        ViewModel?.SelectVersion(version);
    }

    public async Task FillVersionFolderPathAsync()
    {
        if (ViewModel != null)
        {
            await ViewModel.FillVersionFolderPathAsync();
        }
    }
}
```

### Avantages

- ✅ Accès typé au ViewModel sans cast répétitif
- ✅ Null-safe avec l'opérateur `?.`
- ✅ Code plus lisible et maintenable
- ✅ IntelliSense complet sur le ViewModel

### Règles

- ✅ Toujours utiliser `ViewModel?.` pour les appels (null-safe)
- ✅ Déléguer immédiatement au ViewModel
- ✅ Pas de logique métier dans le code-behind
- ✅ Garder les signatures publiques pour compatibilité
- ✅ Utiliser `if (ViewModel != null)` pour les blocs async

## Pattern API Publique du Code-Behind

### Principe

Le code-behind peut exposer une API publique pour les appels externes, mais DOIT déléguer au ViewModel en utilisant la propriété `ViewModel` typée.

### Implémentation

```csharp
public partial class VersionAndOptionUserControl : UserControl
{
    public VersionAndOptionViewModel ViewModel => this.GetViewModel<VersionAndOptionViewModel>();

    // API publique pour compatibilité
    public void SelectVersion(string version)
    {
        ViewModel?.SelectVersion(version);
    }

    public void SetCurrentProjectPath(string path, bool mapCompanyFileVersion, bool mapFrameworkVersion)
    {
        ViewModel?.SetCurrentProjectPath(path, mapCompanyFileVersion, mapFrameworkVersion);
    }
}
```

**Règles:**
- ✅ Utiliser la propriété `ViewModel` typée
- ✅ Toujours utiliser l'opérateur null-safe `?.`
- ✅ Déléguer immédiatement au ViewModel
- ✅ Pas de logique métier dans le code-behind
- ✅ Garder les signatures publiques pour compatibilité

## Checklist de Migration par ViewModel

### Avant de commencer
- [ ] Lire ce guide Tech Lead
- [ ] Identifier la complexité du ViewModel (Faible/Moyenne/Haute)
- [ ] Lister toutes les dépendances nécessaires

### Étape 1: Créer le ViewModel
- [ ] Créer fichier dans `BIA.ToolKit/ViewModels/` ou `BIA.ToolKit.Application/ViewModel/`
- [ ] Hériter de `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
- [ ] Marquer la classe comme `partial`
- [ ] Convertir propriétés avec `[ObservableProperty]`
- [ ] Convertir commandes avec `[RelayCommand]`
- [ ] Créer méthode `Inject()` pour les dépendances

### Étape 2: Configurer le DI
- [ ] Ajouter ViewModel au `ViewModelLocator`
- [ ] Enregistrer dans `App.xaml.cs` (Transient ou Singleton)
- [ ] Ajouter les imports nécessaires

### Étape 3: Extraire la Business Logic
- [ ] Identifier toute la Business Logic dans le code-behind
- [ ] Déplacer les méthodes vers le ViewModel
- [ ] Convertir les event handlers en commandes
- [ ] Gérer les event broker subscriptions dans le ViewModel

### Étape 4: Mettre à jour le XAML
- [ ] Configurer DataContext via ViewModelLocator
- [ ] Remplacer event handlers par Command bindings
- [ ] Vérifier tous les bindings

### Étape 5: Nettoyer le Code-Behind
- [ ] Supprimer tous les champs de dépendances
- [ ] Supprimer toutes les méthodes de Business Logic
- [ ] Ajouter propriété `ViewModel` avec extension method: `public TViewModel ViewModel => this.GetViewModel<TViewModel>();`
- [ ] Garder seulement `InitializeComponent()` + `Inject()` + API publique
- [ ] Utiliser `ViewModel?.` pour tous les appels au ViewModel

### Étape 6: Compiler et Tester
- [ ] Compiler sans erreurs
- [ ] Vérifier diagnostics
- [ ] Tester manuellement toutes les fonctionnalités
- [ ] Vérifier aucune régression

### Étape 7: Commit
- [ ] Commit avec message descriptif
- [ ] Inclure métriques de réduction de code
- [ ] Marquer tâches comme complétées

## Métriques de Qualité

### Objectifs par ViewModel
- **Réduction code-behind:** ≥70% pour fichiers >200 lignes
- **Réduction boilerplate propriétés:** ≥60%
- **Réduction boilerplate commandes:** ≥50%
- **Couverture tests:** ≥80% (si tests écrits)

### Indicateurs de Qualité
- ✅ Aucun `new ViewModel()` dans le code-behind
- ✅ Aucune Business Logic dans le code-behind
- ✅ Tous les event handlers convertis en commandes
- ✅ DataContext configuré via ViewModelLocator
- ✅ Compilation sans erreurs ni warnings MVVM

## Erreurs Courantes à Éviter

### ❌ Erreur 1: Instanciation manuelle du ViewModel
```csharp
// MAUVAIS
public VersionAndOptionViewModel vm = new VersionAndOptionViewModel();
```

### ❌ Erreur 2: Cast direct du DataContext
```csharp
// MAUVAIS
vm = (VersionAndOptionViewModel)base.DataContext;
```

**✅ CORRECT:**
```csharp
// Utiliser l'extension method
public VersionAndOptionViewModel ViewModel => this.GetViewModel<VersionAndOptionViewModel>();
```

### ❌ Erreur 3: Business Logic dans le code-behind
```csharp
// MAUVAIS - dans .xaml.cs
private void RefreshConfiguration()
{
    // Logique métier ici
}
```

### ❌ Erreur 4: Event handlers au lieu de commandes
```xml
<!-- MAUVAIS -->
<ComboBox SelectionChanged="MyHandler" />
```

### ❌ Erreur 5: Oublier d'ajouter au ViewModelLocator
```csharp
// MAUVAIS - ViewModel créé mais pas dans le Locator
```

### ❌ Erreur 6: Répéter le pattern `if (DataContext is ...)`
```csharp
// MAUVAIS - répétitif
public void Method1()
{
    if (DataContext is VersionAndOptionViewModel vm)
    {
        vm.DoSomething();
    }
}

public void Method2()
{
    if (DataContext is VersionAndOptionViewModel vm)
    {
        vm.DoSomethingElse();
    }
}
```

**✅ CORRECT:**
```csharp
// Utiliser la propriété ViewModel typée
public VersionAndOptionViewModel ViewModel => this.GetViewModel<VersionAndOptionViewModel>();

public void Method1()
{
    ViewModel?.DoSomething();
}

public void Method2()
{
    ViewModel?.DoSomethingElse();
}
```

## Ressources

### Fichiers de Référence
- **Pilot complet:** `BIA.ToolKit/ViewModels/LogDetailViewModel.cs` + `BIA.ToolKit/Dialogs/LogDetailUC.xaml.cs`
- **Extension method pattern:** `BIA.ToolKit/Infrastructure/UserControlBase.cs`
- **Exemple d'utilisation:** `BIA.ToolKit/UserControls/VersionAndOptionUserControl.xaml.cs`
- **ViewModelLocator:** `BIA.ToolKit/Infrastructure/ViewModelLocator.cs`
- **Configuration DI:** `BIA.ToolKit/App.xaml.cs`

### Documentation
- [CommunityToolkit.Mvvm Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)

## Support

En cas de doute ou de question:
1. Consulter ce guide
2. Regarder le pilot (LogDetailViewModel)
3. Demander une revue de code au Tech Lead
