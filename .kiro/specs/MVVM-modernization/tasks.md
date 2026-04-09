# Plan d'Implémentation - Modernisation MVVM

## Pass 1 — TERMINÉ

Résumé des tâches complétées :

- CommunityToolkit.Mvvm installé + pilot LogDetailViewModel
- ViewModels migrés : VersionAndOptionViewModel, RepositoryResumeViewModel, RepositoryFormViewModel, OptionGeneratorViewModel, DtoGeneratorViewModel
- Composants code mort supprimés : CustomTemplateRepositorySettingsUC, CustomTemplatesRepositoriesSettingsUC
- UIEventBroker remplacé par CommunityToolkit.Mvvm WeakReferenceMessenger (14 messages, 7 VMs IRecipient)
- Checkpoints 2.1, 2.2, 2.3 validés (build 0 erreur, 0 warning)

## Pass 2 — Corrections et retours

_En attente des retours utilisateur pour définir les tâches._

## Tâches restantes (en pause)

- [ ] Migrer MainWindow (orchestration globale) — EN PAUSE
- [ ] Retirer MicroMvvm complètement
- [ ] Vérification post-migration (MVVM-guidelines.md)
- [ ] Build final sans erreur

## Notes

- Avant de modifier un fichier, le lire en entier
- Après chaque tâche terminée, build le projet et corriger les erreurs avant de passer à la suivante
- Les fichiers modèles à imiter sont listés en section 12 de MVVM-guidelines.md
- Ne pas toucher au code marqué "CONSERVER" dans la section 11.4 de MVVM-guidelines.md
