# 🔧 Setup des Hooks - Guide d'Installation

Ce guide explique comment recréer automatiquement tous les hooks de l'équipe virtuelle dans un nouveau projet.

## Méthode Automatique (Recommandée)

### Étape 1 : Copier le dossier .kiro
Copie tout le dossier `.kiro/` dans ton nouveau projet.

### Étape 2 : Demander à Kiro de recréer les hooks
Ouvre Kiro dans le nouveau projet et envoie ce message :

```
Peux-tu recréer tous les hooks définis dans .kiro/hooks-config.json ?
```

Kiro lira le fichier JSON et recréera automatiquement tous les hooks avec l'outil `createHook`.

## Contenu des Hooks

Le fichier `hooks-config.json` contient 4 hooks :

1. **Validation Pré-Implémentation** (preTaskExecution)
   - Valide l'architecture avant de démarrer les tâches
   - Sollicite le Tech Lead

2. **Expert .NET - Review Code** (fileEdited sur *.cs)
   - Review automatique du code backend
   - Sollicite l'expert .NET 10

3. **Review Automatique de Spec** (fileEdited sur specs/*.md)
   - Analyse automatique des fichiers de spec
   - Sollicite le Spec Reviewer

4. **Expert Angular - Review Code** (fileEdited sur *.component.ts, etc.)
   - Review automatique du code frontend
   - Sollicite l'expert Angular

## Vérification

Après la création, vérifie que les hooks sont bien créés :
- Ouvre la vue "Agent Hooks" dans l'explorateur Kiro
- Ou utilise la palette de commandes : "Open Kiro Hook UI"

## Personnalisation

Tu peux modifier `hooks-config.json` avant de demander à Kiro de les recréer :
- Changer les patterns de fichiers
- Modifier les prompts
- Ajouter/supprimer des hooks
- Ajuster les événements déclencheurs

## Désactivation Temporaire

Si certains hooks sont trop verbeux, tu peux les désactiver temporairement via l'interface "Agent Hooks" sans les supprimer.

## Support

Si tu as des questions sur les hooks, demande à Kiro :
```
#devops-engineer comment optimiser mes hooks pour ce projet ?
```
