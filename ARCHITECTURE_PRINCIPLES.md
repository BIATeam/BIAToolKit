# 🏛️ Principes d'Architecture - BIA.ToolKit Refactoring

**Date**: 22 janvier 2026  
**Objectif**: Définir les règles strictes pour Clean Architecture + MVVM

---

## 🎯 Clean Architecture

### Layering Strict

```
┌─────────────────────────────────────────────┐
│  PRESENTATION LAYER                          │
│  (BIA.ToolKit project)                       │
│                                              │
│  Views (.xaml + minimal .xaml.cs)            │
│  ├── MainWindow.xaml                         │
│  ├── UserControls/*.xaml                     │
│  └── Dialogs/*.xaml                          │
│                                              │
│  ViewModels (*ViewModel.cs)                  │
│  ├── MainViewModel.cs                        │
│  ├── CRUDGeneratorViewModel.cs               │
│  └── ... (ObservableObject + Commands)       │
├──────────────────────────────────────────────┤
│  APPLICATION LAYER                           │
│  (BIA.ToolKit.Application project)           │
│                                              │
│  Services (orchestration)                    │
│  ├── GenerateCrudService                     │
│  ├── ParseProjectService                     │
│  └── IFileDialogService, IDialogService      │
│                                              │
│  Helpers (reusable logic)                    │
│  ├── MainWindowHelper                        │
│  ├── CRUDGeneratorHelper                     │
│  └── ...                                     │
│                                              │
│  DTOs / Messages                             │
│  └── Communication contracts                 │
├──────────────────────────────────────────────┤
│  DOMAIN LAYER                                │
│  (BIA.ToolKit.Domain project)                │
│                                              │
│  Entities                                    │
│  ├── DtoEntity                               │
│  ├── Repository                              │
│  └── ...                                     │
│                                              │
│  Business Rules                              │
│  └── Validation logic                        │
│                                              │
│  Domain Services                             │
│  └── Pure business logic                     │
├──────────────────────────────────────────────┤
│  INFRASTRUCTURE LAYER                        │
│  (BIA.ToolKit.Infrastructure project)        │
│                                              │
│  External Services Implementation            │
│  ├── FileDialogService                       │
│  ├── GitService                              │
│  └── RepositoryService                       │
└──────────────────────────────────────────────┘
```

### Dependency Flow

```
Presentation → Application → Domain ← Infrastructure
     ↓                                      ↑
   Views                                    |
     ↓                                      |
ViewModels ──── DI Container ──────────────┘
```

**Règles**:
- Presentation dépend de Application (services, helpers)
- Application dépend de Domain (entities)
- Infrastructure implémente Domain interfaces
- Domain ne dépend de RIEN (pure business logic)

---

## 🎨 MVVM Strict Pattern

### View (.xaml + .xaml.cs)

**Responsabilités UNIQUES**:
- Définir UI structure (XAML)
- Initializer component
- Binder DataContext au ViewModel
- **RIEN D'AUTRE**

**Interdit**:
- ❌ Logique métier
- ❌ Appels services
- ❌ Manipulation données
- ❌ Event handlers (sauf Loaded/Unloaded)
- ❌ MessageBox.Show
- ❌ File I/O

**Example Correct**:
```csharp
public partial class CRUDGeneratorUC : UserControl
{
    public CRUDGeneratorUC(CRUDGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

**XAML Binding Example**:
```xml
<Button Content="Generate" 
        Command="{Binding GenerateCommand}"
        IsEnabled="{Binding CanGenerate}"/>

<TextBox Text="{Binding EntityName, UpdateSourceTrigger=PropertyChanged}"/>

<ComboBox ItemsSource="{Binding DtoList}"
          SelectedItem="{Binding SelectedDto}"/>
```

---

### ViewModel (*ViewModel.cs)

**Responsabilités**:
- Exposer Properties pour binding
- Exposer Commands pour actions utilisateur
- Orchestrer Services/Helpers
- Validation
- State management
- Navigation logic

**Doit hériter**: `ObservableObject` (CommunityToolkit.Mvvm)

**Patterns à utiliser**:

#### 1. Observable Property
```csharp
[ObservableProperty]
private string entityName;

// Génère automatiquement:
// - public string EntityName { get; set; }
// - INotifyPropertyChanged
// - PropertyChanged event
```

#### 2. Observable Property avec Reaction
```csharp
[ObservableProperty]
private DtoEntity selectedDto;

partial void OnSelectedDtoChanged(DtoEntity value)
{
    // Réaction automatique au changement
    LoadDtoDetailsAsync(value).FireAndForget();
}
```

#### 3. Command Pattern
```csharp
[RelayCommand]
private async Task GenerateAsync()
{
    try
    {
        IsGenerating = true;
        var result = await helper.GenerateCRUDAsync(SelectedDto);
        await dialogService.ShowSuccessAsync("Generation complete!");
    }
    catch (Exception ex)
    {
        await dialogService.ShowErrorAsync(ex.Message);
    }
    finally
    {
        IsGenerating = false;
    }
}

// Génère automatiquement:
// - public IAsyncRelayCommand GenerateCommand { get; }
// - CanExecute logic
```

#### 4. Command avec CanExecute
```csharp
[RelayCommand(CanExecute = nameof(CanGenerate))]
private async Task GenerateAsync()
{
    // ...
}

private bool CanGenerate()
{
    return SelectedDto != null && !string.IsNullOrEmpty(EntityName);
}

// Trigger re-evaluation
partial void OnSelectedDtoChanged(DtoEntity value)
{
    GenerateCommand.NotifyCanExecuteChanged();
}
```

#### 5. Command avec Parameter
```csharp
[RelayCommand]
private async Task DeleteItemAsync(DtoEntity dto)
{
    var confirmed = await dialogService.ConfirmAsync(
        $"Delete {dto.Name}?",
        "Confirm Delete"
    );
    
    if (confirmed)
    {
        await helper.DeleteAsync(dto);
    }
}
```

**Constructor Injection**:
```csharp
public CRUDGeneratorViewModel(
    CRUDGeneratorHelper helper,
    IDialogService dialogService,
    IFileDialogService fileDialogService,
    ILogger<CRUDGeneratorViewModel> logger
)
{
    this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
    this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    this.fileDialogService = fileDialogService;
    this.logger = logger;
}
```

**Interdit dans ViewModel**:
- ❌ References to UI controls (TextBox, Button, etc.)
- ❌ `MessageBox.Show()` → Use IDialogService
- ❌ `new OpenFileDialog()` → Use IFileDialogService
- ❌ Static service access
- ❌ Thread.Sleep / Task.Delay in business logic

---

### Services

**Application Services** (Orchestration):
```csharp
public class GenerateCrudService : IGenerateCrudService
{
    private readonly ICSharpParserService parser;
    private readonly IFileService fileService;
    private readonly ILogger logger;
    
    public GenerateCrudService(
        ICSharpParserService parser,
        IFileService fileService,
        ILogger<GenerateCrudService> logger
    )
    {
        this.parser = parser;
        this.fileService = fileService;
        this.logger = logger;
    }
    
    public async Task<GenerationResult> GenerateAsync(
        DtoEntity dto,
        GenerationOptions options
    )
    {
        logger.LogInformation($"Generating CRUD for {dto.Name}");
        
        // Orchestration logic
        var parsed = await parser.ParseAsync(dto);
        var files = await GenerateFilesAsync(parsed, options);
        await fileService.WriteFilesAsync(files);
        
        return new GenerationResult { Success = true, FilesGenerated = files.Count };
    }
}
```

**Infrastructure Services** (External):
```csharp
public class FileDialogService : IFileDialogService
{
    public Task<string?> OpenFolderDialogAsync()
    {
        var dialog = new FolderBrowserDialog();
        var result = dialog.ShowDialog();
        
        return Task.FromResult(
            result == DialogResult.OK ? dialog.SelectedPath : null
        );
    }
}
```

---

## 🔧 Dependency Injection

### Configuration (App.xaml.cs)

```csharp
public partial class App : Application
{
    private readonly IServiceProvider serviceProvider;
    
    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // ViewModels (Transient - nouvelle instance à chaque injection)
        services.AddTransient<MainViewModel>();
        services.AddTransient<CRUDGeneratorViewModel>();
        services.AddTransient<OptionGeneratorViewModel>();
        services.AddTransient<DtoGeneratorViewModel>();
        services.AddTransient<ModifyProjectViewModel>();
        services.AddTransient<VersionAndOptionViewModel>();
        
        // Views (Transient)
        services.AddTransient<MainWindow>();
        services.AddTransient<CRUDGeneratorUC>();
        services.AddTransient<OptionGeneratorUC>();
        services.AddTransient<DtoGeneratorUC>();
        services.AddTransient<ModifyProjectUC>();
        services.AddTransient<VersionAndOptionUserControl>();
        
        // Helpers (Transient)
        services.AddTransient<MainWindowHelper>();
        services.AddTransient<CRUDGeneratorHelper>();
        services.AddTransient<OptionGeneratorHelper>();
        services.AddTransient<DtoGeneratorHelper>();
        
        // Application Services (Singleton - shared state)
        services.AddSingleton<IRepositoryService, RepositoryService>();
        services.AddSingleton<IGitService, GitService>();
        services.AddSingleton<IRetrieveVersionService, RetrieveVersionService>();
        
        // Application Services (Transient - stateless)
        services.AddTransient<ICSharpParserService, CSharpParserService>();
        services.AddTransient<IGenerateCrudService, GenerateCrudService>();
        services.AddTransient<IParseProjectService, ParseProjectService>();
        
        // Infrastructure Services (Singleton)
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ITextParsingService, TextParsingService>();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Résoudre MainWindow via DI
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

### Lifetime Scopes

| Lifetime | Usage | Example |
|----------|-------|---------|
| **Singleton** | Shared state, expensive to create | RepositoryService, GitService, FileDialogService |
| **Transient** | Lightweight, stateless | ViewModels, Views, Helpers, Parsers |
| **Scoped** | N/A (WPF n'a pas de "request scope") | - |

---

## 📋 SOLID Principles Application

### Single Responsibility Principle (SRP)

**Une classe = Une raison de changer**

✅ **Bon**:
```csharp
// CRUDGeneratorViewModel: Gestion UI état CRUD generation
// CRUDGeneratorHelper: Logique métier CRUD generation
// GenerateCrudService: Orchestration fichiers/templates
// CSharpParserService: Parsing C# files
```

❌ **Mauvais**:
```csharp
// CRUDGeneratorUC.xaml.cs: 
// - UI événements
// - Parsing C#
// - Génération fichiers
// - Validation
// - File I/O
```

---

### Open/Closed Principle (OCP)

**Ouvert extension, fermé modification**

✅ **Bon**:
```csharp
public interface ICodeGenerator
{
    Task<string> GenerateAsync(Entity entity);
}

public class CSharpCodeGenerator : ICodeGenerator { }
public class TypeScriptCodeGenerator : ICodeGenerator { }
```

❌ **Mauvais**:
```csharp
public class CodeGenerator
{
    public string Generate(Entity entity, string language)
    {
        if (language == "CSharp") { /* ... */ }
        else if (language == "TypeScript") { /* ... */ }
        // Modification requise pour nouveau langage
    }
}
```

---

### Liskov Substitution Principle (LSP)

**Sous-types substituables**

✅ **Bon**:
```csharp
public interface IDialogService
{
    Task<bool> ConfirmAsync(string message, string title);
}

// Implémentation WPF
public class WpfDialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string message, string title)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo) 
            == MessageBoxResult.Yes;
    }
}

// Implémentation Tests
public class MockDialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string message, string title)
    {
        return Task.FromResult(true); // Always confirm in tests
    }
}
```

---

### Interface Segregation Principle (ISP)

**Interfaces petites et spécifiques**

✅ **Bon**:
```csharp
public interface IFileDialogService
{
    Task<string?> OpenFileDialogAsync(string filter);
    Task<string?> SaveFileDialogAsync(string filter);
    Task<string?> OpenFolderDialogAsync();
}

public interface IDialogService
{
    Task ShowErrorAsync(string message);
    Task ShowSuccessAsync(string message);
    Task<bool> ConfirmAsync(string message, string title);
}
```

❌ **Mauvais**:
```csharp
public interface IUiService
{
    Task<string?> OpenFileDialogAsync(string filter);
    Task ShowErrorAsync(string message);
    Task UpdateProgressBar(int value);
    Task ShowNotification(string text);
    Task<bool> ConfirmAsync(string message);
    // Too many responsibilities
}
```

---

### Dependency Inversion Principle (DIP)

**Dépendre d'abstractions, pas d'implémentations**

✅ **Bon**:
```csharp
public class CRUDGeneratorViewModel
{
    private readonly IGenerateCrudService crudService; // Abstraction
    private readonly IDialogService dialogService;     // Abstraction
    
    public CRUDGeneratorViewModel(
        IGenerateCrudService crudService,
        IDialogService dialogService
    )
    {
        this.crudService = crudService;
        this.dialogService = dialogService;
    }
}
```

❌ **Mauvais**:
```csharp
public class CRUDGeneratorViewModel
{
    private readonly GenerateCrudService crudService; // Concrete class
    
    public CRUDGeneratorViewModel()
    {
        this.crudService = new GenerateCrudService(); // Hard dependency
    }
}
```

---

## 🎯 KISS Principle (Keep It Simple, Stupid)

### Simplifier Complexité

**Avant (Complexe)**:
```csharp
private void ModifyDto_SelectionChange(object sender, SelectionChangedEventArgs e)
{
    if (e.AddedItems.Count > 0)
    {
        var item = e.AddedItems[0];
        if (item is DtoEntity dto)
        {
            if (dto != null && !string.IsNullOrEmpty(dto.FilePath))
            {
                // 50 lignes parsing...
            }
        }
    }
}
```

**Après (Simple)**:
```csharp
[ObservableProperty]
private DtoEntity selectedDto;

partial void OnSelectedDtoChanged(DtoEntity value)
{
    if (value != null)
    {
        LoadDtoDetailsAsync(value).FireAndForget();
    }
}
```

---

## 🔁 DRY Principle (Don't Repeat Yourself)

### Éliminer Duplication

**Avant (Répété)**:
```csharp
// Dans CRUDGeneratorUC
var dialog = new OpenFileDialog();
if (dialog.ShowDialog() == true)
{
    filePath = dialog.FileName;
}

// Dans OptionGeneratorUC
var dialog = new OpenFileDialog();
if (dialog.ShowDialog() == true)
{
    filePath = dialog.FileName;
}

// Dans ModifyProjectUC
var folderDialog = new FolderBrowserDialog();
if (folderDialog.ShowDialog() == DialogResult.OK)
{
    folderPath = folderDialog.SelectedPath;
}
```

**Après (Service)**:
```csharp
// Dans ViewModel
var filePath = await fileDialogService.OpenFileDialogAsync("C# files|*.cs");
if (filePath != null)
{
    // Use filePath
}
```

---

## 🚫 YAGNI Principle (You Aren't Gonna Need It)

### Ne pas sur-engineer

**À Éviter**:
```csharp
// Generic super-flexible framework (YAGNI si pas besoin)
public interface ICodeGenerator<TEntity, TOptions, TResult>
    where TEntity : IEntity
    where TOptions : IGenerationOptions
    where TResult : IGenerationResult
{
    Task<TResult> GenerateAsync(TEntity entity, TOptions options, CancellationToken ct);
}
```

**Préférer (Simple et suffisant)**:
```csharp
public interface ICodeGenerator
{
    Task<GenerationResult> GenerateAsync(Entity entity);
}
```

---

## ✅ Testing Strategy

### ViewModels (100% Testable)

```csharp
[Fact]
public async Task GenerateCommand_WithValidDto_GeneratesSuccessfully()
{
    // Arrange
    var mockHelper = new Mock<ICRUDGeneratorHelper>();
    var mockDialog = new Mock<IDialogService>();
    
    mockHelper
        .Setup(h => h.GenerateCRUDAsync(It.IsAny<DtoEntity>()))
        .ReturnsAsync(new GenerationResult { Success = true });
    
    var viewModel = new CRUDGeneratorViewModel(
        mockHelper.Object,
        mockDialog.Object
    );
    
    viewModel.SelectedDto = new DtoEntity { Name = "User" };
    
    // Act
    await viewModel.GenerateCommand.ExecuteAsync(null);
    
    // Assert
    mockHelper.Verify(h => h.GenerateCRUDAsync(It.IsAny<DtoEntity>()), Times.Once);
    mockDialog.Verify(d => d.ShowSuccessAsync(It.IsAny<string>()), Times.Once);
}
```

---

## 📚 Références

- [Clean Architecture (Robert C. Martin)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MVVM Pattern (Microsoft)](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Dependency Injection (.NET)](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

---

*Document créé le 22 janvier 2026*  
*Version 1.0*
