# Plan d'implémentation: Migration .NET 10 de BIA.ToolKit

## Overview

Ce plan détaille les tâches pour migrer BIA.ToolKit de .NET 9 vers .NET 10. Il s'agit simplement de mettre à jour les TargetFramework dans les fichiers .csproj et de vérifier que tout compile.

## Tasks

- [x] 1. Mettre à jour les fichiers .csproj vers .NET 10
  - [x] 1.1 Mettre à jour BIA.ToolKit.Common/BIA.ToolKit.Common.csproj
    - Remplacer `<TargetFramework>net9.0</TargetFramework>` par `<TargetFramework>net10.0</TargetFramework>`
    - _Requirements: 1.2_
  
  - [x] 1.2 Mettre à jour BIA.ToolKit.Domain/BIA.ToolKit.Domain.csproj
    - Remplacer `<TargetFramework>net9.0</TargetFramework>` par `<TargetFramework>net10.0</TargetFramework>`
    - _Requirements: 1.3_
  
  - [x] 1.3 Mettre à jour BIA.ToolKit.Application/BIA.ToolKit.Application.csproj
    - Remplacer `<TargetFramework>net9.0</TargetFramework>` par `<TargetFramework>net10.0</TargetFramework>`
    - _Requirements: 1.1_
  
  - [x] 1.4 Mettre à jour BIA.ToolKit.Updater/BIA.ToolKit.Updater.csproj
    - Remplacer `<TargetFramework>net9.0</TargetFramework>` par `<TargetFramework>net10.0</TargetFramework>`
    - _Requirements: 1.4_
  
  - [x] 1.5 Mettre à jour BIA.ToolKit.Test.Templates/BIA.ToolKit.Test.Templates.csproj
    - Remplacer `<TargetFramework>net9.0</TargetFramework>` par `<TargetFramework>net10.0</TargetFramework>`
    - _Requirements: 1.5_
  
  - [x] 1.6 Mettre à jour BIA.ToolKit/BIA.ToolKit.csproj
    - Remplacer `<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>` par `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`
    - _Requirements: 1.6_
  
  - [x] 1.7 Mettre à jour BIA.ToolKit.Tests/BIA.ToolKit.Tests.csproj
    - Remplacer `<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>` par `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`
    - _Requirements: 1.7_

- [x] 2. Vérifier la compilation
  - [x] 2.1 Compiler la solution complète
    - Exécuter `dotnet build BIAToolKit.sln`
    - Vérifier qu'il n'y a pas d'erreurs de compilation
    - _Requirements: 2.1_

- [x] 3. Commit des changements
  - [x] 3.1 Créer un commit avec les modifications
    - Commiter tous les fichiers .csproj modifiés
    - Message de commit: "chore: migration vers .NET 10"

## Notes

- Les 7 projets doivent être mis à jour de net9.0 vers net10.0 (ou net9.0-windows vers net10.0-windows pour les projets WPF)
- Le projet BIA.ToolKit.Application.Templates reste en .NET Framework 4.8 (pas de modification)
- La compilation doit réussir sans erreurs après la mise à jour
