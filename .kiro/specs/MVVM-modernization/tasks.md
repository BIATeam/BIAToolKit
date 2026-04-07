# Plan d'Implémentation - Modernisation MVVM

## Overview

Migration complète de l'infrastructure MVVM de BIA.ToolKit depuis MicroMvvm custom vers CommunityToolkit.Mvvm sur 2-3 semaines. MicroMvvm sera complètement retiré (pas de cohabitation), avec validation à chaque étape pour garantir zéro régression.

**Scope:**
- 11 ViewModels à migrer
- 3 composants CRITIQUES (OptionGeneratorUC, DtoGeneratorUC code-behind + logique)
- 3 composants HAUT (CRUDGeneratorUC + DataContext statiques XAML)
- 3 composants MOYEN (ViewModels avec Inject() + MainViewModel IDisposable)
- ~150 propriétés et ~30 commandes à moderniser
- Réduction attendue: 60-70% de boilerplate
- Retrait complet de MicroMvvm

**Approach:**
- Phase 0 (1 jour): Setup projet de tests
- Phase 1 (3-5 jours): Installation CommunityToolkit + Pilot
- Phase 2 (1-2 semaines): Migration par ordre de sévérité (CRITIQUE → HAUT → MOYEN)
- Phase 3 (2-3 jours): Finalisation + Métriques + Retrait MicroMvvm

**Migration Order (par sévérité MVVM-guidelines.md section 9.1):**
1. CRITIQUE: OptionGeneratorUC (546 LOC code-behind, logique métier complète)
2. CRITIQUE: DtoGeneratorUC (TextChanged/SelectionChanged handlers)
3. HAUT: CRUDGeneratorUC (proxy delegates)
4. HAUT: Supprimer DataContext statiques XAML (3 fichiers)
5. MOYEN: CRUDGeneratorViewModel (Inject() → DI)
6. MOYEN: DtoGeneratorViewModel (Inject() → DI)
7. MOYEN: MainViewModel (IDisposable manquant)
8. Autres ViewModels simples (RepositoryResumeUC, RepositoryFormUC, etc.)
9. MainWindow (orchestration globale)

## Tasks

### Phase 0: Setup (SKIPPED - Focus sur migration MVVM uniquement)

### Phase 1: Installation CommunityToolkit et Pilot (3-5 jours)

- [ ] 1. Installer CommunityToolkit.Mvvm et créer ViewModel pilote
  - [x] 1.1 Installer CommunityToolkit.Mvvm
    - Ajouter PackageReference CommunityToolkit.Mvvm version 8.3.2+ dans BIA.ToolKit.csproj
    - Vérifier que source generators sont activés
    - Compiler le projet et vérifier absence de conflits
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  
  - [x] 1.2 Créer LogDetailViewModel avec CommunityToolkit (Pilot)
    - Créer fichier BIA.ToolKit/ViewModels/LogDetailViewModel.cs
    - Hériter de CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    - Convertir propriétés avec [ObservableProperty]
    - Convertir commandes avec [RelayCommand]
    - Marquer classe comme partial
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.6_
  
  - [x] 1.3 Connecter LogDetailViewModel à la vue
    - Modifier LogDetailUC.xaml.cs pour instancier LogDetailViewModel
    - Configurer DataContext
    - Vérifier bindings XAML fonctionnent
    - Tester manuellement l'affichage et les commandes
    - _Requirements: 5.5_
### Phase 2: Migration par Ordre de Sévérité (1-2 semaines)

**Ordre de migration basé sur MVVM-guidelines.md section 9:**
1. CRITIQUE-1: OptionGeneratorUC (section 9.2)
2. CRITIQUE-2: DtoGeneratorUC (section 9.3)
3. HAUT-1: CRUDGeneratorUC (section 9.4)
4. HAUT-2: DataContext statiques XAML (section 9.5)
5. MOYEN-1: CRUDGeneratorViewModel Inject() (section 9.6)
6. MOYEN-2: DtoGeneratorViewModel Inject() (section 9.7)
7. MOYEN-3: MainViewModel IDisposable (section 9.8)
8. Autres ViewModels simples
9. MainWindow (orchestration finale)

- [ ] 3. CRITIQUE-1: Migrer OptionGeneratorUC (section 9.2)
  - [ ] 3.1 Créer OptionGeneratorViewModel avec constructeur DI
    - Créer BIA.ToolKit.Application/ViewModel/OptionGeneratorViewModel.cs
    - Hériter de CommunityToolkit ObservableObject + IDisposable
    - Injecter via constructeur: CSharpParserService, ZipParserService, GenerateCrudService, CRUDSettings, UIEventBroker, FileGeneratorService, IConsoleWriter
    - Stocker dans readonly fields
    - Migrer ~15 propriétés avec [ObservableProperty]
    - Migrer ~5 commandes avec [RelayCommand]
- [ ] 11. Migrer RepositoryResumeUC
  - [ ] 11.1 Créer RepositoryResumeViewModel
    - Créer BIA.ToolKit/ViewModels/RepositoryResumeViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~8 propriétés avec [ObservableProperty]
    - Migrer ~1 commande avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 11.2 Extraire logique du code-behind
    - Identifier Business_Logic dans RepositoryResumeUC.xaml.cs
    - Déplacer vers RepositoryResumeViewModel
    - Garder seulement UI_Logic dans code-behind
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
- [ ] 13. Migrer RepositoryFormUC (avec validation)
  - [ ] 13.1 Créer RepositoryFormViewModel avec validation
    - Créer BIA.ToolKit/ViewModels/RepositoryFormViewModel.cs
    - Hériter de CommunityToolkit ObservableValidator
    - Migrer ~12 propriétés avec [ObservableProperty] et attributs de validation
    - Ajouter [Required], [MinLength], [Url] selon besoins
    - Migrer ~4 commandes avec [RelayCommand]
    - Implémenter validation automatique avec partial methods
    - _Requirements: 7.1, 7.2, 7.3, 11.1, 11.2, 11.6_
  
  - [ ] 13.2 Extraire logique et connecter à la vue avec bindings validation
    - Déplacer Business_Logic vers ViewModel
    - Modifier RepositoryFormUC.xaml.cs
    - Ajouter bindings XAML pour Errors et HasErrors
    - _Requirements: 8.1, 8.2, 7.5, 11.5_

- [ ] 14. Migrer CustomTemplateRepositorySettingsUC
  - [ ] 14.1 Créer CustomTemplateRepositorySettingsViewModel
    - Créer BIA.ToolKit/ViewModels/CustomTemplateRepositorySettingsViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~10 propriétés avec [ObservableProperty]
    - Migrer ~3 commandes avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 14.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier CustomTemplateRepositorySettingsUC.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_

- [ ] 15. Checkpoint Phase 2.2
  - Compiler le projet sans erreurs
  - Tester manuellement les ViewModels migrés (RepositoryResumeUC, RepositoryFormUC, CustomTemplateRepositorySettingsUC)
  - Vérifier absence de régressions
  - Demander validation utilisateur avant de continuer

- [ ] 16. Implémenter système Messenger
  - [ ] 16.1 Créer classes de messages
    - Créer BIA.ToolKit/Messages/SettingsUpdatedMessage.cs
    - Créer BIA.ToolKit/Messages/RepositoryChangedMessage.cs
    - Créer BIA.ToolKit/Messages/RepositoryDeletedMessage.cs
    - Créer BIA.ToolKit/Messages/RepositoryAddedMessage.cs
    - Créer BIA.ToolKit/Messages/OpenRepositoryFormMessage.cs
    - Utiliser records pour immutabilité
    - _Requirements: 10.5_
  
  - [ ] 16.2 Remplacer UIEventBroker par Messenger
    - Identifier tous les usages de UIEventBroker
    - Remplacer par WeakReferenceMessenger.Default.Send()
    - Implémenter IRecipient<TMessage> dans ViewModels récepteurs
    - _Requirements: 10.1, 10.2, 10.4_

- [ ] 17. Migrer CustomTemplatesRepositoriesSettingsUC
  - [ ] 17.1 Créer CustomTemplatesRepositoriesSettingsViewModel
    - Créer BIA.ToolKit/ViewModels/CustomTemplatesRepositoriesSettingsViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~8 propriétés avec [ObservableProperty]
    - Migrer ~6 commandes avec [RelayCommand]
    - Intégrer Messenger pour communication
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 17.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier CustomTemplatesRepositoriesSettingsUC.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_

- [ ] 18. Checkpoint Phase 2.3
  - Compiler le projet sans erreurs
  - Tester manuellement Messenger fonctionne correctement
  - Vérifier absence de régressions
  - Demander validation utilisateur avant de continuer

- [ ] 19. Migrer MainWindow (orchestration globale)
  - [ ] 19.1 Créer MainWindowViewModel
    - Créer BIA.ToolKit/ViewModels/MainWindowViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~20 propriétés avec [ObservableProperty]
    - Migrer ~12 commandes avec [RelayCommand]
    - Intégrer Messenger pour orchestration globale
    - Implémenter IRecipient pour messages nécessaires
    - Implémenter IDisposable (déjà fait en tâche 9.1)
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ] 19.2 Extraire logique d'orchestration du code-behind
    - Identifier toute la Business_Logic dans MainWindow.xaml.cs
    - Déplacer vers MainWindowViewModel
    - Garder seulement UI_Logic (focus, animations)
    - Réduire code-behind de 70%+
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [ ] 19.3 Connecter ViewModel à la vue
    - Modifier MainWindow.xaml.cs
    - Vérifier tous les bindings fonctionnent
    - Tester navigation entre UserControls
    - _Requirements: 7.5_

- [ ] 20. Checkpoint Phase 2 - Migration complète
  - Compiler le projet sans erreurs ni warnings
  - Tester manuellement tous les ViewModels migrés
  - Vérifier que tous les 11 ViewModels utilisent CommunityToolkit
  - Vérifier conformité avec MVVM-guidelines.md (checklist section 8)
  - Exécuter commandes de vérification (section "Vérification post-migration")
  - Demander validation utilisateur avant Phase 3
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [x] 3.2 Extraire logique du code-behind
    - Identifier Business_Logic dans RepositoryResumeUC.xaml.cs
    - Déplacer vers RepositoryResumeViewModel
    - Garder seulement UI_Logic dans code-behind
    - _Requirements: 8.1, 8.2, 8.3, 8.5_
  
  - [x] 3.3 Connecter ViewModel à la vue
    - Modifier RepositoryResumeUC.xaml.cs pour utiliser RepositoryResumeViewModel
    - Configurer DataContext
    - _Requirements: 7.5_

- [x] 4. Migrer VersionAndOptionUserControl
  - [x] 4.1 Créer VersionAndOptionViewModel
    - Créer BIA.ToolKit/ViewModels/VersionAndOptionViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~6 propriétés avec [ObservableProperty]
    - Migrer ~3 commandes avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [x] 4.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier VersionAndOptionUserControl.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_

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

- [ ] 7. Migrer OptionGeneratorUC
  - [x] 7.1 Créer OptionGeneratorViewModel
    - Créer BIA.ToolKit/ViewModels/OptionGeneratorViewModel.cs
    - Hériter de CommunityToolkit ObservableObject
    - Migrer ~15 propriétés avec [ObservableProperty]
    - Migrer ~5 commandes avec [RelayCommand]
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [x] 7.2 Extraire logique et connecter à la vue
    - Déplacer Business_Logic vers ViewModel
    - Modifier OptionGeneratorUC.xaml.cs
    - _Requirements: 8.1, 8.2, 7.5_

- [ ] 8. Checkpoint Phase 2.1
  - Compiler le projet sans erreurs
  - Tester manuellement les ViewModels migrés
  - Vérifier absence de régressions
  - Demander validation utilisateur avant de continuer
### Phase 3: Finalisation et Retrait MicroMvvm (2-3 jours)

- [ ] 21. Retirer MicroMvvm complètement
  - [ ] 21.1 Supprimer les fichiers MicroMvvm
    - Supprimer BIA.ToolKit/Helper/MicroMvvm/ObservableObject.cs
    - Supprimer BIA.ToolKit/Helper/MicroMvvm/RelayCommand.cs
    - Supprimer le dossier BIA.ToolKit/Helper/MicroMvvm/
    - _Requirements: 7.7_
  
  - [ ] 21.2 Nettoyer les références MicroMvvm
    - Rechercher tous les usings MicroMvvm dans le code
    - Vérifier qu'aucun fichier ne référence MicroMvvm
    - Compiler le projet et vérifier absence d'erreurs
    - _Requirements: 7.7_

- [ ] 22. Établir baseline de performance (optionnel)
  - Créer infrastructure de mesure de performance
  - Capturer et valider baseline post-migration
  - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

- [ ] 23. Exécuter tests de régression manuels
  - Tester tous les workflows utilisateur principaux
  - Vérifier génération CRUD fonctionne
  - Vérifier génération DTO fonctionne
  - Vérifier génération Option fonctionne
  - Vérifier modification de projet fonctionne
  - Vérifier gestion des repositories fonctionne
  - _Requirements: 14.1, 14.3, 14.4_

- [ ] 24. Vérification post-migration (MVVM-guidelines.md section "Vérification post-migration")
  - [ ] 24.1 Exécuter commandes de vérification
    - `grep -r "Inject(" BIA.ToolKit/UserControls/` → 0 résultat attendu
    - `grep -n "UserControl.DataContext" BIA.ToolKit/UserControls/CRUDGeneratorUC.xaml` → 0 résultat
    - `grep -n "UserControl.DataContext" BIA.ToolKit/UserControls/DtoGeneratorUC.xaml` → 0 résultat
    - `grep -n "UserControl.DataContext" BIA.ToolKit/UserControls/OptionGeneratorUC.xaml` → 0 résultat
    - `grep -n "TextChanged=\|SelectionChanged=" BIA.ToolKit/UserControls/DtoGeneratorUC.xaml` → 0 résultat (sauf drag-drop)
    - `grep -n "Click=\|SelectionChanged=" BIA.ToolKit/UserControls/OptionGeneratorUC.xaml` → 0 résultat
    - `grep -rn "IDisposable" BIA.ToolKit.Application/ViewModel/` → tous les VMs avec abonnements broker
    - _Requirements: 17.1, 17.2, 17.3, 19.1, 18.5_
  
  - [ ] 24.2 Build complet sans erreur
    - Compiler le projet sans erreurs ni warnings
    - Vérifier que tous les source generators fonctionnent
    - _Requirements: 14.2_

- [ ] 25. Créer documentation
  - [ ] 25.1 Rédiger guide de migration
    - Créer BIA.ToolKit/Documentation/MVVM-Migration-Guide.md
    - Documenter processus étape par étape
    - Inclure exemples avant/après
    - Documenter pièges courants et solutions
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [ ] 25.2 Rédiger documentation d'architecture
    - Créer BIA.ToolKit/Documentation/MVVM-Architecture.md
    - Décrire architecture CommunityToolkit.Mvvm
    - Inclure diagrammes
    - Documenter patterns et exemples
    - Documenter règles architecturales (MVVM-guidelines.md sections 1-7)
    - _Requirements: 12.1, 12.2, 12.3, 12.5, 12.6_

- [ ] 26. Collecter métriques de réduction du boilerplate
  - [ ] 26.1 Mesurer réduction de code par ViewModel
    - Compter lignes de code avant/après pour chaque ViewModel
    - Calculer % réduction pour propriétés (objectif: 60%+)
    - Calculer % réduction pour commandes (objectif: 50%+)
    - Calculer % réduction pour code-behind (objectif: 70%+)
    - Mesurer spécifiquement OptionGeneratorUC: 546 LOC → ~20 LOC
    - _Requirements: 13.1, 13.2, 13.3, 13.4_
  
  - [ ] 26.2 Créer rapport de migration
    - Créer BIA.ToolKit/Documentation/Migration-Report.md
    - Inclure métriques de réduction de code
    - Inclure métriques de performance
    - Inclure résumé des tests
    - Inclure timeline de migration
    - Inclure tableau des problèmes résolus (MVVM-guidelines.md section 9.1)
    - _Requirements: 13.5_

- [ ] 27. Checkpoint Final - Validation complète
  - Compiler le projet sans erreurs ni warnings
  - Exécuter tous les tests (unit, property, integration, performance)
  - Vérifier couverture de code ≥80%
  - Vérifier tous les seuils de performance respectés
  - Tester manuellement l'application complète
  - Vérifier conformité avec MVVM-guidelines.md (checklist section 8)
  - Vérifier toutes les commandes de vérification passent (section 24.1)
  - Valider avec l'équipe que la migration est complète
  - MicroMvvm complètement retiré
  - Tous les anti-patterns éliminés (proxy, Inject(), DataContext statiques, event handlers)

## Notes

- Les tâches marquées avec `*` sont optionnelles et peuvent être sautées pour un MVP plus rapide
- Chaque tâche référence les requirements spécifiques pour traçabilité
- Les checkpoints assurent validation incrémentale et permettent rollback si nécessaire
- Les property tests valident les propriétés de correction universelles
- Les unit tests valident des exemples spécifiques et cas limites
- La migration suit l'ordre de sévérité: CRITIQUE → HAUT → MOYEN (MVVM-guidelines.md section 9.1)
- Avant de modifier un fichier, le lire en entier
- Après chaque tâche terminée, build le projet et corriger les erreurs avant de passer à la suivante
- Les fichiers modèles à imiter sont listés en section 12 de MVVM-guidelines.md
- Ne pas toucher au code marqué "CONSERVER" dans la section 11.4 de MVVM-guidelines.md
- En cas de régression détectée, suivre la procédure de rollback documentée dans design.md
- MicroMvvm sera complètement retiré à la fin de la Phase 3
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
