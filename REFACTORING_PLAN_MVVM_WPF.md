# 📋 PLAN DE REFACTORISATION MVVM WPF - BIA.ToolKit

**Date:** 20 janvier 2026  
**Durée Estimée:** 12-17 jours  
**Objectif:** Extraire la logique métier des ViewModels WPF vers la couche Application/Services  
**Principes:** Clean Architecture + MVVM WPF Standard + Commits Réguliers

---

## 🏛️ ARCHITECTURE CIBLE

```
01 - PRÉSENTATION (BIA.ToolKit) ✅ ViewModels restent ici
├── Views/
│   ├── MainWindow.xaml(.cs)
│   ├── UserControls/
│   └── Dialogs/
│
└── ViewModels/                          ⭐ MINIFIÉS + OPTIMISÉS
    ├── Main/
    │   └── MainViewModel.cs
    ├── Generators/
    │   ├── CRUDGeneratorViewModel.cs     (150-200 lignes)
    │   ├── DtoGeneratorViewModel.cs      (150-200 lignes)
    │   └── OptionGeneratorViewModel.cs   (150 lignes)
    ├── Project/
    │   └── ModifyProjectViewModel.cs     (150-200 lignes)
    ├── Repository/
    │   ├── RepositoryViewModel.cs
    │   ├── RepositoryGitViewModel.cs
    │   └── RepositoryFolderViewModel.cs
    └── Shared/
        ├── VersionAndOptionViewModel.cs
        └── FeatureSettingViewModel.cs

02 - APPLICATION (BIA.ToolKit.Application) ✅ Logique métier
├── Services/                            ⭐ NOUVEAU
│   ├── CRUD/
│   │   ├── ICRUDGenerationService.cs
│   │   ├── CRUDGenerationService.cs
│   │   └── CRUDHistoryService.cs
│   ├── DTO/
│   │   ├── IDtoGenerationService.cs
│   │   ├── DtoGenerationService.cs
│   │   └── DtoHistoryService.cs
│   ├── Project/
│   │   ├── IProjectMigrationService.cs
│   │   └── ProjectMigrationService.cs
│   ├── Option/
│   │   ├── IOptionGenerationService.cs
│   │   └── OptionGenerationService.cs
│   └── FileGeneration/
│       └── IFileGenerationOrchestrator.cs
│
└── Helper/                              ⭐ DÉPLACÉ D'ICI
    ├── CRUD/
    │   ├── CRUDGeneratorHelper.cs
    │   └── CRUDHistoryHelper.cs
    ├── DTO/
    │   └── DtoGeneratorHelper.cs
    └── Option/
        └── OptionGeneratorHelper.cs

03 - DOMAIN (BIA.ToolKit.Domain) ✅ Contrats
└── Services/ (Interfaces existantes)

04 - INFRASTRUCTURE (BIA.ToolKit.Infrastructure) ✅ Implémentations
└── Services/ (Existantes)

10 - COMMON (BIA.ToolKit.Common) ✅ Utilitaires
```

---

## 📌 PRINCIPES CLEFS

### ✅ ViewModels (RESTENT en BIA.ToolKit/)
```csharp
// ⭐ Responsabilités: Adaptation données UI + Commandes UI
[ObservableProperty]
private string selectedFolder;

[RelayCommand]
private async Task GenerateCrudAsync()
{
    var result = await _crudService.GenerateAsync(request);
    OnPropertyChanged(nameof(Status));  // Mise à jour UI
}
```

### ✅ Services métier (VAS en Application/Services/)
```csharp
// ⭐ Responsabilités: Logique métier pure (sans UI)
public class CRUDGenerationService : ICRUDGenerationService
{
    public async Task<CRUDGenerationResult> GenerateAsync(
        CRUDGenerationRequest request)
    {
        // Parsing DTO
        // Génération CRUD
        // Sauvegarder historique
        return result;
    }
}
```

---

## 🎯 PHASE 1: CARTOGRAPHIE & IDENTIFICATION (1 jour)

### Objectif
Identifier précisément quelle logique métier doit être extraite de chaque ViewModel.

### Actions

#### 1.1 Analyser CRUDGeneratorViewModel (1410 lignes)
- Identifier parsing DTO → ICRUDGenerationService
- Identifier génération CRUD → ICRUDGenerationService
- Identifier gestion historique → CRUDHistoryService
- Identifier logique features → Helper/Service

#### 1.2 Analyser ModifyProjectViewModel (675 lignes)
- Identifier parsing solution → IProjectMigrationService
- Identifier logique migration → IProjectMigrationService
- Identifier gestion fichiers → IFileGenerationOrchestrator

#### 1.3 Analyser DtoGeneratorViewModel (767 lignes)
- Identifier parsing entités → IDtoGenerationService
- Identifier mapping propriétés → IDtoGenerationService
- Identifier historique → DtoHistoryService

#### 1.4 Analyser OptionGeneratorViewModel
- Identifier génération options → IOptionGenerationService
- Identifier historique → Helper/Service

#### 1.5 Analyser VersionAndOptionViewModel
- Identifier logique commune → Services existants

### Livrable
Document `EXTRACTION_MAPPING.txt` (dans racine projet)

### ✅ COMMIT APRÈS CETTE PHASE
```bash
git commit -m "docs: Phase 1 - Cartographie logique métier à extraire"
```

---

## 🎯 PHASE 2: CRÉER SERVICES MÉTIER (3-4 jours)

### Objectif
Créer les services métier en Application/Services avec interfaces définies.

### Actions

#### 2.1 Créer structure de dossiers
```
BIA.ToolKit.Application/Services/
├── CRUD/
├── DTO/
├── Project/
├── Option/
└── FileGeneration/
```

#### 2.2 Créer ICRUDGenerationService + Implémentation
- **Fichier:** `BIA.ToolKit.Application/Services/CRUD/ICRUDGenerationService.cs`
- **Interface:**
  ```csharp
  public interface ICRUDGenerationService
  {
      Task<CRUDGenerationResult> GenerateAsync(CRUDGenerationRequest request);
      Task<CRUDGeneration> GetHistoryAsync(string projectPath);
      Task DeleteLastGenerationAsync(string projectPath);
  }
  ```

- **Implémentation:** `BIA.ToolKit.Application/Services/CRUD/CRUDGenerationService.cs`
  - Extraire logique de `CRUDGeneratorViewModel.GenerateCrudAsync()`
  - Utiliser `CSharpParserService`, `GenerateCrudService`, `CRUDGeneratorHelper`

#### 2.3 Créer IDtoGenerationService + Implémentation
- **Fichier:** `BIA.ToolKit.Application/Services/DTO/IDtoGenerationService.cs`
- **Interface:**
  ```csharp
  public interface IDtoGenerationService
  {
      Task<EntityInfo[]> ParseEntitiesAsync(Project project);
      Task GenerateDtoAsync(DtoGenerationRequest request);
      Task<DtoGenerationHistory> GetHistoryAsync(string projectPath);
  }
  ```

#### 2.4 Créer IProjectMigrationService + Implémentation
- **Fichier:** `BIA.ToolKit.Application/Services/Project/IProjectMigrationService.cs`
- **Interface:**
  ```csharp
  public interface IProjectMigrationService
  {
      Task<MigrationResult> MigrateAsync(MigrationRequest request);
      Task<MigrationDiff> ApplyDiffAsync(Project project);
      Task MergeRejectedFilesAsync(Project project);
  }
  ```

#### 2.5 Créer IOptionGenerationService + Implémentation
- **Fichier:** `BIA.ToolKit.Application/Services/Option/IOptionGenerationService.cs`
- **Interface:**
  ```csharp
  public interface IOptionGenerationService
  {
      Task GenerateOptionAsync(OptionGenerationRequest request);
      Task<OptionGeneration> GetHistoryAsync(string projectPath);
  }
  ```

#### 2.6 Déplacer Helpers vers Application
- `BIA.ToolKit/ViewModels/CRUDGeneratorHelper.cs` → `BIA.ToolKit.Application/Helper/CRUD/CRUDGeneratorHelper.cs`
- `BIA.ToolKit/ViewModels/DtoGeneratorHelper.cs` → `BIA.ToolKit.Application/Helper/DTO/DtoGeneratorHelper.cs`
- `BIA.ToolKit/ViewModels/OptionGeneratorHelper.cs` → `BIA.ToolKit.Application/Helper/Option/OptionGeneratorHelper.cs`
- `BIA.ToolKit/ViewModels/MainWindowHelper.cs` → `BIA.ToolKit.Application/Helper/MainWindowHelper.cs`

### ✅ COMMIT APRÈS CETTE PHASE
```bash
git commit -m "feat(application): Phase 2 - Créer services métier (CRUD, DTO, Project, Option)"
```

---

## 🎯 PHASE 3: MINIFIER LES VIEWMODELS (3-4 jours)

### Objectif
Réduire les ViewModels en supprimant la logique métier et en les connectant aux services.

### Actions

#### 3.1 Minifier CRUDGeneratorViewModel
- **Avant:** 1410 lignes
- **Après:** 150-200 lignes
- **À supprimer:**
  - Parsing DTO (300 lignes) → Service
  - Génération CRUD (400 lignes) → Service
  - Gestion historique (200 lignes) → Service
  - Logique features (300 lignes) → Helper/Service

- **À garder:**
  - Propriétés UI (`[ObservableProperty]`)
  - Commandes simples (`[RelayCommand]`) qui délèguent
  - Construction du request
  - Mise à jour UI

- **Code cible:**
  ```csharp
  public partial class CRUDGeneratorViewModel : ObservableObject
  {
      private readonly ICRUDGenerationService _crudService;
      private readonly IDtoGenerationService _dtoService;
      
      [ObservableProperty]
      private Project currentProject;
      
      [ObservableProperty]
      private EntityInfo dtoEntity;
      
      [RelayCommand]
      private async Task GenerateCrudAsync()
      {
          var result = await _crudService.GenerateAsync(BuildRequest());
          IsDtoParsed = false;
      }
  }
  ```

#### 3.2 Minifier ModifyProjectViewModel
- **Avant:** 675 lignes
- **Après:** 150-200 lignes
- **À supprimer:** Logique migration, parsing, génération → Services
- **À garder:** Propriétés UI, commandes simples qui délèguent

#### 3.3 Minifier DtoGeneratorViewModel
- **Avant:** 767 lignes
- **Après:** 150-200 lignes
- **Même pattern:** Extraire logique, garder UI

#### 3.4 Minifier OptionGeneratorViewModel
- **Avant:** ~500 lignes
- **Après:** 120-150 lignes

#### 3.5 Mettre à jour autres ViewModels
- `VersionAndOptionViewModel`
- `RepositoryViewModel` et dérivés
- `FeatureSettingViewModel`

#### 3.6 Standardiser [ObservableProperty] partout
- Remplacer ancien pattern manuel par `[ObservableProperty]`
- Remplacer `ICommand` custom par `[RelayCommand]`

### ✅ COMMIT APRÈS CETTE PHASE
```bash
git commit -m "refactor(presentation): Phase 3 - Minifier ViewModels en délégant au services métier"
```

---

## 🎯 PHASE 4: RESTRUCTURER ARCHITECTURE (2 jours)

### Objectif
Organiser les dossiers de manière cohérente et configurer DI.

### Actions

#### 4.1 Réorganiser BIA.ToolKit/ViewModels/
```
ViewModels/
├── Main/
│   └── MainViewModel.cs
├── Generators/
│   ├── CRUDGeneratorViewModel.cs
│   ├── DtoGeneratorViewModel.cs
│   └── OptionGeneratorViewModel.cs
├── Project/
│   └── ModifyProjectViewModel.cs
├── Repository/
│   ├── RepositoryViewModel.cs
│   ├── RepositoryGitViewModel.cs
│   └── RepositoryFolderViewModel.cs
└── Shared/
    ├── VersionAndOptionViewModel.cs
    └── FeatureSettingViewModel.cs
```

#### 4.2 Créer Extension Methods pour DI
- **Fichier:** `BIA.ToolKit.Application/Extensions/ServiceCollectionExtensions.cs`

```csharp
namespace BIA.ToolKit.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCRUDServices(
        this IServiceCollection services)
    {
        services.AddScoped<ICRUDGenerationService, CRUDGenerationService>();
        services.AddScoped<CRUDHistoryService>();
        return services;
    }
    
    public static IServiceCollection AddDtoServices(
        this IServiceCollection services)
    {
        services.AddScoped<IDtoGenerationService, DtoGenerationService>();
        services.AddScoped<DtoHistoryService>();
        return services;
    }
    
    public static IServiceCollection AddProjectServices(
        this IServiceCollection services)
    {
        services.AddScoped<IProjectMigrationService, ProjectMigrationService>();
        return services;
    }
    
    public static IServiceCollection AddOptionServices(
        this IServiceCollection services)
    {
        services.AddScoped<IOptionGenerationService, OptionGenerationService>();
        return services;
    }
}
```

#### 4.3 Mettre à jour App.xaml.cs
```csharp
private void ConfigureServices(ServiceCollection services)
{
    // Infrastructure
    services.AddSingleton<IFileSystemService, FileSystemService>();
    services.AddScoped<IFileDialogService, FileDialogService>();
    
    // Application Services - Métier
    services.AddCRUDServices();
    services.AddDtoServices();
    services.AddProjectServices();
    services.AddOptionServices();
    
    // Presentation ViewModels
    services.AddTransient<MainViewModel>();
    services.AddTransient<CRUDGeneratorViewModel>();
    // ... etc.
}
```

### ✅ COMMIT APRÈS CETTE PHASE
```bash
git commit -m "chore(project): Phase 4 - Restructurer dossiers et configurer DI"
```

---

## 🎯 PHASE 5: NETTOYAGE TECHNIQUE (1-2 jours)

### Objectif
Supprimer les redondances et les anciennes approches.

### Actions

#### 5.1 Supprimer redondances
- ❌ Supprimer `BIA.ToolKit/Helper/MicroMvvm/RelayCommand.cs` (redondant avec CommunityToolkit)
- ❌ Supprimer `BIA.ToolKit/Mapper/VersionAndOptionMapper.cs` (si intégré ailleurs)
- ❌ Supprimer ancien pattern MVVM manuel des ViewModels (si tous migrés)

#### 5.2 Nettoyer BIA.ToolKit/Services/
- Vérifier que tous les services UI sont correctement placés
- Supprimer si déplacés ailleurs

#### 5.3 Vérifier code-behind minimal
- Pas de logique métier en code-behind
- Juste DI + DataContext

### ✅ COMMIT APRÈS CETTE PHASE
```bash
git commit -m "chore(cleanup): Phase 5 - Supprimer redondances et nettoyer code-behind"
```

---

## 🎯 PHASE 6: TESTS & VALIDATION (2 jours)

### Objectif
Valider que le refactoring est correct et complet.

### Actions

#### 6.1 Tests de compilation
- ✅ Compilation sans erreurs
- ✅ Pas d'avertissements non résolus
- ✅ Pas de références circulaires

#### 6.2 Tests d'intégraton DI
- ✅ Tous les services résolvent correctement
- ✅ ViewModels reçoivent les bonnes dépendances
- ✅ Application démarre sans erreur

#### 6.3 Tests fonctionnels
- ✅ Affichage UI correct
- ✅ Génération CRUD fonctionne
- ✅ Génération DTO fonctionne
- ✅ Modification projet fonctionne
- ✅ Historique sauvegardé/chargé correctement

#### 6.4 Créer tests unitaires (optionnel pour MVP)
- Tests pour services CRUD
- Tests pour services DTO
- Tests pour services Project

### ✅ COMMIT APRÈS CETTE PHASE
```bash
git commit -m "test(validation): Phase 6 - Tests et validation complète du refactoring"
```

---

## 🎯 PHASE 7: DOCUMENTATION (0.5 jour)

### Objectif
Documenter les changements pour les futurs développeurs.

### Actions

#### 7.1 Créer/Mettre à jour ARCHITECTURE.md
```
# Architecture BIA.ToolKit

## 4 Couches

### 01 - Présentation (BIA.ToolKit)
- ViewModels minifiés (150-200 lignes max)
- Views (XAML + code-behind minimaliste)

### 02 - Application (BIA.ToolKit.Application)
- Services métier (logique pure)
- Helpers (utilitaires métier)
- Messages (MVVM Messaging)

### 03 - Domain (BIA.ToolKit.Domain)
- Services (interfaces)
- Models
- Settings

### 04 - Infrastructure (BIA.ToolKit.Infrastructure)
- Services (implémentations techniques)
- Accès fichiers, Git, etc.
```

#### 7.2 Créer CODING_STANDARDS.md
- Pattern `[ObservableProperty]` obligatoire
- Pattern `[RelayCommand]` obligatoire
- Max 200 lignes par ViewModel
- Services injectés via DI

### ✅ COMMIT APRÈS CETTE PHASE
```bash
git commit -m "docs(architecture): Phase 7 - Documenter architecture et standards"
```

---

## 📊 CALENDRIER DE REFACTORING

| Phase | Description | Durée | Jours | Commit |
|-------|-------------|-------|-------|--------|
| 1️⃣ | Cartographie logique | 1 j | 1 | `chore: Phase 1 - Cartographie` |
| 2️⃣ | Créer Services métier | 3-4 j | 3-4 | `feat: Phase 2 - Services métier` |
| 3️⃣ | Minifier ViewModels | 3-4 j | 3-4 | `refactor: Phase 3 - ViewModels minifiés` |
| 4️⃣ | Restructurer architecture | 2 j | 2 | `chore: Phase 4 - Restructurer` |
| 5️⃣ | Nettoyage technique | 1-2 j | 1-2 | `chore: Phase 5 - Nettoyage` |
| 6️⃣ | Tests & Validation | 2 j | 2 | `test: Phase 6 - Validation complète` |
| 7️⃣ | Documentation | 0.5 j | 0.5 | `docs: Phase 7 - Documentation` |
| | **TOTAL** | | **12-17 jours** | |

---

## 🔄 PATTERN DE COMMITS RÉGULIERS

Après **chaque phase complétée**, exécuter:

```bash
# Phase 1
git add .
git commit -m "chore: Phase 1 - Cartographie logique métier à extraire"

# Phase 2
git add .
git commit -m "feat(application): Phase 2 - Créer services métier (CRUD/DTO/Project/Option)"

# Phase 3
git add .
git commit -m "refactor(presentation): Phase 3 - Minifier ViewModels en délégant services"

# Phase 4
git add .
git commit -m "chore(project): Phase 4 - Restructurer dossiers et configurer DI"

# Phase 5
git add .
git commit -m "chore(cleanup): Phase 5 - Supprimer redondances et nettoyer code"

# Phase 6
git add .
git commit -m "test(validation): Phase 6 - Tests et validation refactoring"

# Phase 7
git add .
git commit -m "docs(architecture): Phase 7 - Documenter architecture et standards"
```

---

## ⚠️ RISQUES & MITIGATIONS

| Risque | Sévérité | Mitigation |
|--------|----------|-----------|
| Compilation échoue en milieu de phase | 🟡 Moyen | Compiler après chaque fichier créé |
| Références circulaires créées | 🟡 Moyen | Vérifier dépendances régulièrement |
| ViewModel encore trop gros | 🟡 Moyen | Limiter strictement à 200 lignes max |
| DI cassée après changements | 🟡 Moyen | Tests DI réguliers |
| Services incomplets | 🟡 Moyen | Tester chaque service isolé |

---

## ✅ CHECKLIST PRE-EXECUTION

Avant de commencer, vérifier:

- [ ] Branche courante est clean (aucun changement non committé)
- [ ] Derniers changements sont poussés sur origin
- [ ] Compiler le projet actuellement - ✅ OK
- [ ] Application démarre correctement
- [ ] Tous les fichiers MD anciens supprimés
- [ ] Ce plan est créé et commité

---

## 🚀 PROCHAINE ÉTAPE

**QUAND VOUS ÊTES PRÊT:**
```
Exécuter le plan en commençant par Phase 1
```

**POUR VÉRIFIER PROGRESSION:**
```bash
git log --oneline | grep "Phase"
```

---

## 📎 FICHIERS PRINCIPAUX À MODIFIER

### Phase 1
- Créer: `EXTRACTION_MAPPING.txt`

### Phase 2
- Créer: Services métier en `BIA.ToolKit.Application/Services/`
- Créer: Helpers en `BIA.ToolKit.Application/Helper/`
- Créer: Extension methods

### Phase 3
- Modifier: `BIA.ToolKit/ViewModels/*.cs` (tous)
- Mettre à jour imports

### Phase 4
- Créer: Nouvelle structure de dossiers
- Modifier: `App.xaml.cs`

### Phase 5
- Supprimer: Fichiers redondants

### Phase 6
- Tester: Compilation, DI, UI

### Phase 7
- Créer: `ARCHITECTURE.md`
- Créer: `CODING_STANDARDS.md`

---

**Version:** 1.0  
**Créé:** 20 janvier 2026  
**Status:** 🟡 EN ATTENTE D'EXÉCUTION
