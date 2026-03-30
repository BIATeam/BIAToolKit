# Design Document: Migration .NET 10 de BIA.ToolKit

## Overview

Ce document détaille l'approche technique pour la migration de BIA.ToolKit de .NET 9 vers .NET 10. Il s'agit d'une simple mise à jour de la propriété TargetFramework dans les fichiers .csproj.

### Objectifs

- Mettre à jour les TargetFramework de net9.0 vers net10.0
- Mettre à jour les TargetFramework de net9.0-windows10.0.19041.0 vers net10.0-windows10.0.19041.0
- Vérifier que la solution compile après la mise à jour

### Projets concernés

**7 projets à mettre à jour:**
1. BIA.ToolKit.csproj (net9.0-windows10.0.19041.0 → net10.0-windows10.0.19041.0)
2. BIA.ToolKit.Application.csproj (net9.0 → net10.0)
3. BIA.ToolKit.Common.csproj (net9.0 → net10.0)
4. BIA.ToolKit.Domain.csproj (net9.0 → net10.0)
5. BIA.ToolKit.Updater.csproj (net9.0 → net10.0)
6. BIA.ToolKit.Test.Templates.csproj (net9.0 → net10.0)
7. BIA.ToolKit.Tests.csproj (net9.0-windows10.0.19041.0 → net10.0-windows10.0.19041.0)

## Approach

### Étape 1: Mise à jour des fichiers .csproj

Pour chaque projet, remplacer la ligne `<TargetFramework>` dans le fichier .csproj:

**Pour les projets console/library:**
```xml
<!-- Avant -->
<TargetFramework>net9.0</TargetFramework>

<!-- Après -->
<TargetFramework>net10.0</TargetFramework>
```

**Pour les projets WPF:**
```xml
<!-- Avant -->
<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>

<!-- Après -->
<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
```

### Étape 2: Compilation

Après la mise à jour, compiler la solution pour vérifier qu'il n'y a pas d'erreurs:

```bash
dotnet build BIAToolKit.sln
```

## Correctness Properties

### Property 1: Mise à jour correcte des TargetFramework

*Pour tout* fichier .csproj dans la liste des 7 projets, après migration, le TargetFramework doit être "net10.0" ou "net10.0-windows10.0.19041.0" selon le type de projet.

**Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7**

### Property 2: Compilation réussie

*Après* la mise à jour de tous les TargetFramework, la commande `dotnet build BIAToolKit.sln` doit réussir sans erreurs.

**Validates: Requirement 2.1**

## Testing Strategy

### Tests manuels

1. Vérifier visuellement que chaque fichier .csproj a été mis à jour
2. Exécuter `dotnet build BIAToolKit.sln` et vérifier qu'il n'y a pas d'erreurs
3. Optionnel: Lancer l'application WPF pour vérifier qu'elle démarre correctement
