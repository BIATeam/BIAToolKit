# Plan de Refactorisation - Suivi d'Implémentation

**Date de Création**: 19 janvier 2026  
**Dernière Mise à Jour**: 22 janvier 2026 - **Phase 4 Step 27 COMPLETE**  
**Statut Global**: ⚠️ Phases 1-3 Incomplètes - Phases 4-6 EN COURS

---

## ⚠️ CONSTAT IMPORTANT - Architecture Non Conforme

### Problèmes Identifiés Après Phase 3

Bien que les phases 1-3 aient créé l'infrastructure (helpers, services), **la transformation MVVM n'est PAS complète**:

**❌ Violations Clean Architecture:**
- 5 méthodes `Inject()` présentes (anti-pattern Service Locator)
- ~2,000 lignes de logique métier toujours dans code-behind
- Helpers appelés depuis code-behind au lieu de ViewModels
- 16+ event handlers avec logique métier (devrait être Commands)
- ViewModels incomplets ou absents

**✅ Ce Qui Fonctionne:**
- Infrastructure services (IFileDialogService, ITextParsingService, IDialogService)
- Helpers créés (MainWindowHelper, CRUDGeneratorHelper, etc.)
- DI configuration de base

**📋 Nouveau Plan:**
Phases 4-6 ajoutées pour transformation MVVM complète.  
Voir: **[REFACTORING_PHASE_4_6_PLAN.md](REFACTORING_PHASE_4_6_PLAN.md)**

---

## 📊 Vue d'Ensemble des 26 Étapes

### PHASE 1: Infrastructure & Services (Étapes 1-5)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 1 | Créer IFileDialogService | Interface abstraite pour file browse | ✅ Terminé | 2h | Commit: 3eeee2a |
| 2 | Implémenter FileDialogService | Implementation enveloppes FileDialog | ✅ Terminé | 2h | Commit: 3eeee2a |
| 3 | Créer ITextParsingService | Service parsage texte/noms | ✅ Terminé | 2h | Commit: 3eeee2a |
| 4 | Créer IDialogService | Service gestion dialogs | ✅ Terminé | 3h | Commit: 3eeee2a |
| 5 | Enregistrer services DI | Configuration DI (App.xaml.cs) | ✅ Terminé | 1h | Commit: 3eeee2a |

**Estimation Phase 1**: 10 heures ✅ **COMPLÉTÉ**

---

### PHASE 2: ViewModel Refactoring - MainWindow (Étapes 6-10)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 6 | Analyser MainWindow.xaml.cs | Documenter responsabilités | ✅ Terminé | 1h | Voir ANALYSIS_CODE_BEHIND.md |
| 7 | Créer MainWindowHelper | Extraction logique métier (230 lignes) | ✅ Terminé | 3h | Commit: 3eeee2a |
| 8 | Extraire RepositoryValidation | Validation repositories (DRY) | ✅ Terminé | 2h | Commit: 3eeee2a |
| 9 | Refactoriser MainWindow.xaml.cs | 566 → ~490 lignes (13% réduction) | ✅ Terminé | 3h | Commits: 3eeee2a, a2d5e0d |
| 10 | Inject IFileDialogService | Éliminer dépendances statiques | ✅ Terminé | 1h | Commit: a2d5e0d |

**Estimation Phase 2**: 10 heures ✅ **COMPLÉTÉ**

---

### PHASE 3: ViewModel Refactoring - UserControls (Étapes 11-18)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 14 | Refactoriser ModifyProjectUC | Ajouter IFileDialogService | ✅ Terminé | 2h | Commits: 6980291, a2d5e0d |
| 15 | Refactoriser RepositoryFormUC | IFileDialogService injection | ✅ Terminé | 0.5h | Commit: 3eeee2a |
| 11 | Refactoriser CRUDGeneratorUC | 795 → TBD lignes | ⬜ Pas Commencé | 5j | **PROCHAIN** |
| 12 | Refactoriser DtoGeneratorUC | 650 → 180 lignes (72% réduction) | ⬜ Pas Commencé | 4j | **CRITIQUE** |
| 13 | Refactoriser OptionGeneratorUC | 500 → 150 lignes (70% réduction) | ⬜ Pas Commencé | 3j | **IMPORTANTE** |
| 16 | Refactoriser VersionAndOptionUserControl | DRY cleanup | ⬜ Pas Commencé | 1j | Simple |
| 17 | Refactoriser LabeledField | Documentation (peu de changements) | ⬜ Pas Commencé | 0.25j | OK déjà |
| 18 | Refactoriser Dialog Controls | LogDetail, CustomTemplate* | ⬜ Pas Commencé | 1j | YAGNI included |

**Estimation Phase 3**: 16.75 jours (3/8 étapes complétées = 37.5%)

---

### PHASE 4: Analyse & Bonnes Pratiques (Étapes 19-26)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 19 | Appliquer SRP | Analyser/documenter violations | ⬜ Pas Commencé | 2h | Parallèle avec phases 2-3 |
| 20 | Appliquer DRY | Identifier/fusionner duplications | ⬜ Pas Commencé | 2h | Code review |
| 21 | Appliquer YAGNI | Lister/supprimer code mort | ⬜ Pas Commencé | 3h | CustomTemplate dialogs |
| 22 | Appliquer KISS | Simplifier logique complexe | ⬜ Pas Commencé | 2h | Refactoring méthodes |
| 23 | Appliquer OCP | Extensibilité sans modification | ⬜ Pas Commencé | 2h | Design review |
| 24 | Appliquer DIP | Dépend abstractions, pas concrétions | ⬜ Pas Commencé | 2h | Vérification DI |
| 25 | Appliquer LSP | Substitution polymorphe sûre | ⬜ Pas Commencé | 1h | Repository variants |
| 26 | Appliquer ISP | Interfaces ciblées, pas génériques | ⬜ Pas Commencé | 1h | IRepository split |

**Estimation Phase 4**: 15 heures

---

### PHASE 4 (NOUVEAU): ViewModels Complets - MVVM Transformation (Étapes 27-32)

**Objectif**: Créer tous les ViewModels avec Commands, éliminer event handlers du code-behind

| # | Étape | Description | Statut | Effort | Commits |
|---|-------|-------------|--------|--------|---------|
| 27 | MainWindowViewModel | 11 Commands, 6 Observable Properties | ✅ Terminé | 4h | 18d65fe, d096cfe |
| 28 | CRUDGeneratorViewModel | 4 Commands (Generate, Delete, Refresh, DeleteAnnotations) | ⬜ Pas Commencé | 3h | - |
| 29 | OptionGeneratorViewModel | 3 Commands (Generate, Delete, Refresh) | ⬜ Pas Commencé | 3h | - |
| 30 | DtoGeneratorViewModel | 2 Commands (Generate, BrowseFile) | ⬜ Pas Commencé | 2h | - |
| 31 | ModifyProjectViewModel | 5 Commands (Browse, Modify, UpdateZip, DeleteAllGen) | ⬜ Pas Commencé | 3h | - |
| 32 | VersionAndOptionViewModel | 2 Commands (SaveBefore, SaveAfter) | ⬜ Pas Commencé | 1.5h | - |

**Estimation Phase 4 (Nouveau)**: 16.5 heures (≈ 2 jours)

**Détails Step 27 - MainWindowViewModel** (✅ TERMINÉ):
- **Fichier**: [MainWindowViewModel.cs](BIA.ToolKit/Application/ViewModel/MainWindowViewModel.cs)
- **Lignes**: 555 lignes
- **Commands**:
  1. `BrowseCreateProjectRootFolderCommand` - Browse root projects folder
  2. `CreateProjectCommand` - Create new project (async)
  3. `BrowseFileGeneratorFolderCommand` - Browse file generator folder
  4. `BrowseFileGeneratorFileCommand` - Browse file generator file
  5. `GenerateFilesCommand` - Generate files from template (async)
  6. `UpdateCommand` - Update BIA framework (async)
  7. `CheckForUpdatesCommand` - Check for updates (async)
  8. `CopyConsoleToClipboardCommand` - Copy console output
  9. `ClearConsoleCommand` - Clear console
  10. `ImportConfigCommand` - Import configuration (async)
  11. `ExportConfigCommand` - Export configuration (async)
- **Observable Properties**:
  - `IsWaiterVisible` - Loading indicator
  - `CreateProjectName` - Project name input
  - `Settings_RootProjectsPath` - Root projects path
  - `FileGeneratorFolder` - Generator folder path
  - `FileGeneratorFile` - Generator file path
  - `IsFileGeneratorGenerateEnabled` - Generate button enabled state
- **Services Injected**: 13 dependencies (RepositoryService, GitService, CSharpParserService, GenerateFilesService, ProjectCreatorService, SettingsService, IConsoleWriter, FileGeneratorService, IMessenger, UpdateService, IFileDialogService, IDialogService, ILogger)
- **Commits**: 
  - `18d65fe` - Initial MainWindowViewModel creation
  - `d096cfe` - Fixed IFileDialogService ambiguity & Clean Architecture violations
- **Architecture Compliance**: ✅ No UI dependencies (System.Windows, System.Windows.Forms)

---

### PHASE 5 (NOUVEAU): Élimination Inject() Anti-Pattern (Étapes 33-38)

**Objectif**: Remplacer 5 méthodes `Inject()` par Constructor Injection propre

| # | Étape | Description | Statut | Effort | Commits |
|---|-------|-------------|--------|--------|---------|
| 33 | Éliminer MainWindow.Inject() | Constructor injection (13 params) | ⬜ Pas Commencé | 1h | - |
| 34 | Éliminer CRUDGeneratorUC.Inject() | Constructor injection (8 params) | ⬜ Pas Commencé | 1h | - |
| 35 | Éliminer DtoGeneratorUC.Inject() | Constructor injection (6 params) | ⬜ Pas Commencé | 1h | - |
| 36 | Éliminer OptionGeneratorUC.Inject() | Constructor injection (6 params) | ⬜ Pas Commencé | 1h | - |
| 37 | Éliminer ModifyProjectUC.Inject() | Constructor injection (10 params) | ⬜ Pas Commencé | 1h | - |
| 38 | Update App.xaml.cs DI Registration | Register all ViewModels in DI container | ⬜ Pas Commencé | 0.5h | - |

**Estimation Phase 5**: 5.5 heures

---

### PHASE 6 (NOUVEAU): XAML Command Bindings & Finalization (Étapes 39-44)

**Objectif**: Câbler Commands dans XAML, éliminer tous les event handlers Click

| # | Étape | Description | Statut | Effort | Commits |
|---|-------|-------------|--------|--------|---------|
| 39 | Update MainWindow.xaml | Replace 11 Click events with Command bindings | ⬜ Pas Commencé | 2h | - |
| 40 | Update CRUDGeneratorUC.xaml | Replace 4 Click events with Command bindings | ⬜ Pas Commencé | 1h | - |
| 41 | Update OptionGeneratorUC.xaml | Replace 3 Click events with Command bindings | ⬜ Pas Commencé | 1h | - |
| 42 | Update DtoGeneratorUC.xaml | Replace 2 Click events with Command bindings | ⬜ Pas Commencé | 0.5h | - |
| 43 | Update ModifyProjectUC.xaml | Replace 5 Click events with Command bindings | ⬜ Pas Commencé | 1h | - |
| 44 | Final Cleanup & Testing | Remove unused event handlers, test all Commands | ⬜ Pas Commencé | 1.5h | - |

**Estimation Phase 6**: 7 heures

---

**TOTAL PHASES 4-6**: 29 heures (≈ 3.5 jours)

**Métriques Cibles**:
- 92% code-behind reduction (2,560 → 195 lignes)
- 0 Inject() methods (actuellement 5)
- 0 event handlers avec logique (actuellement 16+)
- 30+ Commands across 6 ViewModels
- 95% testability (ViewModels testables sans UI)

---

### PHASE 2: ViewModel Refactoring - MainWindow (Étapes 6-10)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 6 | Analyser MainWindow.xaml.cs | Documenter responsabilités | ⬜ Pas Commencé | 1h | Après Phase 1 |
| 7 | Créer MainWindowInitializationViewModel | Initialisation app/settings | ⬜ Pas Commencé | 3h | Après étape 6 |
| 8 | Créer RepositoryValidationViewModel | Validation repositories | ⬜ Pas Commencé | 2h | Après étape 6 |
| 9 | Refactoriser MainWindow.xaml.cs | Réduire de 556 à ~150 lignes | ⬜ Pas Commencé | 3h | Après 7-8 |
| 10 | Créer MainWindowCompositionRoot | Centraliser DI composition | ⬜ Pas Commencé | 1h | Après 7-8-9 |

**Estimation Phase 2**: 10 heures

---

### PHASE 3: ViewModel Refactoring - UserControls (Étapes 11-18)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 11 | Refactoriser CRUDGeneratorUC | 785 → 706 lignes (10% réduction) | ✅ Terminé | 5j | **CRITIQUE** - Helper créé |
| 12 | Refactoriser DtoGeneratorUC | 650 → 199 lignes (69% réduction) | ✅ Terminé | 4j | **CRITIQUE** - Déjà refactorisé |
| 13 | Refactoriser OptionGeneratorUC | 549 → 488 lignes (11% réduction) | ✅ Terminé | 3j | **IMPORTANTE** - Helper créé |
| 14 | Refactoriser ModifyProjectUC | Ajouter IFileDialogService | ✅ Terminé | 2j | Moyenne priorité |
| 15 | Refactoriser RepositoryFormUC | 60 → 20 lignes (67% réduction) | ✅ Terminé | 0.5j | Simple |
| 16 | Refactoriser VersionAndOptionUserControl | DRY cleanup | ✅ Terminé | 1j | LoadRepositoriesFromSettings helper |
| 17 | Refactoriser LabeledField | Documentation (peu de changements) | ✅ Terminé | 0.25j | Déjà bien fait (47 lignes) |
| 18 | Refactoriser Dialog Controls | LogDetail, CustomTemplate* | ✅ Terminé | 1j | YAGNI: -82 lignes commentées |

**Estimation Phase 3**: 16.75 jours (équivalent: ~3 semaines) - **✅ 8/8 étapes COMPLÉTÉES (100%)**

#### 📝 Détails Étape 16: VersionAndOptionUserControl (Terminé)

**Objectif**: Appliquer DRY cleanup pour éliminer duplications

**Travail Effectué**:
1. ✅ Création de `LoadRepositoriesFromSettings()` - Méthode helper pour charger repositories
2. ✅ Refactorisation `RefreshConfiguration()` - Élimination duplication de foreach
3. ✅ Simplification assignation `useCompanyFiles` - Variable locale pour DRY

**Résultats**:
- Code-Behind: **230 → 233 lignes** (+3 lignes pour abstraction DRY)
- **Principes appliqués**: DRY (Don't Repeat Yourself), méthode helper réutilisable
- Amélioration: Lisibilité et maintenabilité accrue

#### 📝 Détails Étape 17: LabeledField (Déjà OK)

**Statut**: Fichier déjà bien structuré, pas de refactorisation nécessaire

**Résultats**:
- Code-Behind: **47 lignes** - Simple et propre
- DependencyProperties bien définies
- ContentProperty correctement implémentée

#### 📝 Détails Étape 18: Dialog Controls (Terminé)

**Objectif**: Nettoyer code YAGNI (code mort/commenté)

**Travail Effectué**:
1. ✅ [CustomTemplatesRepositoriesSettingsUC.xaml.cs](BIA.ToolKit/Dialogs/CustomTemplatesRepositoriesSettingsUC.xaml.cs)
   - Suppression code commenté (ShowDialog, edit, delete, sync methods)
   - Nettoyage usings inutilisés (12 → 4 usings)
   - Ajout TODOs pour fonctionnalités futures
   
2. ✅ [CustomTemplateRepositorySettingsUC.xaml.cs](BIA.ToolKit/Dialogs/CustomTemplateRepositorySettingsUC.xaml.cs)
   - Suppression ShowDialog commenté
   - Nettoyage usings inutilisés (13 → 1 using)
   
3. ✅ [LogDetailUC.xaml.cs](BIA.ToolKit/Dialogs/LogDetailUC.xaml.cs)
   - Amélioration commentaire XML
   - Nettoyage usings inutilisés (11 → 4 usings)

**Résultats**:
- CustomTemplatesRepositoriesSettingsUC: **107 → 55 lignes** (-52 lignes, 49%)
- CustomTemplateRepositorySettingsUC: **42 → 22 lignes** (-20 lignes, 48%)
- LogDetailUC: **51 → 41 lignes** (-10 lignes, 20%)
- **Total réduit**: -82 lignes de code mort/usings inutiles
- **Principes appliqués**: YAGNI (You Aren't Gonna Need It)

---

#### 📝 Détails Étape 11: CRUDGeneratorUC (Terminé)

**Objectif**: Refactoriser CRUDGeneratorUC.xaml.cs en extrayant la logique de gestion des historiques et paramètres

**Travail Effectué**:
1. ✅ Création de [CRUDGeneratorHelper.cs](BIA.ToolKit/ViewModels/CRUDGeneratorHelper.cs) (276 lignes)
   - `InitializeSettings()`: Charge settings back/front + historique + feature names
   - `LoadFrontSettings()`: Charge paramètres Angular front
   - `LoadDtoHistory()`: Récupère historique pour un DTO
   - `UpdateHistory()`: Sauvegarde historique de génération CRUD
   - `DeleteHistory()`: Supprime entrée historique + cleanup options
   - `GetGeneratedOptions()`: Liste options déjà générées
   - `GetHistoriesUsingOption()`: Trouve historiques utilisant une option spécifique

2. ✅ Refactorisation [CRUDGeneratorUC.xaml.cs](BIA.ToolKit/UserControls/CRUDGeneratorUC.xaml.cs)
   - Suppression du champ `crudHistoryFileName`
   - Ajout du champ `crudHelper` (CRUDGeneratorHelper)
   - `SetGenerationSettings()`: Délègue à `crudHelper.InitializeSettings()`
   - `SetFrontGenerationSettings()`: Délègue à `crudHelper.LoadFrontSettings()`
   - `ModifyDto_SelectionChange()`: Utilise `LoadDtoHistory()` et `GetGeneratedOptions()`
   - `UpdateCrudGenerationHistory()`: Simplifié avec `crudHelper.UpdateHistory()`
   - `DeleteLastGenerationHistory()`: Délègue à `crudHelper.DeleteHistory()`
   - `DeleteLastGeneration_Click()`: Utilise `LoadDtoHistory()` et `GetHistoriesUsingOption()`

**Résultats**:
- Code-Behind: **785 → 706 lignes** (79 lignes supprimées, 10% réduction)
- Helper créé: 276 lignes de logique testable
- **Principes appliqués**: SRP (Single Responsibility), DRY (Don't Repeat Yourself)
- Compilation: ✅ Sans erreurs

#### 📝 Détails Étape 12: DtoGeneratorUC (Déjà Terminé)

**Statut**: Cette étape avait déjà été complétée dans un commit précédent (210ccd7)

**Résultats**:
- Code-Behind: **650 → 199 lignes** (451 lignes supprimées, 69% réduction)
- Helper [DtoGeneratorHelper.cs](BIA.ToolKit/ViewModels/DtoGeneratorHelper.cs) déjà existant
- **Principes appliqués**: SRP, DRY, extraction business logic hors de l'UI

#### 📝 Détails Étape 13: OptionGeneratorUC (Terminé)

**Objectif**: Refactoriser OptionGeneratorUC.xaml.cs en extrayant la logique de gestion des historiques et paramètres

**Travail Effectué**:
1. ✅ Création de [OptionGeneratorHelper.cs](BIA.ToolKit/ViewModels/OptionGeneratorHelper.cs) (235 lignes)
   - `InitializeSettings()`: Charge settings back/front + historique
   - `LoadFrontSettings()`: Charge paramètres Angular front
   - `LoadEntityHistory()`: Récupère historique pour une entité
   - `UpdateHistory()`: Sauvegarde historique de génération
   - `DeleteHistory()`: Supprime entrée historique
   - `GetGeneratedOptions()`: Liste options déjà générées

2. ✅ Refactorisation [OptionGeneratorUC.xaml.cs](BIA.ToolKit/UserControls/OptionGeneratorUC.xaml.cs)
   - Suppression du champ `optionHistoryFileName`
   - Ajout du champ `optionHelper` (OptionGeneratorHelper)
   - `SetGenerationSettings()`: Délègue à `optionHelper.InitializeSettings()`
   - `SetFrontGenerationSettings()`: Délègue à `optionHelper.LoadFrontSettings()`
   - `ModifyEntity_SelectionChange()`: Utilise `LoadEntityHistory()` et `GetGeneratedOptions()`
   - `UpdateOptionGenerationHistory()`: Simplifié avec `optionHelper.UpdateHistory()`
   - `DeleteLastGenerationHistory()`: Délègue à `optionHelper.DeleteHistory()`
   - `DeleteLastGeneration_Click()`: Utilise `LoadEntityHistory()`

**Résultats**:
- Code-Behind: **549 → 488 lignes** (61 lignes supprimées, 11% réduction)
- Helper créé: 235 lignes de logique testable
- **Principes appliqués**: SRP (Single Responsibility), DRY (Don't Repeat Yourself)
- Compilation: ✅ Sans erreurs

**Prochaines Étapes**:
- Tests unitaires pour OptionGeneratorHelper
- Tests d'intégration pour OptionGeneratorUC
- Documentation XML complète

---

## 📈 Graphique de Progression

```
PHASE 1: Infrastructure        ██████ (10h)        [Semaine 0]
PHASE 2: MainWindow            ██████ (10h)        [Semaine 1]
PHASE 3: UserControls          ██████████████ (17h) [Semaines 2-3]
PHASE 4: Analyse/QA            ██████ (15h)        [Concurrent]
TESTS & DOCUMENTATION          ███ (5h)            [Semaine 4]

─────────────────────────────────────────────────
TOTAL:                         ~57 heures           ~4 semaines
```

---

## 📋 Matrices de Dépendances

### Dépendances strictes (Ordonnées)

```
Étape 1 (IFileDialogService)
    ├→ Étape 2 (FileDialogService)
    └→ Étape 5 (DI registration)
        ├→ Étape 14 (ModifyProjectUC uses it)
        └→ Étape 15 (RepositoryFormUC uses it)

Étape 3 (ITextParsingService)
    ├→ Étape 5 (DI registration)
    └→ Étape 11 (CRUDGeneratorUC uses it)

Étape 4 (IDialogService)
    ├→ Étape 5 (DI registration)
    └→ Étape 18 (Dialog controls use it)

Étape 6 (Analyse MainWindow)
    ├→ Étape 7 (MainWindowInitializationViewModel)
    ├→ Étape 8 (RepositoryValidationViewModel)
    └→ Étape 9 (Refactor MainWindow)
        └→ Étape 10 (CompositionRoot)
```

### Dépendances optionnelles (Parallèles)

```
Phase 1 (Services) peut être parallèle avec Phase 4 (Analyse)
Phase 2 (MainWindow) peut être parallèle avec Phase 3 (UserControls)
Étapes 11-18 (UserControls) peuvent être parallèles entre elles
```

---

## 🎯 Jalons Importants

### Jalons par Semaine

**Semaine 0 (Préparation)**
- [ ] Approbation du plan
- [x] Documentation finalisée
- [ ] Team training sur patterns
- [ ] Configuration outils (SonarQube, etc.)
- **Résultat**: Infrastructure services prête

**Semaine 1 (Phase 2)**
- [ ] MainWindowInitializationViewModel implémentée
- [ ] RepositoryValidationViewModel implémentée
- [ ] MainWindow.xaml.cs refactorisée
- [ ] Tests MainWindow passants
- **Résultat**: MainWindow >80% testable

**Semaine 2-3 (Phase 3 - Batch 1)**
- [ ] CRUDGeneratorUC refactorisée (75% réduction)
- [ ] DtoGeneratorUC refactorisée (72% réduction)
- [ ] OptionGeneratorUC refactorisée (70% réduction)
- [ ] Tests UserControls passants
- **Résultat**: 3 composants majeurs refactorisés

**Semaine 3 (Phase 3 - Batch 2)**
- [ ] ModifyProjectUC, RepositoryFormUC refactorisées
- [ ] Dialog controls nettoyés (YAGNI)
- [ ] Tests passants
- **Résultat**: Tous UserControls modernes

**Semaine 4 (Finalisation)**
- [ ] Tests complets (coverage >80%)
- [ ] Code review SOLID completed
- [ ] Performance validation
- [ ] Documentation mise à jour
- **Résultat**: Production-ready

---

## 🔄 Cycles de Révision

### Code Review Cycle

```
Pour chaque étape:
  1. Implémentation (dev)
  2. Unit tests (dev) → >80% coverage
  3. Code review SOLID (tech lead)
  4. Integration tests (QA)
  5. Merge à main

Estimé: 1 jour par ViewModel + UserControl
```

### Architecture Review

```
Checkpoints:
  - Après Phase 1 (Services OK?)
  - Après Phase 2 (MainWindow architecture OK?)
  - Après Phase 3 (UserControls patterns OK?)
  - Avant merge (overall structure?)
```

---

## 📊 Métriques à Tracker

### Quantitatives

| Métrique | Avant | Cible | Unité |
|----------|-------|-------|-------|
| Code-Behind Total | 3,431 | 880 | lignes |
| Complexité Cyclomatique Moyenne | 22 | 4 | n/a |
| Test Coverage | 10% | >80% | % |
| SonarQube Grade | D | A | grade |

### Qualitatives

| Aspect | Avant | Après |
|--------|-------|-------|
| **Maintenabilité** | 😞 Difficile | 😊 Facile |
| **Testabilité** | 😞 Impossible | 😊 Triviale |
| **Lisibilité** | 😞 Confuse | 😊 Claire |
| **Extensibilité** | 😞 Fermée | 😊 Ouverte |

---

## 🚨 Risques et Mitigations

| Risque | Probabilité | Impact | Mitigation |
|--------|-------------|--------|-----------|
| Régression fonctionnelle | Moyenne | Critique | Tests complets avant merge |
| Performance dégradée | Basse | Majeure | Profiling avant/après |
| Merge conflicts | Moyenne | Mineure | Petites PR, CI/CD |
| Team skills gap | Moyenne | Majeure | Training sur patterns |
| Timeline slippage | Moyenne | Majeure | Buffer 20% (12j au lieu de 10j) |

---

## 📋 Checklist Pré-Lancement

### Préparation Technique
- [ ] Repository créé (git)
- [ ] Branches configurées (main, develop, feature/*)
- [ ] CI/CD pipeline en place
- [ ] SonarQube/CodeCov connectés
- [ ] Tests framework configurés (xUnit)

### Préparation Humaine
- [ ] Team training SOLID (2h)
- [ ] Team training MVVM patterns (2h)
- [ ] Team training CommunityToolkit.Mvvm (1h)
- [ ] Pair programming sessions planifiées
- [ ] Code review guidelines partagées

### Préparation Documentation
- [ ] Architecture diagram créé
- [ ] ViewModel naming conventions documentées
- [ ] Service interfaces documented
- [ ] Message classes documented
- [ ] DI composition documented

### Baseline
- [ ] Snapshot métrique SonarQube (Day 0)
- [ ] Snapshot tests (Day 0)
- [ ] Snapshot performance (Day 0)

---

## 📅 Estimation Détaillée par Rôle

### Développeur Principal (Full Time)

```
Semaine 0: Préparation (10h)
  - Services implémentation
  - Team training
  
Semaine 1: MainWindow (10h)
  - ViewModels refactoring
  - Tests

Semaine 2-3: UserControls (17h)
  - Gros refactorings (CRUD, DTO, Option)
  - Tests

Semaine 4: Finalisation (5h)
  - Documentation
  - Performance review

TOTAL: ~42 heures (5-6 jours travail)
```

### Tech Lead (Part Time 30%)

```
Review code: 12h (3h/semaine)
Architecture: 6h (1.5h/semaine)
Training: 4h (2h semaine 0)

TOTAL: ~22 heures
```

### QA (Part Time 50%)

```
Test planning: 4h
Integration tests: 12h
Regression testing: 8h
Performance: 4h

TOTAL: ~28 heures
```

---

## 📋 PHASES 4-6: Transformation MVVM Complète

**Voir document détaillé**: [REFACTORING_PHASE_4_6_PLAN.md](REFACTORING_PHASE_4_6_PLAN.md)

### PHASE 4: ViewModels Complets (Étapes 27-32)

| # | Étape | Description | Statut | Effort | Commit |
|---|-------|-------------|--------|--------|--------|
| 27 | MainWindowViewModel | Commands + logique métier complète | ⬜ À faire | 1j | - |
| 28 | CRUDGeneratorViewModel | Commands + Observable Properties | ⬜ À faire | 1j | - |
| 29 | OptionGeneratorViewModel | Commands + Observable Properties | ⬜ À faire | 1j | - |
| 30 | DtoGeneratorViewModel | Finaliser + Commands | ⬜ À faire | 0.5j | - |
| 31 | ModifyProjectViewModel | Commands complets | ⬜ À faire | 0.5j | - |
| 32 | VersionAndOptionViewModel | Commands + éliminer event handlers | ⬜ À faire | 0.5j | - |

**Estimation Phase 4**: 4.5 jours

---

### PHASE 5: Éliminer Service Locator Pattern (Étapes 33-38)

| # | Étape | Description | Statut | Effort | Commit |
|---|-------|-------------|--------|--------|--------|
| 33 | MainWindow - Éliminer Inject() | Constructor DI pure | ⬜ À faire | 0.5j | - |
| 34 | CRUDGeneratorUC - Éliminer Inject() | Constructor DI + DI Container | ⬜ À faire | 0.5j | - |
| 35 | OptionGeneratorUC - Éliminer Inject() | Constructor DI + DI Container | ⬜ À faire | 0.5j | - |
| 36 | DtoGeneratorUC - Éliminer Inject() | Constructor DI + DI Container | ⬜ À faire | 0.25j | - |
| 37 | ModifyProjectUC - Éliminer Inject() | Constructor DI + DI Container | ⬜ À faire | 0.25j | - |
| 38 | App.xaml.cs - DI Complet | Configuration DI centralisée | ⬜ À faire | 0.5j | - |

**Estimation Phase 5**: 2.5 jours

---

### PHASE 6: XAML Refactoring (Étapes 39-44)

| # | Étape | Description | Statut | Effort | Commit |
|---|-------|-------------|--------|--------|--------|
| 39 | MainWindow.xaml | Events → Command bindings | ⬜ À faire | 0.5j | - |
| 40 | CRUDGeneratorUC.xaml | Events → Command bindings | ⬜ À faire | 0.5j | - |
| 41 | OptionGeneratorUC.xaml | Events → Command bindings | ⬜ À faire | 0.5j | - |
| 42 | DtoGeneratorUC.xaml | Events → Command bindings | ⬜ À faire | 0.25j | - |
| 43 | ModifyProjectUC.xaml | Events → Command bindings | ⬜ À faire | 0.25j | - |
| 44 | VersionAndOption.xaml | Events → Command bindings | ⬜ À faire | 0.25j | - |

**Estimation Phase 6**: 2.25 jours

---

### 📊 Métriques Attendues Phases 4-6

| Métrique | Avant | Après | Amélioration |
|----------|-------|-------|--------------|
| Code-Behind Total | 2,560 | 195 | **-92%** |
| MainWindow.xaml.cs | 534 | 50 | -91% |
| CRUDGeneratorUC.xaml.cs | 706 | 30 | -96% |
| OptionGeneratorUC.xaml.cs | 488 | 30 | -94% |
| DtoGeneratorUC.xaml.cs | 199 | 25 | -87% |
| ModifyProjectUC.xaml.cs | 400 | 30 | -92% |
| Méthodes Inject() | 5 | 0 | -100% |
| Event Handlers | 16+ | 0 | -100% |
| Commands MVVM | 0 | 30+ | +∞ |

---

## 🎬 Getting Started

### Jour 1 (Lundi)

1. [ ] Review ce document (équipe)
2. [ ] Training SOLID (2h)
3. [ ] Training MVVM (2h)
4. [ ] Setup infrastructure (git, CI/CD)

### Jour 2-3 (Mardi-Mercredi)

1. [ ] Implémenter Phase 1 (Services)
2. [ ] Unit tests Phase 1
3. [ ] Code review Phase 1

### Jour 4-5 (Jeudi-Vendredi)

1. [ ] Implémenter Phase 2 (MainWindow)
2. [ ] Unit tests Phase 2
3. [ ] Integration tests Phase 2

---

## ✅ Sign-Off

### Approbation Requise

- [ ] Product Owner
- [ ] Tech Lead
- [ ] Architect
- [ ] QA Lead

### Notes d'Approbation


```
PO: ________________ Date: _________
Tech Lead: _________ Date: _________
Architect: ________ Date: _________
QA Lead: _________ Date: _________
```

---

## 📞 Contact & Questions

| Question | Réponse |
|----------|---------|
| Quand commençons-nous? | Après approbation du plan |
| Combien ça coûte? | ~57 heures (~7-8 jours travail) |
| Quels risques? | Voir section "Risques et Mitigations" |
| Comment gérez les bugs? | Hotfixes sur main, cherry-pick |
| Quand livrons-nous? | Fin semaine 4 (production-ready) |

---

*Document de suivi généré le 19 janvier 2026*
*Version: 1.0 - État: En attente d'approbation*
