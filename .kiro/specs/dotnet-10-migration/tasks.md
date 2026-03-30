# Plan d'implémentation: Migration .NET 10 de BIA.ToolKit

## Overview

Ce plan détaille les tâches d'implémentation pour migrer BIA.ToolKit de .NET 9 vers .NET 10. La migration suit une approche bottom-up en commençant par les projets de base, puis la couche application, les projets exécutables, et enfin les projets de test. Le projet BIA.ToolKit.Application.Templates reste en .NET Framework 4.8 pour maintenir la compatibilité T4.

## Tasks

- [ ] 1. Créer les interfaces et modèles de base
  - [ ] 1.1 Créer les interfaces IProjectFileUpdater, INuGetCompatibilityChecker, IBuildValidator, ITestRunner
    - Créer le fichier BIA.ToolKit.Application/Migration/Interfaces/IProjectFileUpdater.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Interfaces/INuGetCompatibilityChecker.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Interfaces/IBuildValidator.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Interfaces/ITestRunner.cs
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 3.1, 4.1, 5.1_
  
  - [ ] 1.2 Créer les modèles de données (UpdateResult, PackageCompatibility, BuildResult, TestResult)
    - Créer le fichier BIA.ToolKit.Application/Migration/Models/UpdateResult.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Models/PackageCompatibility.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Models/BuildResult.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Models/TestResult.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Models/ProjectConfiguration.cs
    - _Requirements: 1.1, 3.1, 4.1, 4.3, 5.3, 5.4_
  
  - [ ] 1.3 Créer les classes d'exception personnalisées
    - Créer le fichier BIA.ToolKit.Application/Migration/Exceptions/FileOperationException.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Exceptions/PackageCompatibilityException.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Exceptions/BuildFailedException.cs
    - Créer le fichier BIA.ToolKit.Application/Migration/Exceptions/TestFailedException.cs
    - _Requirements: 4.3, 5.4_

- [ ] 2. Implémenter ProjectFileUpdater
  - [ ] 2.1 Implémenter la classe ProjectFileUpdater
    - Implémenter UpdateTargetFrameworkAsync pour modifier les fichiers .csproj
    - Implémenter ShouldMigrate pour identifier les projets à migrer
    - Implémenter BackupProjectFileAsync pour créer des backups
    - Utiliser System.Xml.Linq pour parser et modifier les fichiers .csproj
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.1_
  
  - [ ]* 2.2 Écrire les tests de propriété pour ProjectFileUpdater
    - **Property 1: Mise à jour correcte des Target Frameworks**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.1**
  
  - [ ]* 2.3 Écrire les tests unitaires pour ProjectFileUpdater
    - Tester la mise à jour de BIA.ToolKit.Application.csproj (net9.0 → net10.0)
    - Tester la mise à jour de BIA.ToolKit.csproj (net9.0-windows → net10.0-windows)
    - Tester la préservation de BIA.ToolKit.Application.Templates.csproj (v4.8)
    - Tester la gestion des erreurs de fichiers manquants ou corrompus
    - _Requirements: 1.1, 1.6, 1.8, 2.1_

- [ ] 3. Implémenter NuGetCompatibilityChecker
  - [ ] 3.1 Implémenter la classe NuGetCompatibilityChecker
    - Implémenter CheckPackageCompatibilityAsync pour vérifier la compatibilité
    - Implémenter GetMinimumCompatibleVersionAsync pour obtenir la version minimale
    - Implémenter AnalyzeProjectPackagesAsync pour analyser tous les packages d'un projet
    - Utiliser NuGet.Protocol pour interroger les métadonnées des packages
    - _Requirements: 3.1, 3.2, 3.3_
  
  - [ ]* 3.2 Écrire les tests de propriété pour NuGetCompatibilityChecker
    - **Property 3: Vérification de compatibilité des packages**
    - **Property 4: Identification des packages incompatibles**
    - **Validates: Requirements 3.1, 3.2**
  
  - [ ]* 3.3 Écrire les tests unitaires pour NuGetCompatibilityChecker
    - Tester la vérification de Microsoft.Extensions.DependencyInjection
    - Tester la vérification de Microsoft.CodeAnalysis.CSharp.Workspaces
    - Tester la gestion des packages inexistants
    - Tester la gestion des erreurs réseau
    - _Requirements: 3.1, 3.2_

- [ ] 4. Checkpoint - Vérifier la compilation des composants de base
  - Compiler BIA.ToolKit.Application avec les nouveaux composants de migration
  - Exécuter les tests unitaires créés jusqu'à présent
  - Demander à l'utilisateur si des questions se posent

- [ ] 5. Implémenter BuildValidator
  - [ ] 5.1 Implémenter la classe BuildValidator
    - Implémenter BuildSolutionAsync pour compiler la solution complète
    - Implémenter BuildProjectAsync pour compiler un projet spécifique
    - Implémenter RestorePackagesAsync pour restaurer les packages NuGet
    - Utiliser Microsoft.Build.Locator et Microsoft.Build pour invoquer MSBuild
    - Parser les résultats de compilation pour extraire les erreurs et warnings
    - _Requirements: 4.1, 4.3_
  
  - [ ]* 5.2 Écrire les tests de propriété pour BuildValidator
    - **Property 6: Compilation réussie des projets migrés**
    - **Property 7: Rapport des erreurs de compilation avec localisation**
    - **Validates: Requirements 4.1, 4.3**
  
  - [ ]* 5.3 Écrire les tests unitaires pour BuildValidator
    - Tester la compilation d'un projet simple
    - Tester la restauration des packages NuGet
    - Tester la capture des erreurs de compilation
    - Tester la compilation du projet Legacy (v4.8)
    - _Requirements: 4.1, 4.2, 4.3_

- [ ] 6. Implémenter TestRunner
  - [ ] 6.1 Implémenter la classe TestRunner
    - Implémenter RunTestsAsync pour exécuter les tests d'un projet
    - Implémenter RunAllTestsAsync pour exécuter tous les tests de la solution
    - Utiliser xUnit.Runner pour exécuter les tests programmatiquement
    - Parser les résultats de test pour extraire les échecs et leurs détails
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  
  - [ ]* 6.2 Écrire les tests de propriété pour TestRunner
    - **Property 8: Rapport des résultats de tests**
    - **Property 9: Rapport des échecs de tests avec détails**
    - **Validates: Requirements 5.3, 5.4**
  
  - [ ]* 6.3 Écrire les tests unitaires pour TestRunner
    - Tester l'exécution des tests de BIA.ToolKit.Tests
    - Tester l'exécution des tests de BIA.ToolKit.Test.Templates
    - Tester la capture des échecs de tests
    - Tester le calcul des statistiques de tests
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 7. Implémenter RollbackManager
  - [ ] 7.1 Implémenter la classe RollbackManager
    - Implémenter CreateRestorePointAsync pour créer des backups
    - Implémenter RestoreFromPointAsync pour restaurer les fichiers
    - Implémenter CleanupRestorePointAsync pour nettoyer les backups
    - Utiliser un système de versioning pour les points de restauration
    - _Requirements: 4.1, 4.3_
  
  - [ ]* 7.2 Écrire les tests unitaires pour RollbackManager
    - Tester la création de points de restauration
    - Tester la restauration complète après échec
    - Tester le nettoyage des backups
    - Tester la gestion des erreurs de restauration
    - _Requirements: 4.1_

- [ ] 8. Checkpoint - Vérifier tous les composants individuels
  - Compiler tous les nouveaux composants
  - Exécuter tous les tests unitaires
  - Demander à l'utilisateur si des questions se posent

- [ ] 9. Implémenter MigrationOrchestrator
  - [ ] 9.1 Implémenter la classe MigrationOrchestrator
    - Implémenter ExecuteMigrationAsync pour orchestrer la migration complète
    - Implémenter RollbackMigrationAsync pour annuler la migration
    - Implémenter GenerateReportAsync pour générer le rapport de migration
    - Coordonner les appels aux composants (ProjectUpdater, NuGetChecker, BuildValidator, TestRunner)
    - Implémenter la logique de migration par phases (base → application → exécutables → tests)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 4.1, 4.3, 5.1, 5.2, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4_
  
  - [ ]* 9.2 Écrire les tests de propriété pour MigrationOrchestrator
    - **Property 2: Préservation des références de projet**
    - **Property 5: Mise à jour des packages incompatibles**
    - **Property 10: Documentation complète des changements**
    - **Validates: Requirements 2.3, 3.3, 7.1, 7.2, 7.3, 7.4**
  
  - [ ]* 9.3 Écrire les tests d'intégration pour MigrationOrchestrator
    - Tester la migration complète d'un projet simple
    - Tester le rollback après échec de compilation
    - Tester la génération du rapport de migration
    - Tester la gestion des erreurs à chaque phase
    - _Requirements: 1.1, 2.3, 4.1, 7.1, 7.2, 7.3, 7.4_

- [ ] 10. Créer la configuration de migration
  - [ ] 10.1 Créer le fichier de configuration MigrationConfiguration
    - Définir les mappings de frameworks (net9.0 → net10.0, net9.0-windows → net10.0-windows)
    - Définir les règles de mise à jour des packages (Microsoft.Extensions.DependencyInjection, Microsoft.CodeAnalysis.*)
    - Lister les 7 projets à migrer avec leurs chemins
    - Identifier le projet Legacy à préserver (BIA.ToolKit.Application.Templates)
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.1, 3.3_

- [ ] 11. Exécuter la migration des projets de base
  - [ ] 11.1 Migrer BIA.ToolKit.Common.csproj
    - Exécuter MigrationOrchestrator.ExecuteMigrationAsync pour BIA.ToolKit.Common
    - Vérifier la mise à jour de net9.0 → net10.0
    - Compiler le projet pour valider
    - _Requirements: 1.2, 4.1_
  
  - [ ] 11.2 Migrer BIA.ToolKit.Domain.csproj
    - Exécuter MigrationOrchestrator.ExecuteMigrationAsync pour BIA.ToolKit.Domain
    - Vérifier la mise à jour de net9.0 → net10.0
    - Compiler le projet pour valider
    - _Requirements: 1.3, 4.1_

- [ ] 12. Exécuter la migration de la couche application
  - [ ] 12.1 Migrer BIA.ToolKit.Application.csproj
    - Exécuter MigrationOrchestrator.ExecuteMigrationAsync pour BIA.ToolKit.Application
    - Vérifier la mise à jour de net9.0 → net10.0
    - Vérifier la préservation de la référence vers BIA.ToolKit.Application.Templates (v4.8)
    - Analyser et mettre à jour les packages NuGet incompatibles
    - Compiler le projet pour valider
    - _Requirements: 1.1, 2.3, 3.1, 3.2, 3.3, 4.1_
  
  - [ ]* 12.2 Écrire les tests de validation pour la migration de BIA.ToolKit.Application
    - Tester que la référence cross-framework (net10.0 → net48) fonctionne
    - Tester que Mono.TextTemplating fonctionne avec les templates T4
    - _Requirements: 2.2, 2.3_

- [ ] 13. Checkpoint - Vérifier la compilation de la couche application
  - Compiler BIA.ToolKit.Common, BIA.ToolKit.Domain, et BIA.ToolKit.Application
  - Vérifier qu'il n'y a pas d'erreurs de compilation
  - Demander à l'utilisateur si des questions se posent

- [ ] 14. Exécuter la migration des projets exécutables
  - [ ] 14.1 Migrer BIA.ToolKit.csproj (projet WPF principal)
    - Exécuter MigrationOrchestrator.ExecuteMigrationAsync pour BIA.ToolKit
    - Vérifier la mise à jour de net9.0-windows10.0.19041.0 → net10.0-windows10.0.19041.0
    - Analyser et mettre à jour les packages WPF (MaterialDesignThemes, Microsoft.Xaml.Behaviors.Wpf)
    - Compiler le projet pour valider
    - _Requirements: 1.6, 3.1, 3.3, 4.1_
  
  - [ ] 14.2 Migrer BIA.ToolKit.Updater.csproj
    - Exécuter MigrationOrchestrator.ExecuteMigrationAsync pour BIA.ToolKit.Updater
    - Vérifier la mise à jour de net9.0 → net10.0
    - Compiler le projet pour valider
    - _Requirements: 1.4, 4.1_

- [ ] 15. Exécuter la migration des projets de test
  - [ ] 15.1 Migrer BIA.ToolKit.Test.Templates.csproj
    - Exécuter MigrationOrchestrator.ExecuteMigrationAsync pour BIA.ToolKit.Test.Templates
    - Vérifier la mise à jour de net9.0 → net10.0
    - Compiler le projet pour valider
    - _Requirements: 1.5, 4.1_
  
  - [ ] 15.2 Migrer BIA.ToolKit.Tests.csproj
    - Exécuter MigrationOrchestrator.ExecuteMigrationAsync pour BIA.ToolKit.Tests
    - Vérifier la mise à jour de net9.0-windows10.0.19041.0 → net10.0-windows10.0.19041.0
    - Compiler le projet pour valider
    - _Requirements: 1.7, 4.1_

- [ ] 16. Vérifier la compilation complète de la solution
  - [ ] 16.1 Compiler la solution BIA.ToolKit.sln
    - Exécuter BuildValidator.BuildSolutionAsync pour toute la solution
    - Vérifier qu'il n'y a pas d'erreurs de compilation
    - Capturer et rapporter les warnings éventuels
    - _Requirements: 4.1, 4.2, 4.3_
  
  - [ ]* 16.2 Écrire les tests de validation de compilation
    - Tester que tous les projets migrés compilent sans erreurs
    - Tester que le projet Legacy compile toujours
    - _Requirements: 4.1, 4.2_

- [ ] 17. Checkpoint - Vérifier la compilation complète
  - S'assurer que la solution complète compile sans erreurs
  - Demander à l'utilisateur si des questions se posent

- [ ] 18. Exécuter et valider les tests
  - [ ] 18.1 Exécuter tous les tests de la solution
    - Exécuter TestRunner.RunAllTestsAsync pour BIA.ToolKit.Tests
    - Exécuter TestRunner.RunAllTestsAsync pour BIA.ToolKit.Test.Templates
    - Capturer les résultats (passed, failed, skipped)
    - Rapporter les échecs avec détails (nom du test, message d'erreur, stack trace)
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  
  - [ ]* 18.2 Écrire les tests de validation pour TestRunner
    - Tester l'exécution des tests et la capture des résultats
    - Tester le rapport des échecs de tests
    - _Requirements: 5.3, 5.4_

- [ ] 19. Générer le rapport de migration
  - [ ] 19.1 Implémenter la génération du rapport de migration
    - Créer le fichier BIA.ToolKit.Application/Migration/Reports/MigrationReportGenerator.cs
    - Documenter tous les changements de TargetFramework (7 projets)
    - Documenter toutes les mises à jour de packages NuGet
    - Documenter les breaking changes rencontrés
    - Documenter les workarounds appliqués
    - Générer un fichier markdown avec le rapport complet
    - _Requirements: 7.1, 7.2, 7.3, 7.4_
  
  - [ ]* 19.2 Écrire les tests de propriété pour le rapport de migration
    - **Property 10: Documentation complète des changements**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.4**
  
  - [ ]* 19.3 Écrire les tests unitaires pour le rapport de migration
    - Tester la génération du rapport avec tous les changements
    - Tester le format markdown du rapport
    - Tester l'inclusion de tous les projets migrés
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [ ] 20. Créer l'interface utilisateur ou CLI pour la migration
  - [ ] 20.1 Créer une commande CLI ou un bouton dans l'interface WPF
    - Ajouter un UserControl ou une commande pour lancer la migration
    - Afficher la progression de la migration (phase actuelle, projet en cours)
    - Afficher les résultats de la migration (succès, erreurs, warnings)
    - Permettre à l'utilisateur de consulter le rapport de migration
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 7.1, 7.2, 7.3, 7.4_

- [ ] 21. Validation finale et tests manuels
  - [ ] 21.1 Valider le lancement de l'application WPF
    - Lancer BIA.ToolKit.exe après migration
    - Vérifier que tous les UserControls se chargent correctement
    - Vérifier que les fonctionnalités principales fonctionnent (CRUD Generator, DTO Generator, etc.)
    - _Requirements: 6.1, 6.2, 6.3_
  
  - [ ] 21.2 Exécuter tous les tests de propriété
    - Exécuter tous les tests FsCheck avec 100 itérations minimum
    - Vérifier que toutes les propriétés sont satisfaites
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 2.3, 3.1, 3.2, 3.3, 4.1, 4.3, 5.3, 5.4, 7.1, 7.2, 7.3, 7.4_

- [ ] 22. Checkpoint final - Vérifier que tout fonctionne
  - S'assurer que tous les tests passent
  - S'assurer que l'application WPF démarre et fonctionne
  - S'assurer que le rapport de migration est complet
  - Demander à l'utilisateur si des questions se posent

## Notes

- Les tâches marquées avec `*` sont optionnelles et peuvent être sautées pour un MVP plus rapide
- Chaque tâche référence les exigences spécifiques pour la traçabilité
- Les checkpoints garantissent une validation incrémentale
- Les tests de propriété valident les propriétés de correction universelles
- Les tests unitaires valident des exemples spécifiques et des cas limites
- La migration suit une approche bottom-up pour minimiser les risques
- Le projet BIA.ToolKit.Application.Templates doit rester en .NET Framework 4.8
