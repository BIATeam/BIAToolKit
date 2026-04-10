# Plan d'Implémentation - Modernisation MVVM

## Pass 1 — TERMINÉ

Résumé des tâches complétées :

- CommunityToolkit.Mvvm installé + pilot LogDetailViewModel
- ViewModels migrés : VersionAndOptionViewModel, RepositoryResumeViewModel, RepositoryFormViewModel, OptionGeneratorViewModel, DtoGeneratorViewModel
- Composants code mort supprimés : CustomTemplateRepositorySettingsUC, CustomTemplatesRepositoriesSettingsUC
- UIEventBroker remplacé par CommunityToolkit.Mvvm WeakReferenceMessenger (14 messages, 7 VMs IRecipient)
- Checkpoints 2.1, 2.2, 2.3 validés (build 0 erreur, 0 warning)

## Pass 2 — TERMINÉ

- [x] MainWindow code-behind nettoyé :
  - Suppression de `BuildSettingsFromUserPreferences` et `HandleSettingsUpdated`
  - Suppression de `HandleRepositoryFormOpened` (déplacé dans `IDialogService`)
  - Suppression des handlers legacy File Generator + XAML du tab + `GenerateFilesService`
  - Code-behind réduit à ~50 lignes (wiring de vue uniquement)
- [x] Abstraction `ISettingsPersistence` créée (Application.Helper)
- [x] Implémentation `UserPreferencesSettingsPersistence` (WPF/Infrastructure) : seul endroit
  où `Properties.Settings.Default` est référencé. Gère aussi l'upgrade one-shot.
- [x] `SettingsService.Load()` / `NotifyInitialized()` + save automatique sur chaque setter
- [x] `MainViewModel.InitAsync()` (sans paramètre) charge via `SettingsService`
- [x] `IDialogService.ShowRepositoryForm` + `MainViewModel` recipient de `OpenRepositoryFormMessage`
- [x] MicroMvvm déjà totalement retiré (0 référence en code)
- [x] Build final : 0 warning, 0 erreur

## Notes

- Avant de modifier un fichier, le lire en entier
- Après chaque tâche terminée, build le projet et corriger les erreurs avant de passer à la suivante
