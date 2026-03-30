# Plan d'Implémentation - Modernisation MVVM

## Overview

Migration complète de l'infrastructure MVVM de BIA.ToolKit depuis MicroMvvm custom vers CommunityToolkit.Mvvm sur 2-3 semaines. MicroMvvm sera complètement retiré (pas de cohabitation), avec validation à chaque étape pour garantir zéro régression.

**Scope:**
- 11 ViewModels à migrer
- 22 propriétés de correction à valider
- ~150 propriétés et ~30 commandes à moderniser
- Réduction attendue: 60-70% de boilerplate
- Retrait complet de MicroMvvm

**Approach:**
- Phase 0 (1 jour): Setup projet de tests
- Phase 1 (3-5 jours): Installation CommunityToolkit + Pilot
- Phase 2 (1-2 semaines): Migration des 11 ViewModels + Tests
- Phase 3 (2-3 jours): Finalisation + Métriques + Retrait MicroMvvm

## Tasks

### Phase 0: Setup Infrastructure de Tests (1 jour)

- [ ] 0. Créer projet de tests et installer dépendances
  - [ ] 0.1 Créer projet BIA.ToolKit.Tests
    - Créer BIA.ToolKit.Tests/BIA.ToolKit.Tests.csproj (net9.0-windows)
    - Ajouter référence au projet BIA.ToolKit
    - Créer structure de dossiers: Unit/, Properties/, Integration/, Performance/
    - _Requirements: 9.1_
  
  - [ ] 0.2 Installer packages de test
    - Ajouter xUnit 2.9.0
    - Ajouter xUnit.runner.visualstudio 2.8.2
    - Ajouter Moq 4.20.0
    - Ajouter FluentAssertions 6.12.0
    - Ajouter FsCheck 2.16.6
    - Ajouter FsCheck.Xunit 2.16.6
    - Ajouter Coverlet.collector 6.0.0
    - Vérifier que le projet compile
    - _Requirements: 9.1, 9.5_
  
  - [ ] 0.3 Créer classe de test exemple
    - Créer BIA.ToolKit.Tests/Unit/SampleTest.cs
    - Écrire un test simple pour vérifier que xUnit fonctionne
    - Écrire un property test simple pour vérifier que FsCheck fonctionne
    - Exécuter les tests et vérifier qu'ils passent

### Phase 1: Installation CommunityToolkit et Pilot (3-5 jours)

- [ ] 1. Installer CommunityToolkit.Mvvm et créer ViewModel pilote
  - [ ] 1.1 Installer CommunityToolkit.Mvvm
    - Ajouter PackageReference CommunityToolkit.Mvvm version 8.3.2+ dans BIA.ToolKit.csproj
    - Vérifier que source generators sont activés
    - Compiler le projet et vérifier absence de conflits
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  
  - [ ] 1.2 Créer LogDetailViewModel avec CommunityToolkit (Pilot)
    - Créer fichier BIA.ToolKit/ViewModels/LogDetailViewModel.cs
    - Hériter de CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    - Convertir propriétés avec [ObservableProperty]
    - Convertir commandes avec [RelayCommand]
    - Marquer classe comme partial
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.6_
  
  - [ ] 1.3 Connecter LogDetailViewModel à la vue
    - Modifier LogDetailUC.xaml.cs pour instancier LogDetailViewModel
    - Configurer DataContext
    - Vérifier bindings XAML fonctionnent
    - Tester manuellement l'affichage et les commandes
    - _Requirements: 5.5_
  
  - [ ]* 1.4 Écrire tests pour LogDetailViewModel
    - Unit tests: notifications de propriétés, exécution des commandes
    - Property test: Migration Pilot Equivalence (Property 10)
    - _Requirements: 9.2, 9.3, 5.5_

- [ ] 2. Checkpoint Phase 1
  - Compiler le projet sans erreurs ni warnings
  - Tester manuellement LogDetailUC (affichage, commandes)
  - Vérifier que CommunityToolkit fonctionne correctement
  - Demander validation utilisateur avant Phase 2


### Phase 2: Migration des 11 ViewModels (1-2 semaines)

- [ ] 3. Migrer RepositoryResumeUC
  - [ ] 3.1 Créer RepositoryResumeViewModel
    - Créer BIA.ToolKit/ViewModels/RepositoryResumeViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~8 propriétés avec [ObservableProperty]
    - Migrer ~1 commande avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 3.2 Extraire logique du code-behind
    - Identifier Business_Logic dans RepositoryResumeUC.xaml.cs
    - Déplacer vers RepositoryResumeViewModel
    - Garder seulement UI_Logic dans code-behind
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [ ] 3.3 Connecter ViewModel à la vue
    - Modifier RepositoryResumeUC.xaml.cs pour utiliser RepositoryResumeViewModel
    - Configurer DataContext
    - _Requirements: 7.5_
  
  - [ ]* 3.4 Écrire tests pour RepositoryResumeViewModel
    - Unit tests: propriétés, commandes
    - Property tests: API compatibility, View compatibility, Extracted logic
    - _Requirements: 9.2, 9.3, 9.5_

- [ ] 4. Migrer VersionAndOptionUserControl
  - [ ] 4.1 Créer VersionAndOptionViewModel
    - Créer BIA.ToolKit/ViewModels/VersionAndOptionViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~6 propriétés avec [ObservableProperty]
    - Migrer ~3 commandes avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 4.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier VersionAndOptionUserControl.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_
  
  - [ ]* 4.3 Écrire tests pour VersionAndOptionViewModel
    - Unit tests et property tests
    - _Requirements: 9.2, 9.3_

- [ ] 5. Migrer RepositoryFormUC (avec validation)
  - [ ] 5.1 Créer RepositoryFormViewModel avec validation
    - Créer BIA.ToolKit/ViewModels/RepositoryFormViewModel.cs
    - Hériter de CommunityToolkit ObservableValidator
    - Migrer ~12 propriétés avec [ObservableProperty] et attributs de validation
    - Ajouter [Required], [MinLength], [Url] selon besoins
    - Migrer ~4 commandes avec [RelayCommand]
    - Implémenter validation automatique avec partial methods
    - _Requirements: 7.1, 7.2, 7.3, 11.1, 11.2, 11.6_
  
  - [ ] 5.2 Extraire logique et connecter à la vue avec bindings validation
    - Déplacer Business_Logic vers ViewModel
    - Modifier RepositoryFormUC.xaml.cs
    - Ajouter bindings XAML pour Errors et HasErrors
    - _Requirements: 8.1, 8.2, 7.5, 11.5_
  
  - [ ]* 5.3 Écrire tests pour RepositoryFormViewModel
    - Unit tests: propriétés, commandes, validation
    - Property tests: Validation (Properties 15-18)
    - _Requirements: 9.2, 9.3, 9.7_

- [ ] 6. Migrer CustomTemplateRepositorySettingsUC
  - [ ] 6.1 Créer CustomTemplateRepositorySettingsViewModel
    - Créer BIA.ToolKit/ViewModels/CustomTemplateRepositorySettingsViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~10 propriétés avec [ObservableProperty]
    - Migrer ~3 commandes avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 6.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier CustomTemplateRepositorySettingsUC.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_
  
  - [ ]* 6.3 Écrire tests
    - Unit tests et property tests
    - _Requirements: 9.2, 9.3_

- [ ] 7. Migrer OptionGeneratorUC
  - [ ] 7.1 Créer OptionGeneratorViewModel
    - Créer BIA.ToolKit/ViewModels/OptionGeneratorViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~15 propriétés avec [ObservableProperty]
    - Migrer ~5 commandes avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 7.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier OptionGeneratorUC.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_
  
  - [ ]* 7.3 Écrire tests
    - Unit tests et property tests
    - _Requirements: 9.2, 9.3_

- [ ] 8. Checkpoint Phase 2.1
  - Compiler le projet sans erreurs
  - Exécuter tous les tests unitaires
  - Tester manuellement les 5 ViewModels migrés
  - Vérifier absence de régressions
  - Demander validation utilisateur avant de continuer

- [ ] 9. Implémenter système Messenger
  - [ ] 9.1 Créer classes de messages
    - Créer BIA.ToolKit/Messages/SettingsUpdatedMessage.cs
    - Créer BIA.ToolKit/Messages/RepositoryChangedMessage.cs
    - Créer BIA.ToolKit/Messages/RepositoryDeletedMessage.cs
    - Créer BIA.ToolKit/Messages/RepositoryAddedMessage.cs
    - Créer BIA.ToolKit/Messages/OpenRepositoryFormMessage.cs
    - Utiliser records pour immutabilité
    - _Requirements: 10.5_
  
  - [ ] 9.2 Remplacer UIEventBroker par Messenger
    - Identifier tous les usages de UIEventBroker
    - Remplacer par WeakReferenceMessenger.Default.Send()
    - Implémenter IRecipient<TMessage> dans ViewModels récepteurs
    - _Requirements: 10.1, 10.2, 10.4_
  
  - [ ]* 9.3 Écrire tests pour Messenger
    - Property tests: Message Delivery, Weak Reference Cleanup (Properties 19-20)
    - Integration tests: communication entre ViewModels
    - _Requirements: 10.2, 10.3, 10.6_

- [ ] 10. Migrer CustomTemplatesRepositoriesSettingsUC
  - [ ] 10.1 Créer CustomTemplatesRepositoriesSettingsViewModel
    - Créer BIA.ToolKit/ViewModels/CustomTemplatesRepositoriesSettingsViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~8 propriétés avec [ObservableProperty]
    - Migrer ~6 commandes avec [RelayCommand]
    - Intégrer Messenger pour communication
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 10.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier CustomTemplatesRepositoriesSettingsUC.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_
  
  - [ ]* 10.3 Écrire tests
    - Unit tests: propriétés, commandes, Messenger
    - _Requirements: 9.2, 9.3_

- [ ] 11. Migrer ModifyProjectUC
  - [ ] 11.1 Créer ModifyProjectViewModel
    - Créer BIA.ToolKit/ViewModels/ModifyProjectViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~18 propriétés avec [ObservableProperty]
    - Migrer ~7 commandes avec [RelayCommand]
    - Intégrer Messenger si nécessaire
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 11.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier ModifyProjectUC.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_
  
  - [ ]* 11.3 Écrire tests
    - Unit tests et property tests
    - _Requirements: 9.2, 9.3_

- [ ] 12. Checkpoint Phase 2.2
  - Compiler le projet sans erreurs
  - Exécuter tous les tests unitaires
  - Tester manuellement les 8 ViewModels migrés
  - Vérifier Messenger fonctionne correctement
  - Demander validation utilisateur avant de continuer

- [ ] 13. Migrer DtoGeneratorUC (Complexité: Haute)
  - [ ] 13.1 Créer DtoGeneratorViewModel
    - Créer BIA.ToolKit/ViewModels/DtoGeneratorViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~25 propriétés avec [ObservableProperty]
    - Migrer ~8 commandes avec [RelayCommand]
    - Gérer dépendances CSharpParser et Roslyn
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 13.2 Extraire logique complexe du code-behind
    - Identifier toute la Business_Logic dans DtoGeneratorUC.xaml.cs
    - Déplacer vers ViewModel (génération, parsing, validation)
    - Réduire code-behind de 70%+
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [ ] 13.3 Connecter ViewModel à la vue
    - Modifier DtoGeneratorUC.xaml.cs
    - Vérifier tous les bindings fonctionnent
    - _Requirements: 7.5_
  
  - [ ]* 13.4 Écrire tests pour DtoGeneratorViewModel
    - Unit tests: propriétés, commandes, logique de génération
    - Property tests: Extracted logic (Properties 13-14)
    - _Requirements: 9.2, 9.3, 9.5, 8.4, 8.6_

- [ ] 14. Migrer CRUDGeneratorUC (Complexité: Haute)
  - [ ] 14.1 Créer CRUDGeneratorViewModel
    - Créer BIA.ToolKit/ViewModels/CRUDGeneratorViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~30 propriétés avec [ObservableProperty]
    - Migrer ~10 commandes avec [RelayCommand]
    - Gérer dépendances CSharpParser et Roslyn
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 14.2 Extraire logique complexe du code-behind
    - Identifier toute la Business_Logic dans CRUDGeneratorUC.xaml.cs
    - Déplacer vers ViewModel (génération CRUD, parsing, orchestration)
    - Réduire code-behind de 70%+
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [ ] 14.3 Connecter ViewModel à la vue
    - Modifier CRUDGeneratorUC.xaml.cs
    - Vérifier tous les bindings fonctionnent
    - _Requirements: 7.5_
  
  - [ ]* 14.4 Écrire tests pour CRUDGeneratorViewModel
    - Unit tests: propriétés, commandes, logique de génération CRUD
    - Property tests: Extracted logic (Properties 13-14)
    - _Requirements: 9.2, 9.3, 9.5, 8.4, 8.6_

- [ ] 15. Migrer MainWindow (Complexité: Haute, orchestration globale)
  - [ ] 15.1 Créer MainWindowViewModel
    - Créer BIA.ToolKit/ViewModels/MainWindowViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~20 propriétés avec [ObservableProperty]
    - Migrer ~12 commandes avec [RelayCommand]
    - Intégrer Messenger pour orchestration globale
    - Implémenter IRecipient pour messages nécessaires
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 15.2 Extraire logique d'orchestration du code-behind
    - Identifier toute la Business_Logic dans MainWindow.xaml.cs
    - Déplacer vers MainWindowViewModel
    - Garder seulement UI_Logic (focus, animations)
    - Réduire code-behind de 70%+
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [ ] 15.3 Connecter ViewModel à la vue
    - Modifier MainWindow.xaml.cs
    - Vérifier tous les bindings fonctionnent
    - Tester navigation entre UserControls
    - _Requirements: 7.5_
  
  - [ ]* 15.4 Écrire tests pour MainWindowViewModel
    - Unit tests: propriétés, commandes, orchestration
    - Integration tests: communication avec autres ViewModels via Messenger
    - Property tests: API compatibility, View compatibility
    - _Requirements: 9.2, 9.3, 9.5_

- [ ] 16. Checkpoint Phase 2 - Migration complète
  - Compiler le projet sans erreurs ni warnings
  - Exécuter tous les tests unitaires (100% doivent passer)
  - Exécuter tous les property tests (100% doivent passer)
  - Tester manuellement tous les ViewModels migrés
  - Vérifier que tous les 11 ViewModels utilisent CommunityToolkit
  - Demander validation utilisateur avant Phase 3

### Phase 3: Finalisation et Retrait MicroMvvm (2-3 jours)

- [ ] 17. Retirer MicroMvvm complètement
  - [ ] 17.1 Supprimer les fichiers MicroMvvm
    - Supprimer BIA.ToolKit/Helper/MicroMvvm/ObservableObject.cs
    - Supprimer BIA.ToolKit/Helper/MicroMvvm/RelayCommand.cs
    - Supprimer le dossier BIA.ToolKit/Helper/MicroMvvm/
    - _Requirements: 7.7_
  
  - [ ] 17.2 Nettoyer les références MicroMvvm
    - Rechercher tous les usings MicroMvvm dans le code
    - Vérifier qu'aucun fichier ne référence MicroMvvm
    - Compiler le projet et vérifier absence d'erreurs
    - _Requirements: 7.7_

- [ ] 18. Établir baseline de performance
  - [ ] 18.1 Créer infrastructure de mesure de performance
    - Créer BIA.ToolKit.Tests/Performance/BaselineTests.cs
    - Implémenter mesure du temps de démarrage
    - Implémenter mesure de la mémoire
    - Implémenter mesure du temps d'exécution des commandes
    - Implémenter mesure du temps de build
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_
  
  - [ ] 18.2 Capturer et valider baseline post-migration
    - Exécuter tests de performance 3 fois
    - Calculer moyennes et écarts-types
    - Documenter specs hardware et version .NET
    - Sauvegarder baseline dans fichier JSON
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

- [ ] 19. Exécuter suite de tests de régression complète
  - [ ]* 19.1 Écrire property test pour Migration Behavioral Invariant
    - **Property 21: Migration Behavioral Invariant**
    - **Validates: Requirements 14.1, 14.4**
  
  - [ ]* 19.2 Écrire property test pour Data Binding Correctness
    - **Property 22: Data Binding Correctness**
    - **Validates: Requirements 14.5**
  
  - [ ] 19.3 Exécuter tests de régression manuels
    - Tester tous les workflows utilisateur principaux
    - Vérifier génération CRUD fonctionne
    - Vérifier génération DTO fonctionne
    - Vérifier modification de projet fonctionne
    - Vérifier gestion des repositories fonctionne
    - _Requirements: 14.1, 14.3, 14.4_

- [ ] 20. Créer documentation
  - [ ] 20.1 Rédiger guide de migration
    - Créer BIA.ToolKit/Documentation/MVVM-Migration-Guide.md
    - Documenter processus étape par étape
    - Inclure exemples avant/après
    - Documenter pièges courants et solutions
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [ ] 20.2 Rédiger documentation d'architecture
    - Créer BIA.ToolKit/Documentation/MVVM-Architecture.md
    - Décrire architecture CommunityToolkit.Mvvm
    - Inclure diagrammes
    - Documenter patterns et exemples
    - _Requirements: 12.1, 12.2, 12.3, 12.5, 12.6_

- [ ] 21. Collecter métriques de réduction du boilerplate
  - [ ] 21.1 Mesurer réduction de code par ViewModel
    - Compter lignes de code avant/après pour chaque ViewModel
    - Calculer % réduction pour propriétés (objectif: 60%+)
    - Calculer % réduction pour commandes (objectif: 50%+)
    - Calculer % réduction pour code-behind (objectif: 70%+)
    - _Requirements: 13.1, 13.2, 13.3, 13.4_
  
  - [ ] 21.2 Créer rapport de migration
    - Créer BIA.ToolKit/Documentation/Migration-Report.md
    - Inclure métriques de réduction de code
    - Inclure métriques de performance
    - Inclure résumé des tests
    - Inclure timeline de migration
    - _Requirements: 13.5_

- [ ] 22. Checkpoint Final - Validation complète
  - Compiler le projet sans erreurs ni warnings
  - Exécuter tous les tests (unit, property, integration, performance)
  - Vérifier couverture de code ≥80%
  - Vérifier tous les seuils de performance respectés
  - Tester manuellement l'application complète
  - Valider avec l'équipe que la migration est complète
  - MicroMvvm complètement retiré

## Notes

- Les tâches marquées avec `*` sont optionnelles et peuvent être sautées pour un MVP plus rapide
- Chaque tâche référence les requirements spécifiques pour traçabilité
- Les checkpoints assurent validation incrémentale et permettent rollback si nécessaire
- Les property tests valident les propriétés de correction universelles
- Les unit tests valident des exemples spécifiques et cas limites
- La migration est progressive: chaque ViewModel est indépendant
- En cas de régression détectée, suivre la procédure de rollback documentée dans design.md
- MicroMvvm sera complètement retiré à la fin de la Phase 3
