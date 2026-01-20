# Standards de Code - BIA.ToolKit

## Table des Matières
1. [Conventions de Nommage](#conventions-de-nommage)
2. [Structure des Fichiers](#structure-des-fichiers)
3. [Pattern MVVM](#pattern-mvvm)
4. [Services Métier](#services-métier)
5. [Gestion des Erreurs](#gestion-des-erreurs)
6. [Async/Await](#asyncawait)
7. [Injection de Dépendances](#injection-de-dépendances)
8. [Tests](#tests)

---

## Conventions de Nommage

### Général

| Type | Convention | Exemple |
|------|------------|---------|
| Classe | PascalCase | `CRUDGenerationService` |
| Interface | IPascalCase | `ICRUDGenerationService` |
| Méthode | PascalCase | `GenerateAsync()` |
| Propriété publique | PascalCase | `ProjectPath` |
| Champ privé | _camelCase | `_projectService` |
| Variable locale | camelCase | `selectedFile` |
| Constante | SCREAMING_SNAKE | `MAX_RETRY_COUNT` |
| Paramètre | camelCase | `filePath` |

### ViewModels

```csharp
// ✅ Bon
public partial class CRUDGeneratorViewModel : ObservableObject { }

// ❌ Éviter
public class CRUDGeneratorVM : ViewModelBase { }
public class CrudGeneratorViewModel : ViewModelBase { }
```

### Services

```csharp
// ✅ Bon - Interface avec 'I' prefix
public interface ICRUDGenerationService { }

// ✅ Bon - Implémentation sans prefix
public class CRUDGenerationService : ICRUDGenerationService { }

// ❌ Éviter
public interface CRUDGenerationService { }
public class CRUDGenerationServiceImpl { }
```

### Propriétés Observable (CommunityToolkit.Mvvm)

```csharp
// ✅ Bon - Champ privé avec underscore, le source generator créé la propriété publique
[ObservableProperty]
private string _projectName;
// Génère automatiquement: public string ProjectName { get; set; }

// ✅ Bon - Avec notification de changement
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(CanGenerate))]
private string _selectedFile;

// ❌ Éviter - Pas d'underscore
[ObservableProperty]
private string projectName;
```

---

## Structure des Fichiers

### Organisation des Dossiers

```
BIA.ToolKit/
├── Behaviors/           # Behaviors WPF attachés
├── Controls/            # Contrôles personnalisés
├── Converters/          # Convertisseurs de valeur
├── Dialogs/             # Fenêtres de dialogue
├── Helper/              # Helpers spécifiques UI (à migrer)
├── Images/              # Ressources images
├── Mapper/              # Mappers AutoMapper (si utilisé)
├── Messages/            # Messages pour le Messenger
├── Services/            # Services UI uniquement
├── UserControls/        # User controls
└── ViewModels/          # ViewModels
    ├── Generators/      # ViewModels de génération
    ├── Repository/      # ViewModels de gestion repo
    └── Shared/          # ViewModels partagés

BIA.ToolKit.Application/
├── Extensions/          # Méthodes d'extension
├── Helper/              # Helpers métier
│   ├── CRUD/
│   ├── DTO/
│   └── Option/
├── Parser/              # Parsers de code
├── Services/            # Services métier
│   ├── CRUD/
│   ├── DTO/
│   ├── Option/
│   └── ProjectMigration/
├── Settings/            # Configuration
└── ViewModel/           # ViewModels partagés applicatifs
```

### Un Fichier = Une Classe

```csharp
// ✅ Bon - CRUDGenerationService.cs
public class CRUDGenerationService : ICRUDGenerationService { }

// ❌ Éviter - Plusieurs classes dans un fichier
public class CRUDGenerationService { }
public class DtoGenerationService { }  // Doit être dans son propre fichier
```

### Exception : Classes Internes Liées

```csharp
// ✅ Acceptable - Classes de résultat liées au service
// Dans ICRUDGenerationService.cs

public interface ICRUDGenerationService { }

// Classes de résultat directement liées
public class CRUDInitializationResult { }
public class CRUDDtoParseResult { }
public class CRUDGenerationRequest { }
```

---

## Pattern MVVM

### Structure d'un ViewModel

```csharp
public partial class ExampleViewModel : ObservableObject
{
    #region Dépendances
    private readonly IExampleService _exampleService;
    private readonly IMessenger _messenger;
    #endregion

    #region Propriétés Observable
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<Item> _items;
    #endregion

    #region Propriétés Calculées
    public bool CanSave => !string.IsNullOrEmpty(Name);
    #endregion

    #region Constructeur
    public ExampleViewModel(IExampleService exampleService, IMessenger messenger)
    {
        _exampleService = exampleService;
        _messenger = messenger;
        _items = new ObservableCollection<Item>();
        
        RegisterMessages();
    }
    #endregion

    #region Commandes
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await _exampleService.SaveAsync(Name);
    }

    [RelayCommand]
    private void Cancel()
    {
        Name = string.Empty;
    }
    #endregion

    #region Messages
    private void RegisterMessages()
    {
        _messenger.Register<DataChangedMessage>(this, OnDataChanged);
    }

    private void OnDataChanged(object recipient, DataChangedMessage message)
    {
        // Traiter le message
    }
    #endregion

    #region Méthodes Privées
    private void ValidateInput()
    {
        // Logique de validation UI
    }
    #endregion
}
```

### Taille Maximum d'un ViewModel

| Métrique | Seuil | Action |
|----------|-------|--------|
| Lignes de code | < 200 | ✅ OK |
| Lignes de code | 200-400 | ⚠️ Refactoriser bientôt |
| Lignes de code | > 400 | ❌ Refactoriser immédiatement |
| Dépendances | < 5 | ✅ OK |
| Dépendances | 5-8 | ⚠️ Vérifier le SRP |
| Dépendances | > 8 | ❌ Diviser le ViewModel |

### Ce que NE DOIT PAS faire un ViewModel

```csharp
// ❌ PAS de logique métier
public void Generate()
{
    // Mauvais : parsing, transformation, etc.
    var lines = File.ReadAllLines(filePath);
    var parsed = lines.Where(l => l.Contains("class")).ToList();
    // ...
}

// ✅ Déléguer au service
public async Task GenerateAsync()
{
    var result = await _generationService.GenerateAsync(request);
    Items = new ObservableCollection<Item>(result.Items);
}
```

---

## Services Métier

### Structure d'un Service

```csharp
public interface IExampleService
{
    Task<Result> ProcessAsync(Request request);
    IEnumerable<Item> ListItems();
}

public class ExampleService : IExampleService
{
    private readonly IDependency _dependency;

    public ExampleService(IDependency dependency)
    {
        _dependency = dependency;
    }

    public async Task<Result> ProcessAsync(Request request)
    {
        // Validation
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Logique métier
        var data = await _dependency.GetDataAsync();
        
        // Transformation
        return new Result { Data = data };
    }

    public IEnumerable<Item> ListItems()
    {
        return _dependency.GetItems();
    }
}
```

### Enregistrement DI

```csharp
// Dans ServiceCollectionExtensions.cs
public static IServiceCollection AddExampleServices(this IServiceCollection services)
{
    services.AddSingleton<IExampleService, ExampleService>();
    return services;
}
```

---

## Gestion des Erreurs

### Dans les Services

```csharp
public async Task<GenerationResult> GenerateAsync(GenerationRequest request)
{
    try
    {
        // Logique métier
        return new GenerationResult { Success = true };
    }
    catch (FileNotFoundException ex)
    {
        return new GenerationResult 
        { 
            Success = false, 
            Error = $"Fichier non trouvé: {ex.FileName}" 
        };
    }
    catch (Exception ex)
    {
        // Logger l'erreur
        return new GenerationResult 
        { 
            Success = false, 
            Error = "Une erreur inattendue s'est produite." 
        };
    }
}
```

### Dans les ViewModels

```csharp
[RelayCommand]
private async Task GenerateAsync()
{
    try
    {
        IsLoading = true;
        var result = await _service.GenerateAsync(request);
        
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            return;
        }
        
        // Succès
        Items = result.Items;
    }
    finally
    {
        IsLoading = false;
    }
}
```

---

## Async/Await

### Conventions de Nommage

```csharp
// ✅ Suffixe Async pour les méthodes asynchrones
public async Task<Result> LoadDataAsync()

// ✅ Même pour les commandes
[RelayCommand]
private async Task SaveAsync()

// ❌ Éviter
public async Task<Result> LoadData()
public async void SaveAsync()  // Jamais async void sauf event handlers
```

### Bonnes Pratiques

```csharp
// ✅ Bon - ConfigureAwait(false) dans les services (pas d'UI)
public async Task<Data> GetDataAsync()
{
    var result = await _httpClient.GetAsync(url).ConfigureAwait(false);
    return await result.Content.ReadAsAsync<Data>().ConfigureAwait(false);
}

// ✅ Bon - Pas de ConfigureAwait dans les ViewModels (besoin du contexte UI)
[RelayCommand]
private async Task LoadAsync()
{
    var data = await _service.GetDataAsync();
    Items = data;  // Mise à jour UI
}

// ❌ Éviter - .Result ou .Wait() qui bloquent
var data = _service.GetDataAsync().Result;  // Peut causer un deadlock
```

---

## Injection de Dépendances

### Principes

1. **Dépendre des abstractions** (interfaces), pas des implémentations
2. **Injecter via constructeur**
3. **Éviter le Service Locator pattern**

```csharp
// ✅ Bon
public class MyViewModel
{
    private readonly IService _service;
    
    public MyViewModel(IService service)
    {
        _service = service;
    }
}

// ❌ Éviter - Service Locator
public class MyViewModel
{
    private readonly IService _service;
    
    public MyViewModel()
    {
        _service = ServiceLocator.Get<IService>();
    }
}
```

### Cycles de Vie

| Cycle | Usage | Exemple |
|-------|-------|---------|
| Singleton | Services stateless, configuration | `IConfigurationService` |
| Scoped | Par opération/requête | N/A (WPF desktop) |
| Transient | Services stateful par instance | `IFileDialog` |

---

## Tests

### Nommage des Tests

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
}

// Exemples
[Fact]
public void Parse_ValidDto_ReturnsProperties()
[Fact]
public void Generate_EmptyInput_ThrowsArgumentException()
[Fact]
public async Task LoadAsync_FileNotFound_ReturnsError()
```

### Structure AAA

```csharp
[Fact]
public async Task GenerateAsync_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var mockDependency = new Mock<IDependency>();
    mockDependency.Setup(d => d.GetData()).Returns(testData);
    var service = new MyService(mockDependency.Object);
    var request = new Request { Name = "Test" };

    // Act
    var result = await service.GenerateAsync(request);

    // Assert
    Assert.True(result.Success);
    Assert.NotEmpty(result.Items);
}
```

---

## Checklist de Revue de Code

### ViewModel
- [ ] Moins de 200 lignes
- [ ] Pas de logique métier (délégué aux services)
- [ ] Utilise `[ObservableProperty]` pour les propriétés
- [ ] Utilise `[RelayCommand]` pour les commandes
- [ ] Dépendances injectées via constructeur
- [ ] Pas d'accès direct aux fichiers/API

### Service
- [ ] Interface définie
- [ ] Enregistré dans DI
- [ ] Gestion d'erreurs appropriée
- [ ] Tests unitaires présents
- [ ] Pas de dépendance sur l'UI

### Général
- [ ] Nommage cohérent
- [ ] Pas de code dupliqué
- [ ] Comments XML pour les API publiques
- [ ] Pas de TODO/FIXME non traités
