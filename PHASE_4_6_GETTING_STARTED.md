# 🚀 Plan de Refactorisation Phases 4-6 - Guide de Démarrage

**Date**: 22 janvier 2026  
**Objectif**: Transformation complète MVVM + Clean Architecture  
**Durée estimée**: 9 jours

---

## 📋 Documents Créés

| Document | Description | Usage |
|----------|-------------|-------|
| **REFACTORING_PHASE_4_6_PLAN.md** | Plan détaillé des 18 étapes (27-44) | Guide d'implémentation |
| **CODE_BEHIND_DETAILED_ANALYSIS.md** | Analyse ligne par ligne des violations | Référence pendant refactoring |
| **ARCHITECTURE_PRINCIPLES.md** | Patterns et principes à suivre | Guide architectural |
| **REFACTORING_TRACKING.md** | Suivi mis à jour avec Phases 4-6 | Tracking progrès |

---

## 🎯 Objectifs Phases 4-6

### Phase 4: ViewModels Complets (Étapes 27-32)
**Durée**: 4.5 jours  
**Objectif**: Créer/compléter tous les ViewModels avec Commands et Observable Properties

**Livrables**:
- MainWindowViewModel avec tous les Commands
- CRUDGeneratorViewModel complet
- OptionGeneratorViewModel complet
- DtoGeneratorViewModel finalisé
- ModifyProjectViewModel avec Commands
- VersionAndOptionViewModel avec bindings

---

### Phase 5: Éliminer Service Locator (Étapes 33-38)
**Durée**: 2.5 jours  
**Objectif**: Remplacer toutes les méthodes `Inject()` par Constructor Injection

**Livrables**:
- Suppression de 5 méthodes Inject()
- Configuration DI complète dans App.xaml.cs
- Constructor injection partout
- UserControls créés via DI Container

---

### Phase 6: XAML Refactoring (Étapes 39-44)
**Durée**: 2.25 jours  
**Objectif**: Remplacer tous les event handlers par Command bindings

**Livrables**:
- Conversion de 16+ event handlers en Command bindings
- XAML avec bindings uniquement
- Code-behind réduits à ~30-50 lignes chacun

---

## 📊 Métriques Cibles

### Code-Behind Reduction

| Fichier | Avant | Après | Réduction |
|---------|-------|-------|-----------|
| MainWindow.xaml.cs | 534 lignes | 50 lignes | **-91%** |
| CRUDGeneratorUC.xaml.cs | 706 lignes | 30 lignes | **-96%** |
| OptionGeneratorUC.xaml.cs | 488 lignes | 30 lignes | **-94%** |
| DtoGeneratorUC.xaml.cs | 199 lignes | 25 lignes | **-87%** |
| ModifyProjectUC.xaml.cs | 400 lignes | 30 lignes | **-92%** |
| VersionAndOption.xaml.cs | 233 lignes | 30 lignes | **-87%** |
| **TOTAL** | **2,560** | **195** | **-92%** |

### Architecture Improvements

| Métrique | Avant | Après |
|----------|-------|-------|
| Méthodes Inject() | 5 | 0 ✅ |
| Event Handlers | 16+ | 0 ✅ |
| ViewModels complets | 0 | 6 ✅ |
| Commands MVVM | 0 | 30+ ✅ |
| Testability | 10% | 95% ✅ |

---

## 🛣️ Roadmap Phase 4

### Semaine 1: ViewModels (Jour 1-5)

#### Jour 1: MainWindowViewModel
- [ ] Créer MainWindowViewModel.cs
- [ ] Ajouter 8 Commands (CreateProject, Browse, Export, etc.)
- [ ] Déplacer logique Create_Run, InitSettings, Validation
- [ ] Tester compilation
- [ ] **Commit**: "feat: Add MainWindowViewModel with all commands"

#### Jour 2: CRUDGeneratorViewModel
- [ ] Créer CRUDGeneratorViewModel.cs
- [ ] Ajouter Commands (Generate, Delete, Refresh, DeleteAnnotations)
- [ ] Observable Properties (SelectedDto, DtoList)
- [ ] Reactions automatiques (OnSelectedDtoChanged)
- [ ] **Commit**: "feat: Add CRUDGeneratorViewModel with commands"

#### Jour 3: OptionGeneratorViewModel
- [ ] Créer OptionGeneratorViewModel.cs
- [ ] Similar à CRUDGenerator (Generate, Delete, Refresh, etc.)
- [ ] Observable Properties
- [ ] **Commit**: "feat: Add OptionGeneratorViewModel with commands"

#### Jour 4: DtoGenerator + ModifyProject ViewModels
- [ ] Finaliser DtoGeneratorViewModel.cs
- [ ] Créer ModifyProjectViewModel.cs
- [ ] Commands pour Browse, Add, Delete, Open, Parse
- [ ] **Commit**: "feat: Complete Dto and ModifyProject ViewModels"

#### Jour 5: VersionAndOptionViewModel
- [ ] Créer VersionAndOptionViewModel.cs
- [ ] Observable Properties pour versions
- [ ] Reactions automatiques
- [ ] **Commit**: "feat: Add VersionAndOptionViewModel"
- [ ] **Commit Phase 4**: "feat: Phase 4 complete - All ViewModels created"

---

## 🛣️ Roadmap Phase 5

### Semaine 2: DI Container (Jour 6-8)

#### Jour 6-7: Éliminer Inject() Methods
- [ ] Supprimer MainWindow UserControl Inject() calls
- [ ] Supprimer CRUDGeneratorUC.Inject()
- [ ] Supprimer OptionGeneratorUC.Inject()
- [ ] Supprimer DtoGeneratorUC.Inject()
- [ ] Supprimer ModifyProjectUC.Inject()
- [ ] Remplacer par constructor injection partout
- [ ] **Commits**: Un par UserControl

#### Jour 8: App.xaml.cs Configuration
- [ ] Enregistrer tous les ViewModels
- [ ] Enregistrer tous les UserControls
- [ ] Configuration DI complète
- [ ] Tester résolution dépendances
- [ ] **Commit**: "feat: Complete DI Container configuration"
- [ ] **Commit Phase 5**: "refactor: Phase 5 complete - Service Locator eliminated"

---

## 🛣️ Roadmap Phase 6

### Semaine 2: XAML Bindings (Jour 8-10)

#### Jour 8-9: Convert Events to Commands
- [ ] MainWindow.xaml: Events → Command bindings
- [ ] CRUDGeneratorUC.xaml: Events → Commands
- [ ] OptionGeneratorUC.xaml: Events → Commands
- [ ] DtoGeneratorUC.xaml: Events → Commands
- [ ] ModifyProjectUC.xaml: Events → Commands
- [ ] VersionAndOption.xaml: Events → Commands
- [ ] **Commits**: Un par fichier XAML

#### Jour 10: Final Cleanup & Tests
- [ ] Supprimer event handlers des code-behind
- [ ] Code-behind finaux (~30-50 lignes)
- [ ] Tests E2E
- [ ] Vérifier toutes les fonctionnalités
- [ ] **Commit**: "refactor: Phase 6 complete - XAML bindings only"
- [ ] **Commit Final**: "feat: Complete MVVM transformation (Phases 4-6)"

---

## 🏁 Checklist Avant de Commencer

### Prérequis Techniques
- [ ] Avoir terminé Phases 1-3
- [ ] Tous les Helpers créés et fonctionnels
- [ ] IFileDialogService, IDialogService en place
- [ ] CommunityToolkit.Mvvm installé
- [ ] Git repository propre (pas de changements non commités)

### Prérequis Connaissance
- [ ] Comprendre MVVM pattern
- [ ] Connaître CommunityToolkit.Mvvm ([ObservableProperty], [RelayCommand])
- [ ] Comprendre Dependency Injection
- [ ] Lire ARCHITECTURE_PRINCIPLES.md

### Setup Environnement
- [ ] Créer branche `feature/phase-4-6-mvvm-complete`
- [ ] Backup du code actuel
- [ ] Tests unitaires existants passent

---

## 📝 Template Commit Messages

### Phase 4 (ViewModels)
```
feat(viewmodel): Add [Name]ViewModel with commands

- Create [Name]ViewModel.cs with ObservableObject
- Add RelayCommands: [Command1], [Command2], ...
- Add ObservableProperties: [Prop1], [Prop2], ...
- Move business logic from code-behind to ViewModel

Relates to Phase 4, Step [XX]
```

### Phase 5 (DI)
```
refactor(di): Remove Inject() from [ControlName]

- Delete public void Inject() method
- Add constructor injection for ViewModel
- Register control in App.xaml.cs DI configuration
- Update MainWindow to use DI-resolved control

Relates to Phase 5, Step [XX]
```

### Phase 6 (XAML)
```
refactor(xaml): Convert [ControlName] events to command bindings

- Replace Click events with Command bindings
- Replace SelectionChanged with SelectedItem bindings
- Remove event handlers from code-behind
- Code-behind reduced from [XXX] to [YY] lines (-ZZ%)

Relates to Phase 6, Step [XX]
```

---

## 🚦 Étape 27: Démarrage - MainWindowViewModel

### Fichier à Créer
`BIA.ToolKit.Application/ViewModel/MainWindowViewModel.cs`

### Code Template
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BIA.ToolKit.Application.Helper;
using BIA.ToolKit.Application.Services;

namespace BIA.ToolKit.Application.ViewModel;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly MainWindowHelper helper;
    private readonly IFileDialogService fileDialogService;
    private readonly IDialogService dialogService;
    private readonly ILogger<MainWindowViewModel> logger;
    
    public MainWindowViewModel(
        MainWindowHelper helper,
        IFileDialogService fileDialogService,
        IDialogService dialogService,
        ILogger<MainWindowViewModel> logger
    )
    {
        this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
        this.fileDialogService = fileDialogService;
        this.dialogService = dialogService;
        this.logger = logger;
        
        // Initialize
        InitializeAsync().FireAndForget();
    }
    
    #region Properties
    
    [ObservableProperty]
    private bool isWaiterVisible;
    
    [ObservableProperty]
    private string projectName = string.Empty;
    
    [ObservableProperty]
    private string projectRootFolder = string.Empty;
    
    // ... autres properties
    
    #endregion
    
    #region Commands
    
    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        try
        {
            IsWaiterVisible = true;
            
            // Validation
            if (string.IsNullOrEmpty(ProjectName))
            {
                await dialogService.ShowErrorAsync("Project name is required");
                return;
            }
            
            // Logique création via helper
            await helper.CreateProjectAsync(ProjectName, ProjectRootFolder);
            
            await dialogService.ShowSuccessAsync("Project created successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating project");
            await dialogService.ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsWaiterVisible = false;
        }
    }
    
    [RelayCommand]
    private async Task BrowseProjectFolderAsync()
    {
        var folder = await fileDialogService.OpenFolderDialogAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            ProjectRootFolder = folder;
        }
    }
    
    // ... autres commands
    
    #endregion
    
    #region Private Methods
    
    private async Task InitializeAsync()
    {
        try
        {
            await helper.InitializeSettingsAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing MainWindow");
        }
    }
    
    #endregion
}
```

### Prochaines Actions
1. Créer ce fichier
2. Identifier TOUS les event handlers dans MainWindow.xaml.cs
3. Créer un Command pour chacun
4. Déplacer la logique métier vers le ViewModel
5. Commit

---

## 📚 Ressources

### Documentation
- [REFACTORING_PHASE_4_6_PLAN.md](REFACTORING_PHASE_4_6_PLAN.md) - Plan détaillé
- [CODE_BEHIND_DETAILED_ANALYSIS.md](CODE_BEHIND_DETAILED_ANALYSIS.md) - Violations ligne par ligne
- [ARCHITECTURE_PRINCIPLES.md](ARCHITECTURE_PRINCIPLES.md) - Patterns à suivre
- [REFACTORING_TRACKING.md](REFACTORING_TRACKING.md) - Suivi progrès

### Références Externes
- [CommunityToolkit.Mvvm Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

---

## ✅ Critères de Succès Final

### Architecture
- [ ] Aucune logique métier dans code-behind
- [ ] Tous les ViewModels avec DI
- [ ] Aucune méthode Inject()
- [ ] Commands partout

### Code Quality
- [ ] Code-behind < 50 lignes
- [ ] ViewModels testables à 100%
- [ ] Respect SOLID principles
- [ ] Respect Clean Architecture

### Fonctionnel
- [ ] Toutes les fonctionnalités marchent
- [ ] Aucune régression
- [ ] Performance maintenue
- [ ] UI responsive

---

## 🤝 Support

En cas de questions ou blocages:
1. Consulter ARCHITECTURE_PRINCIPLES.md
2. Consulter CODE_BEHIND_DETAILED_ANALYSIS.md pour le fichier spécifique
3. Vérifier les examples dans REFACTORING_PHASE_4_6_PLAN.md

---

**Prêt à démarrer ? Commencer par l'Étape 27: MainWindowViewModel** 🚀

---

*Document créé le 22 janvier 2026*  
*Guide de démarrage Phases 4-6*
