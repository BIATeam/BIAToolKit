# Requirements Document - Modernisation MVVM

## Introduction

Ce document définit les exigences pour la modernisation de l'architecture MVVM de BIA.ToolKit, une application WPF .NET 9.0. L'objectif est de réduire le code boilerplate, améliorer la testabilité et adopter les standards modernes Microsoft tout en maintenant la compatibilité avec le code existant durant une migration progressive en 3 phases.

## Migration Phases

### Phase 1: Fondations et Préparation
- Amélioration de MicroMvvm avec SetProperty (Req 1)
- Implémentation du cache des commandes (Req 2)
- Support des opérations asynchrones (Req 3)
- Installation de CommunityToolkit.Mvvm (Req 4)
- Création du ViewModel pilote (Req 5)
- Rédaction du guide de migration (Req 6)

### Phase 2: Migration et Tests
- Migration progressive des 11 ViewModels (Req 7)
- Extraction de la logique des code-behind (Req 8)
- Création des tests unitaires (Req 9)
- Adoption du Messenger pattern (Req 10)
- Implémentation de la validation (Req 11)

### Phase 3: Finalisation et Documentation
- Documentation complète (Req 12)
- Collecte des métriques (Req 13)
- Validation de la compatibilité (Req 14)
- Validation des performances (Req 15)
- Dépréciation de MicroMvvm custom

## Glossary

- **MicroMvvm**: Implémentation custom légère du pattern MVVM actuellement utilisée dans BIA.ToolKit
- **CommunityToolkit.Mvvm**: Bibliothèque MVVM officielle Microsoft utilisant des source generators (successeur de MVVM Light)
- **ObservableObject**: Classe de base implémentant INotifyPropertyChanged pour la notification de changement de propriétés
- **RelayCommand**: Implémentation de ICommand permettant de lier des méthodes aux commandes XAML
- **AsyncRelayCommand**: Commande supportant les opérations asynchrones avec gestion de l'état d'exécution
- **SetProperty**: Méthode helper réduisant le boilerplate pour les propriétés notifiables
- **Source_Generator**: Outil de génération de code à la compilation (Roslyn) utilisé par CommunityToolkit.Mvvm
- **Code_Behind**: Fichier .xaml.cs contenant la logique associée à une vue XAML
- **ViewModel**: Classe intermédiaire entre la vue (XAML) et le modèle de données dans le pattern MVVM
- **Command_Cache**: Mécanisme de stockage des instances de commandes pour éviter leur recréation
- **Messenger**: Pattern de communication découplée entre ViewModels via un bus de messages
- **Migration_Pilot**: Premier ViewModel migré servant de référence pour les migrations suivantes
- **Baseline**: État de référence de l'application avant migration, utilisé pour les comparaisons de performance
- **Regression**: Comportement fonctionnel différent de l'état avant migration
- **Business_Logic**: Logique métier manipulant des données ou orchestrant des opérations (à placer dans ViewModel)
- **UI_Logic**: Logique spécifique à l'interface (animations, focus, états visuels) pouvant rester dans code-behind

## ViewModels Inventory

L'application contient 11 ViewModels/UserControls à migrer:

### UserControls (6)
1. **CRUDGeneratorUC** - Génération de code CRUD (Complexité: Haute)
2. **DtoGeneratorUC** - Génération de DTOs (Complexité: Haute)
3. **ModifyProjectUC** - Modification de projet (Complexité: Moyenne)
4. **OptionGeneratorUC** - Génération d'options (Complexité: Moyenne)
5. **RepositoryResumeUC** - Résumé de repository (Complexité: Faible)
6. **VersionAndOptionUserControl** - Gestion version/options (Complexité: Faible)

### Dialogs (4)
7. **CustomTemplateRepositorySettingsUC** - Configuration repository template (Complexité: Moyenne)
8. **CustomTemplatesRepositoriesSettingsUC** - Configuration repositories (Complexité: Moyenne)
9. **LogDetailUC** - Détails de log (Complexité: Faible)
10. **RepositoryFormUC** - Formulaire repository (Complexité: Moyenne)

### Main Window (1)
11. **MainWindow** - Fenêtre principale (Complexité: Haute)

### Migration Order (Simple → Complex)
1. LogDetailUC (Faible, peu de dépendances)
2. RepositoryResumeUC (Faible, affichage simple)
3. VersionAndOptionUserControl (Faible, formulaire basique)
4. RepositoryFormUC (Moyenne, formulaire avec validation)
5. CustomTemplateRepositorySettingsUC (Moyenne, configuration)
6. OptionGeneratorUC (Moyenne, génération simple)
7. CustomTemplatesRepositoriesSettingsUC (Moyenne, liste + configuration)
8. ModifyProjectUC (Moyenne, orchestration)
9. DtoGeneratorUC (Haute, génération complexe)
10. CRUDGeneratorUC (Haute, génération complexe)
11. MainWindow (Haute, orchestration globale)

## Requirements

### Requirement 1: Amélioration de MicroMvvm - Méthode SetProperty

**Dependencies:** None

**Phase:** 1 - Fondations et Préparation

**User Story:** En tant que développeur, je veux une méthode SetProperty dans ObservableObject, afin de réduire le code boilerplate des propriétés notifiables de 60-70%.

#### Acceptance Criteria

1. THE ObservableObject SHALL provide a protected SetProperty method accepting a ref field, new value, and optional property name
2. WHEN SetProperty is called with equal values, THE ObservableObject SHALL return false without raising PropertyChanged
3. WHEN SetProperty is called with different values, THE ObservableObject SHALL update the field, raise PropertyChanged, and return true
4. THE SetProperty method SHALL use CallerMemberName attribute to automatically capture the property name
5. THE SetProperty method SHALL use EqualityComparer<T>.Default for value comparison
6. FOR ALL existing properties using manual RaisePropertyChanged, refactoring to SetProperty SHALL produce equivalent notification behavior (metamorphic property)

### Requirement 2: Cache des Commandes

**Dependencies:** None

**Phase:** 1 - Fondations et Préparation

**User Story:** En tant que développeur, je veux que les commandes soient cachées, afin d'éviter la création d'instances multiples et améliorer les performances.

#### Acceptance Criteria

1. WHEN a command property is accessed multiple times, THE ViewModel SHALL return the same command instance (idempotence property)
2. THE ViewModel SHALL use lazy initialization pattern with null-coalescing operator for command caching
3. THE cached command instance SHALL remain valid for the lifetime of the ViewModel
4. WHEN a ViewModel is disposed, THE Command_Cache SHALL be eligible for garbage collection

### Requirement 3: Support des Opérations Asynchrones

**Dependencies:** Req 2 (Cache des Commandes)

**Phase:** 1 - Fondations et Préparation

**User Story:** En tant que développeur, je veux une AsyncRelayCommand, afin de gérer proprement les opérations asynchrones dans les commandes.

#### Acceptance Criteria

1. THE AsyncRelayCommand SHALL implement ICommand interface
2. WHEN an async command is executing, THE AsyncRelayCommand SHALL set CanExecute to false
3. WHEN an async command completes, THE AsyncRelayCommand SHALL restore CanExecute to its original state
4. IF an exception occurs during async execution, THEN THE AsyncRelayCommand SHALL capture and expose the exception
5. THE AsyncRelayCommand SHALL provide an IsExecuting property for UI binding
6. THE AsyncRelayCommand SHALL support cancellation via CancellationToken
7. WHEN Execute is called while already executing, THE AsyncRelayCommand SHALL ignore the duplicate call

### Requirement 4: Installation de CommunityToolkit.Mvvm

**Dependencies:** Req 1, 2, 3 (MicroMvvm amélioré)

**Phase:** 1 - Fondations et Préparation

**User Story:** En tant que développeur, je veux installer CommunityToolkit.Mvvm en parallèle de MicroMvvm, afin de permettre une migration progressive sans breaking changes.

#### Acceptance Criteria

1. THE project SHALL reference CommunityToolkit.Mvvm NuGet package version 8.3.2 or higher
2. THE MicroMvvm classes SHALL remain available and functional after CommunityToolkit installation
3. THE project SHALL compile without conflicts between MicroMvvm and CommunityToolkit.Mvvm
4. THE Source_Generator from CommunityToolkit SHALL be enabled and functional

### Requirement 5: ViewModel Pilote avec CommunityToolkit

**Dependencies:** Req 4 (Installation CommunityToolkit)

**Phase:** 1 - Fondations et Préparation

**User Story:** En tant que développeur, je veux créer un ViewModel pilote utilisant CommunityToolkit.Mvvm, afin de valider l'approche avant la migration complète.

#### Pilot Selection

THE Migration_Pilot SHALL be **LogDetailUC** because:
- Faible complexité (affichage de logs)
- Peu de dépendances avec autres ViewModels
- Contient des propriétés simples et au moins une commande
- Permet de valider le workflow complet sans risque

#### Acceptance Criteria

1. THE Migration_Pilot SHALL use CommunityToolkit.Mvvm.ComponentModel.ObservableObject as base class
2. THE Migration_Pilot SHALL demonstrate [ObservableProperty] attribute usage for at least 3 properties
3. THE Migration_Pilot SHALL demonstrate [RelayCommand] attribute usage for at least 2 commands
4. THE Migration_Pilot SHALL demonstrate async command with [RelayCommand] for at least 1 async operation
5. THE Migration_Pilot SHALL maintain functional equivalence with its MicroMvvm counterpart
6. THE Migration_Pilot SHALL compile without errors and generate expected properties and commands

### Requirement 6: Guide de Migration

**Dependencies:** Req 5 (ViewModel Pilote)

**Phase:** 1 - Fondations et Préparation

**User Story:** En tant que développeur, je veux un guide de migration documenté, afin de migrer systématiquement les ViewModels existants vers CommunityToolkit.

#### Acceptance Criteria

1. THE migration guide SHALL document the step-by-step process for converting MicroMvvm ViewModels to CommunityToolkit
2. THE migration guide SHALL provide before/after code examples for properties, commands, and async commands
3. THE migration guide SHALL document common pitfalls and their solutions
4. THE migration guide SHALL include a checklist for verifying migration completeness
5. THE migration guide SHALL specify the order of ViewModel migration based on dependencies

### Requirement 7: Migration Progressive des ViewModels

**Dependencies:** Req 6 (Guide de Migration)

**Phase:** 2 - Migration et Tests

**User Story:** En tant que développeur, je veux migrer progressivement les 11 ViewModels existants, afin de minimiser les risques et valider chaque étape.

#### Acceptance Criteria

1. WHEN a ViewModel is migrated, THE ViewModel SHALL inherit from CommunityToolkit.Mvvm.ComponentModel.ObservableObject
2. WHEN a ViewModel is migrated, THE ViewModel SHALL use [ObservableProperty] for all notifiable properties
3. WHEN a ViewModel is migrated, THE ViewModel SHALL use [RelayCommand] or [RelayCommand(CanExecute=...)] for all commands
4. WHEN a ViewModel is migrated, THE ViewModel SHALL maintain identical public API (properties and commands)
5. FOR ALL migrated ViewModels, the associated views SHALL function identically without XAML changes (invariant property)
6. THE migration SHALL follow the order defined in "ViewModels Inventory" section (LogDetailUC → MainWindow)
7. WHEN all 11 ViewModels are migrated, THE MicroMvvm classes SHALL be marked as obsolete

### Requirement 8: Extraction de la Logique des Code-Behind

**Dependencies:** Req 7 (Migration ViewModels)

**Phase:** 2 - Migration et Tests

**User Story:** En tant que développeur, je veux extraire la logique métier des fichiers .xaml.cs vers les ViewModels, afin d'améliorer la testabilité et respecter le pattern MVVM.

#### Logic Classification

**Business_Logic (à extraire vers ViewModel):**
- Manipulation de données (calculs, transformations, validations)
- Appels à des services ou repositories
- Orchestration de workflows métier
- Gestion d'état métier

**UI_Logic (peut rester dans code-behind):**
- Gestion du focus clavier
- Animations et transitions visuelles
- Manipulation directe du VisualTree
- Gestion des états visuels (VisualStateManager)
- Drag & drop UI (positions, curseurs)

#### Acceptance Criteria

1. WHEN business logic is identified in Code_Behind, THE logic SHALL be moved to the corresponding ViewModel
2. THE Code_Behind SHALL only contain UI_Logic as defined above
3. THE Code_Behind file size SHALL be reduced by at least 70% for files exceeding 200 lines
4. THE extracted logic SHALL be testable via unit tests without UI dependencies
5. WHEN logic is extracted, THE ViewModel SHALL expose commands or properties for view binding
6. THE extracted logic SHALL maintain identical functional behavior (metamorphic property)

### Requirement 9: Tests Unitaires des ViewModels

**Dependencies:** Req 7 (Migration ViewModels), Req 8 (Extraction logique)

**Phase:** 2 - Migration et Tests

**User Story:** En tant que développeur, je veux des tests unitaires pour les ViewModels, afin de garantir la qualité et faciliter les refactorings futurs.

#### Acceptance Criteria

1. THE project SHALL include a test project for ViewModel unit tests
2. WHEN a ViewModel is migrated, THE ViewModel SHALL have unit tests covering property notifications
3. WHEN a ViewModel is migrated, THE ViewModel SHALL have unit tests covering command execution
4. WHEN a ViewModel is migrated, THE ViewModel SHALL have unit tests covering async command execution and cancellation
5. THE unit tests SHALL not depend on WPF UI components
6. THE unit tests SHALL achieve at least 80% code coverage for ViewModel logic
7. FOR ALL properties with validation, tests SHALL verify error states (error condition property)

### Requirement 10: Adoption du Messenger Pattern

**Dependencies:** Req 7 (Migration ViewModels)

**Phase:** 2 - Migration et Tests

**User Story:** En tant que développeur, je veux utiliser le Messenger de CommunityToolkit, afin de remplacer l'UIEventBroker custom et standardiser la communication entre ViewModels.

#### Acceptance Criteria

1. THE application SHALL use CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger for inter-ViewModel communication
2. WHEN a ViewModel sends a message, THE Messenger SHALL deliver it to all registered recipients
3. WHEN a ViewModel is disposed, THE Messenger SHALL automatically unregister weak references
4. THE custom UIEventBroker SHALL be replaced by Messenger-based messages
5. THE message classes SHALL be strongly-typed and inherit from appropriate message base classes
6. THE Messenger usage SHALL be documented with examples for common scenarios

### Requirement 11: Support de la Validation

**Dependencies:** Req 7 (Migration ViewModels)

**Phase:** 2 - Migration et Tests

**User Story:** En tant que développeur, je veux implémenter la validation avec CommunityToolkit, afin de valider les entrées utilisateur et afficher les erreurs dans l'UI.

#### Acceptance Criteria

1. THE ViewModels requiring validation SHALL inherit from ObservableValidator
2. THE validated properties SHALL use validation attributes ([Required], [Range], [EmailAddress], etc.)
3. WHEN a property value is invalid, THE ViewModel SHALL populate the Errors collection
4. WHEN a property value becomes valid, THE ViewModel SHALL clear errors for that property
5. THE XAML views SHALL bind to HasErrors property for visual feedback
6. THE validation SHALL execute automatically when properties change
7. FOR ALL validated properties, setting an invalid value then valid value SHALL result in no errors (round-trip property)

### Requirement 12: Documentation et Formation

**Dependencies:** Req 7, 8, 9, 10, 11 (Toutes les migrations)

**Phase:** 3 - Finalisation et Documentation

**User Story:** En tant que développeur, je veux une documentation complète sur la nouvelle architecture MVVM, afin de faciliter l'onboarding et la maintenance future.

#### Acceptance Criteria

1. THE project SHALL include architecture documentation describing the MVVM implementation
2. THE documentation SHALL include code examples for common patterns (properties, commands, validation, messaging)
3. THE documentation SHALL explain when to use each CommunityToolkit feature
4. THE documentation SHALL document the migration history and lessons learned
5. THE documentation SHALL be available in French (primary language of the team)
6. THE documentation SHALL include diagrams showing ViewModel relationships and message flows

### Requirement 13: Métriques de Réduction du Boilerplate

**Dependencies:** Req 7, 8 (Migration et extraction)

**Phase:** 3 - Finalisation et Documentation

**User Story:** En tant que tech lead, je veux mesurer la réduction du code boilerplate, afin de quantifier les bénéfices de la modernisation.

#### Acceptance Criteria

1. THE project SHALL track lines of code before and after migration for each ViewModel
2. THE migration SHALL achieve at least 60% reduction in property boilerplate code
3. THE migration SHALL achieve at least 50% reduction in command boilerplate code
4. THE migration SHALL reduce Code_Behind lines by at least 70% for files exceeding 200 lines
5. THE metrics SHALL be documented in a migration report

### Requirement 14: Compatibilité et Non-Régression

**Dependencies:** Req 7 (Migration ViewModels)

**Phase:** 3 - Finalisation et Documentation

**User Story:** En tant que utilisateur, je veux que l'application fonctionne identiquement après la migration, afin de ne pas perdre de fonctionnalités.

#### Regression Detection Criteria

A regression is detected WHEN:
- A command that previously executed now throws an exception
- A property binding that previously worked now shows no data
- A user interaction that previously triggered an action now does nothing
- A validation that previously worked now fails incorrectly
- A navigation flow that previously worked now breaks

#### Rollback Procedure

WHEN a regression is detected:
1. THE developer SHALL document the regression with steps to reproduce
2. THE developer SHALL revert the ViewModel to its MicroMvvm version via git
3. THE developer SHALL analyze the root cause before attempting re-migration
4. IF the regression affects dependent ViewModels, THEN those SHALL also be rolled back
5. THE regression SHALL be documented in the migration guide as a known pitfall

#### Acceptance Criteria

1. FOR ALL existing features, the behavior SHALL remain identical after migration (invariant property)
2. THE application SHALL compile without warnings related to MVVM changes
3. THE application SHALL start and display all views correctly
4. WHEN user interactions are performed, THE commands SHALL execute as before migration
5. THE data binding SHALL work correctly for all migrated ViewModels
6. IF a regression is detected per criteria above, THEN THE rollback procedure SHALL be followed

### Requirement 15: Performance

**Dependencies:** Req 7 (Migration ViewModels)

**Phase:** 3 - Finalisation et Documentation

**User Story:** En tant que utilisateur, je veux que l'application reste performante après la migration, afin de maintenir une expérience utilisateur fluide.

#### Performance Measurement

**Baseline Establishment:**
1. THE baseline SHALL be captured BEFORE any migration begins
2. THE baseline SHALL include measurements from at least 3 application runs
3. THE baseline SHALL be documented with hardware specs and .NET version

**Measurement Tools:**
- Application startup time: Stopwatch from Main() to MainWindow.Loaded event
- Memory usage: Process.WorkingSet64 after application stabilization (30s idle)
- Command execution time: Stopwatch around command Execute() method
- Property notification: Stopwatch around PropertyChanged event raise
- Build time: MSBuild diagnostic output with /clp:PerformanceSummary

**Measurement Procedure:**
1. Close all other applications
2. Clear system cache (restart machine)
3. Run application 3 times and take average
4. Compare against baseline with tolerance thresholds

#### Acceptance Criteria

1. THE application startup time SHALL not increase by more than 5% compared to baseline
2. THE memory usage SHALL not increase by more than 10% compared to baseline
3. THE command execution time SHALL not increase by more than 5% compared to baseline
4. THE property change notification SHALL execute in less than 1ms per property
5. THE Source_Generator compilation time SHALL not add more than 2 seconds to total build time
6. IF any threshold is exceeded, THE performance issue SHALL be investigated and resolved before proceeding

