# Requirements Document

## Introduction

Ce document définit les exigences pour la migration de BIA.ToolKit de .NET 9 vers .NET 10. Il s'agit d'une simple mise à jour de version du framework dans les fichiers .csproj.

## Glossary

- **Target_Project**: Un projet .csproj qui doit être migré vers .NET 10
- **TargetFramework**: La propriété MSBuild qui définit la version du framework .NET cible

## Requirements

### Requirement 1: Mettre à jour les TargetFramework vers .NET 10

**User Story:** En tant que développeur, je veux mettre à jour les TargetFramework de .NET 9 vers .NET 10 dans tous les fichiers .csproj concernés.

#### Acceptance Criteria

1. Le fichier BIA.ToolKit.Application/BIA.ToolKit.Application.csproj doit avoir `<TargetFramework>net10.0</TargetFramework>`
2. Le fichier BIA.ToolKit.Common/BIA.ToolKit.Common.csproj doit avoir `<TargetFramework>net10.0</TargetFramework>`
3. Le fichier BIA.ToolKit.Domain/BIA.ToolKit.Domain.csproj doit avoir `<TargetFramework>net10.0</TargetFramework>`
4. Le fichier BIA.ToolKit.Updater/BIA.ToolKit.Updater.csproj doit avoir `<TargetFramework>net10.0</TargetFramework>`
5. Le fichier BIA.ToolKit.Test.Templates/BIA.ToolKit.Test.Templates.csproj doit avoir `<TargetFramework>net10.0</TargetFramework>`
6. Le fichier BIA.ToolKit/BIA.ToolKit.csproj doit avoir `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`
7. Le fichier BIA.ToolKit.Tests/BIA.ToolKit.Tests.csproj doit avoir `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`

### Requirement 2: Vérifier que la solution compile

**User Story:** En tant que développeur, je veux vérifier que la solution compile après la mise à jour.

#### Acceptance Criteria

1. La commande `dotnet build BIAToolKit.sln` doit réussir sans erreurs
