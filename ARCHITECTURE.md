# Architecture - BIA.ToolKit

## Vue d'ensemble

BIA.ToolKit suit une architecture **Clean Architecture** avec le pattern **MVVM** pour la couche présentation WPF.

```
┌─────────────────────────────────────────────────────────────────────┐
│                          PRÉSENTATION                                │
│  BIA.ToolKit (WPF)                                                  │
│  ├── Views (*.xaml)                                                 │
│  ├── ViewModels (orchestration UI uniquement)                       │
│  ├── Behaviors, Controls, Dialogs                                   │
│  └── Converters                                                     │
├─────────────────────────────────────────────────────────────────────┤
│                          APPLICATION                                 │
│  BIA.ToolKit.Application                                            │
│  ├── Services/ (logique métier)                                     │
│  │   ├── CRUD/        → Génération CRUD                            │
│  │   ├── DTO/         → Génération DTO                             │
│  │   ├── Option/      → Génération Options                         │
│  │   └── ProjectMigration/ → Migration de projets                  │
│  ├── Helper/          (helpers métier)                              │
│  ├── Parser/          (analyse de code)                             │
│  └── Settings/        (configuration)                               │
├─────────────────────────────────────────────────────────────────────┤
│                           DOMAINE                                    │
│  BIA.ToolKit.Domain                                                 │
│  ├── Model/           (entités métier)                              │
│  ├── ModifyProject/   (modèles de modification)                     │
│  ├── ProjectAnalysis/ (analyse de projets)                          │
│  ├── Settings/        (configuration domaine)                       │
│  └── Work/            (objets de travail)                           │
├─────────────────────────────────────────────────────────────────────┤
│                        INFRASTRUCTURE                                │
│  BIA.ToolKit.Infrastructure                                         │
│  └── Services/        (accès fichiers, Git, API)                    │
├─────────────────────────────────────────────────────────────────────┤
│                           COMMON                                     │
│  BIA.ToolKit.Common                                                 │
│  ├── Extensions/      (méthodes d'extension)                        │
│  ├── Helpers/         (utilitaires génériques)                      │
│  └── Constants        (constantes partagées)                        │
└─────────────────────────────────────────────────────────────────────┘
```

## Projets de la Solution

| Projet | Rôle | Dépendances |
|--------|------|-------------|
| **BIA.ToolKit** | Application WPF principale | Application, Common |
| **BIA.ToolKit.Application** | Services métier et logique | Domain, Common |
| **BIA.ToolKit.Application.Templates** | Templates de génération | - |
| **BIA.ToolKit.Domain** | Modèles et interfaces | Common |
| **BIA.ToolKit.Infrastructure** | Implémentations techniques | Domain, Common |
| **BIA.ToolKit.Common** | Utilitaires partagés | - |
| **BIA.ToolKit.Updater** | Application de mise à jour | - |
| **BIA.ToolKit.Test.Templates** | Tests des templates | - |
| **Bia.ToolKit.MSIX** | Package MSIX | - |

## Pattern MVVM

### Structure d'un ViewModel

```csharp
// Hériter de ViewModelBase du toolkit MVVM
public partial class MyViewModel : ObservableObject
{
    // 1. DÉPENDANCES (injectées via constructeur)
    private readonly IMyService _myService;
    private readonly IMessenger _messenger;

    // 2. PROPRIÉTÉS OBSERVABLES (pour le binding)
    [ObservableProperty]
    private string _myProperty;

    // 3. CONSTRUCTEUR (injection de dépendances)
    public MyViewModel(IMyService myService, IMessenger messenger)
    {
        _myService = myService;
        _messenger = messenger;
    }

    // 4. COMMANDES (actions UI)
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        // Déléguer au service métier
        var result = await _myService.LoadDataAsync();
        MyProperty = result;
    }

    // 5. HANDLERS DE MESSAGES (communication inter-ViewModels)
    private void OnSomeMessage(SomeMessage message)
    {
        // Réagir aux messages
    }
}
```

### Responsabilités du ViewModel

| ✅ Responsabilités du ViewModel | ❌ À éviter dans le ViewModel |
|--------------------------------|-------------------------------|
| Orchestration de l'UI | Logique métier complexe |
| Binding de données | Accès direct aux fichiers |
| Gestion des commandes | Parsing de code |
| Communication via Messenger | Requêtes HTTP/API |
| Validation UI | Manipulation de Git |

## Services Métier (Application Layer)

### Services Créés

#### ICRUDGenerationService
Gère la génération de code CRUD (Create, Read, Update, Delete).
- `InitializeAsync()` - Initialise le contexte
- `ListDtoFiles()` - Liste les fichiers DTO disponibles
- `ParseDtoFile()` - Parse un fichier DTO
- `GenerateAsync()` - Génère le code CRUD
- `DeleteLastGeneration()` - Supprime la dernière génération
- `GetHistory()` / `UpdateHistory()` - Gère l'historique

#### IDtoGenerationService
Gère la génération de fichiers DTO.
- `Initialize()` - Initialise le service
- `ListEntitiesAsync()` - Liste les entités disponibles
- `GenerateAsync()` - Génère les DTOs
- `LoadFromHistory()` / `UpdateHistory()` - Gère l'historique

#### IOptionGenerationService
Gère la génération des options.
- `InitializeAsync()` - Initialise le contexte
- `ListEntities()` - Liste les entités
- `ParseEntity()` - Parse une entité
- `GenerateAsync()` - Génère les options
- `DeleteLastGeneration()` - Supprime la dernière génération

#### IProjectMigrationService
Gère la migration de projets entre versions.
- `LoadProjectAsync()` - Charge un projet
- `ParseProjectAsync()` - Analyse le projet
- `MigrateAsync()` - Effectue la migration
- `ApplyDiffAsync()` - Applique les différences
- `MergeRejectedAsync()` - Merge les rejets

### Injection de Dépendances

Les services sont enregistrés dans `App.xaml.cs` via les méthodes d'extension :

```csharp
// Dans BIA.ToolKit.Application/Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddCRUDServices();
    services.AddDtoServices();
    services.AddProjectServices();
    services.AddOptionServices();
    return services;
}
```

## Communication Inter-ViewModels

Utiliser **CommunityToolkit.Mvvm.Messaging** pour la communication découplée :

```csharp
// Définir un message
public class ProjectSelectedMessage
{
    public Project Project { get; }
    public ProjectSelectedMessage(Project project) => Project = project;
}

// Envoyer
_messenger.Send(new ProjectSelectedMessage(selectedProject));

// Recevoir (dans le constructeur ou via [Recipient])
_messenger.Register<ProjectSelectedMessage>(this, (r, m) => 
{
    CurrentProject = m.Project;
});
```

## Flux de Données Typique

```
┌─────────┐    ┌───────────┐    ┌─────────┐    ┌──────────────┐
│  View   │───▶│ ViewModel │───▶│ Service │───▶│ Infrastructure│
│ (XAML)  │    │           │    │ (Métier)│    │  (IO, API)   │
└─────────┘    └───────────┘    └─────────┘    └──────────────┘
     ▲              │                │               │
     │              │                │               │
     └──────────────┴────────────────┴───────────────┘
                    Données/Résultats
```

## Fichiers de Configuration

| Fichier | Emplacement | Rôle |
|---------|-------------|------|
| `App.config` | BIA.ToolKit/ | Configuration applicative |
| `settings.json` | %AppData%/BIA.ToolKit/ | Préférences utilisateur |
| `RepositorySettings.json` | BIA.ToolKit.Domain/ | Configuration des dépôts |

## Tests

### Structure des Tests
```
BIA.ToolKit.Test.Templates/
├── _5_0_0/          # Tests version 5.0.0
├── _6_0_0/          # Tests version 6.0.0
├── Assertions/      # Assertions personnalisées
└── Helpers/         # Helpers de test
```

### Exécution
```bash
dotnet test BIAToolKit.sln
```

## État de la Refactorisation

### ✅ Terminé
- Phase 1: Cartographie (voir `EXTRACTION_MAPPING.txt`)
- Phase 2: Services métier créés dans `BIA.ToolKit.Application/Services/`
- Configuration DI dans `App.xaml.cs`

### 🔄 En Cours / À Faire
- Phase 3: Minifier les ViewModels (utiliser les nouveaux services)
- Phase 4: Restructurer l'architecture des ViewModels
- Phase 5: Nettoyage des fichiers redondants
- Phase 6: Tests et validation

## Conventions de Nommage

Voir [CODING_STANDARDS.md](CODING_STANDARDS.md) pour les standards de code détaillés.

## Ressources

- [CommunityToolkit.Mvvm Documentation](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
