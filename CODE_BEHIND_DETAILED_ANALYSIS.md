# 🔍 Analyse Détaillée Code-Behind - Violations MVVM

**Date**: 22 janvier 2026  
**Objectif**: Documenter chaque violation MVVM pour guider la refactorisation

---

## 📁 MainWindow.xaml.cs (534 lignes)

### ❌ Constructor - Injection Manuelle (lignes 30-80)
```csharp
public MainWindow(
    // 14 services injectés
    IRepositoryService repositoryService,
    IGitService gitService,
    IRetrieveVersionService retrieveVersionService,
    // ... 11 autres
)
{
    InitializeComponent();
    
    // ❌ VIOLATION: Injection manuelle dans UserControls
    CreateVersionAndOption.Inject(this.repositoryService, gitService, ...);
    ModifyProject.Inject(this.repositoryService, gitService, ...);
    
    // ❌ VIOLATION: Event subscription
    CreateVersionAndOption.OnChange += (s, e) => InitSettings();
    ModifyProject.Loaded += (s, e) => InitSettings();
}
```

**Problème**: UserControls devraient être injectés avec leurs ViewModels par DI Container

---

### ❌ Event Handlers avec Logique Métier

#### 1. Create_Click (ligne 347)
```csharp
private void Create_Click(object sender, RoutedEventArgs e)
{
    // ❌ 20 lignes de logique métier
    if (txtProjectName.Text == "") { MessageBox.Show(...); return; }
    if (CreateProject.ProjectCsProj == "") { MessageBox.Show(...); return; }
    // ... validation complexe
    await Create_Run();
}
```
**Devrait être**: `CreateProjectCommand` dans `MainWindowViewModel`

---

#### 2. Create_Run (ligne 368) - 50 lignes!
```csharp
private async Task Create_Run()
{
    try
    {
        // ❌ Logique complexe orchestration
        await Task.Run(() =>
        {
            generateProjectService.CopyProject(...);
            generateProjectService.InitSolution(...);
            parseProjectService.CreateDirectoryAndParseProject(...);
            // ... 30+ lignes
        });
    }
    catch { }
}
```
**Devrait être**: `MainWindowViewModel.CreateProjectAsync()`

---

#### 3. CreateProjectRootFolderBrowse_Click (ligne 322)
```csharp
private void CreateProjectRootFolderBrowse_Click(...)
{
    // ❌ File dialog logic
    FolderBrowserDialog dialog = new();
    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    {
        CreateProject.ProjectRootFolder = dialog.SelectedPath;
    }
}
```
**Devrait être**: `BrowseProjectFolderCommand` utilisant `IFileDialogService`

---

#### 4. ExportConfigButton_Click (ligne 384)
```csharp
private async void ExportConfigButton_Click(...)
{
    // ❌ Export logic
    SaveFileDialog saveFileDialog = new();
    // ... configuration export
    await exportSettingsService.ExportAsync(...);
}
```
**Devrait être**: `ExportConfigCommand`

---

#### 5. CopyConsoleContentToClipboard_Click (ligne 442)
```csharp
private void CopyConsoleContentToClipboard_Click(...)
{
    Clipboard.SetText(ConsoleTextBox.Text);
}
```
**Devrait être**: `CopyConsoleCommand` (binding sur Text property)

---

#### 6. btnFileGenerator_Generate_Click (ligne 421)
```csharp
private async void btnFileGenerator_Generate_Click(...)
{
    // ❌ Grosse logique métier (20+ lignes)
    this.generateProjectService.GenerateNewFile(...);
    this.parseProjectService.UpdateDisplayFile(...);
}
```
**Devrait être**: `GenerateFileCommand`

---

### ❌ Méthodes Privées avec Logique Métier

#### 1. InitSettings (ligne 481) - 35 lignes
```csharp
private async Task InitSettings()
{
    // ❌ Initialization logic
    await retrieveVersionService.RetrieveAsync(...);
    CreateVersionAndOption.SetCurrentVersion(...);
    // ... 30 lignes
}
```
**Devrait être**: `MainWindowViewModel.InitializeAsync()`

---

#### 2. EnsureValidRepositoriesConfiguration (ligne 218) - 100 lignes!
```csharp
private bool EnsureValidRepositoriesConfiguration()
{
    // ❌ Validation complexe repositories
    if (repositoryFolderService.GetMainRepository() == null) { ... }
    if (repositoryGitService.GetMainRepository() == null) { ... }
    // ... énorme logique validation
    return true;
}
```
**Devrait être**: `MainWindowViewModel.ValidateRepositoriesAsync()`

---

### 📊 Résumé MainWindow

| Métrique | Valeur |
|----------|--------|
| **Lignes totales** | 534 |
| **Event handlers** | 8 |
| **Méthodes privées métier** | 10+ |
| **Lignes à déplacer** | ~480 |
| **Cible finale** | ~50 lignes |

---

## 📁 CRUDGeneratorUC.xaml.cs (706 lignes)

### ❌ Méthode Inject (ligne 61)
```csharp
public void Inject(
    CSharpParserService cSharpParserService,
    ZipParserService zipParserService,
    GenerateCrudService generateCrudService,
    GenerateTeamConfigService generateTeamConfigService,
    ParseProjectService parseProjectService
)
{
    this.cSharpParserService = cSharpParserService;
    this.zipParserService = zipParserService;
    // ... 5 services
}
```
**Problème**: Service Locator anti-pattern

**Devrait être**:
```csharp
public CRUDGeneratorUC(CRUDGeneratorViewModel viewModel)
{
    InitializeComponent();
    DataContext = viewModel;
}
```

---

### ❌ Event Handlers

#### 1. Generate_Click (ligne 101) - 80 lignes!
```csharp
private async void Generate_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // ❌ Énorme logique génération
        dtoEntity = ParseDtoFile();
        await Task.Run(() =>
        {
            generateCrudService.Generate(
                dtoEntity,
                entitySingular,
                // ... 10 paramètres
            );
        });
        // ... 50 lignes
    }
    catch { }
}
```
**Devrait être**: `GenerateCRUDCommand` dans ViewModel

---

#### 2. ModifyDto_SelectionChange (ligne 127) - 30 lignes
```csharp
private void ModifyDto_SelectionChange(...)
{
    if (ModifyDto.SelectedItem is DtoEntity selectedDto)
    {
        // ❌ Logique parsing + UI update
        bool success = ParseDtoFile();
        ModifyEntitySingular.Text = selectedDto.Name;
        // ... update 10+ contrôles
    }
}
```
**Devrait être**: 
```csharp
[ObservableProperty]
private DtoEntity selectedDto;

partial void OnSelectedDtoChanged(DtoEntity value)
{
    LoadDtoDetailsAsync(value);
}
```

---

#### 3. RefreshDtoList_Click (ligne 193)
```csharp
private void RefreshDtoList_Click(...)
{
    ListDtoFiles();
}
```
**Devrait être**: `RefreshDtoListCommand`

---

#### 4. DeleteLastGeneration_Click (ligne 198) - 40 lignes
```csharp
private void DeleteLastGeneration_Click(...)
{
    // ❌ Logique suppression complexe
    var result = MessageBox.Show("Are you sure?", ...);
    if (result == MessageBoxResult.Yes)
    {
        zipParserService.Delete(...);
        // ... 30 lignes
    }
}
```
**Devrait être**: `DeleteLastGenerationCommand` avec confirmation dialog

---

#### 5. DeleteBIAToolkitAnnotations_Click (ligne 243) - 70 lignes!
```csharp
private void DeleteBIAToolkitAnnotations_Click(...)
{
    // ❌ Énorme logique manipulation fichiers
    string[] files = Directory.GetFiles(...);
    foreach (var file in files)
    {
        string content = File.ReadAllText(file);
        // ... regex replacements
        File.WriteAllText(file, content);
    }
}
```
**Devrait être**: `DeleteAnnotationsCommand` + Service

---

#### 6. BiaFront_SelectionChanged (ligne 696)
```csharp
private void BiaFront_SelectionChanged(...)
{
    ParseFrontDomains();
}
```
**Devrait être**: Observable Property reaction

---

### ❌ Méthodes Privées Métier

#### 1. ListDtoFiles (ligne 316) - 80 lignes
```csharp
private void ListDtoFiles()
{
    // ❌ File system logic + parsing
    string[] files = Directory.GetFiles(dtoPath, "*.cs");
    var dtoList = new List<DtoEntity>();
    
    foreach (var file in files)
    {
        var dto = cSharpParserService.ParseDto(file);
        dtoList.Add(dto);
    }
    
    ModifyDto.ItemsSource = dtoList;
}
```
**Devrait être**: `CRUDGeneratorViewModel.LoadDtoFilesAsync()`

---

#### 2. ParseDtoFile (ligne 401) - 120 lignes!
```csharp
private bool ParseDtoFile()
{
    // ❌ Énorme parsing logic
    try
    {
        string fileContent = File.ReadAllText(filePath);
        var ast = CSharpSyntaxTree.ParseText(fileContent);
        // ... 100+ lignes parsing
        return true;
    }
    catch
    {
        return false;
    }
}
```
**Devrait être**: Dans Service ou Helper, appelé par ViewModel

---

#### 3. ParseFrontDomains (ligne 524) - 100 lignes
```csharp
private void ParseFrontDomains()
{
    // ❌ Front-end file parsing
    string frontPath = Path.Combine(...);
    string[] domainFiles = Directory.GetFiles(frontPath);
    // ... 90 lignes parsing Angular/React
}
```
**Devrait être**: `LoadFrontDomainsAsync()` dans ViewModel

---

### 📊 Résumé CRUDGeneratorUC

| Métrique | Valeur |
|----------|--------|
| **Lignes totales** | 706 |
| **Event handlers** | 6+ |
| **Méthodes privées métier** | 8+ |
| **Lignes Helper** | 276 (déjà créé) |
| **Lignes à déplacer VM** | ~400 |
| **Cible finale** | ~30 lignes |

---

## 📁 OptionGeneratorUC.xaml.cs (488 lignes)

### ❌ Méthode Inject (ligne 67)
```csharp
public void Inject(
    CSharpParserService cSharpParserService,
    GenerateOptionService generateOptionService,
    ParseProjectService parseProjectService,
    ZipParserService zipParserService
)
{
    this.cSharpParserService = cSharpParserService;
    // ... 4 services
}
```
**Même problème que CRUDGeneratorUC**

---

### ❌ Event Handlers (similaires à CRUDGenerator)

1. **Generate_Click** (ligne 97) - 60 lignes
2. **DeleteLastGeneration_Click** (ligne 160)
3. **RefreshEntitiesList_Click** (ligne 165)
4. **ModifyEntity_SelectionChange** (ligne 133) - 25 lignes
5. **DeleteBIAToolkitAnnotations_Click** (ligne 175) - 60 lignes
6. **BIAFront_SelectionChanged** (ligne 479)

**Tous doivent devenir**: Commands dans `OptionGeneratorViewModel`

---

### ❌ Méthodes Privées Métier

1. **ListEntityFiles** (ligne 239) - 70 lignes
2. **ParseEntityFile** (ligne 312) - 100 lignes
3. **ParseFrontDomains** (ligne 415) - 60 lignes

**Tous doivent aller dans**: ViewModel

---

### 📊 Résumé OptionGeneratorUC

| Métrique | Valeur |
|----------|--------|
| **Lignes totales** | 488 |
| **Event handlers** | 6+ |
| **Lignes Helper** | 235 (déjà créé) |
| **Lignes à déplacer VM** | ~230 |
| **Cible finale** | ~30 lignes |

---

## 📁 DtoGeneratorUC.xaml.cs (199 lignes)

### ❌ Méthode Inject (ligne 50)
```csharp
public void Inject(
    CSharpParserService cSharpParserService,
    GenerateDtoService generateDtoService
)
{
    this.cSharpParserService = cSharpParserService;
    this.generateDtoService = generateDtoService;
}
```

---

### ❌ Event Handlers

1. **MappingOptionId_SelectionChanged** (ligne 162)
2. **EntitiesComboBox_SelectionChanged** (ligne 170)
3. **Generate_Click** (implied)

---

### 📊 Résumé DtoGeneratorUC

**Note**: Ce UC est déjà bien refactorisé avec DtoGeneratorHelper (180 lignes)

| Métrique | Valeur |
|----------|--------|
| **Lignes totales** | 199 |
| **Lignes Helper** | 180 (déjà créé) |
| **Cible finale** | ~25 lignes |

---

## 📁 ModifyProjectUC.xaml.cs (~400 lignes)

### ❌ Méthode Inject (ligne 47)
```csharp
public void Inject(
    IRepositoryService repositoryService,
    IGitService gitService,
    IRetrieveVersionService retrieveVersionService,
    ParseProjectService parseProjectService
)
{
    // 4 services
}
```

---

### ❌ Event Handlers

1. **BrowseFolder_Click**
2. **AddProject_Click**
3. **DeleteProject_Click**
4. **OpenProject_Click**
5. **ParseProject_Click**

**Tous doivent devenir**: Commands dans `ModifyProjectViewModel`

---

### 📊 Résumé ModifyProjectUC

| Métrique | Valeur |
|----------|--------|
| **Lignes totales** | ~400 |
| **Event handlers** | 5+ |
| **Cible finale** | ~30 lignes |

---

## 📁 VersionAndOptionUserControl.xaml.cs (233 lignes)

### ❌ Méthode Inject (ligne 42)
```csharp
public void Inject(
    IRepositoryService repositoryService,
    IGitService gitService,
    IRetrieveVersionService retrieveVersionService
)
{
    // 3 services
}
```

---

### ❌ Event Handlers

1. **FrameworkVersion_SelectionChanged** (ligne 213)
```csharp
private void FrameworkVersion_SelectionChanged(...)
{
    // ❌ Logique update autres contrôles
    UpdateCFVersionComboBox();
    RaiseOnChange();
}
```

2. **CFVersion_SelectionChanged** (ligne 227)
```csharp
private void CFVersion_SelectionChanged(...)
{
    RaiseOnChange();
}
```

**Devrait être**: Observable Properties avec reactions automatiques

---

### 📊 Résumé VersionAndOptionUserControl

| Métrique | Valeur |
|----------|--------|
| **Lignes totales** | 233 |
| **Event handlers** | 2 |
| **Cible finale** | ~30 lignes |

---

## 📊 SYNTHÈSE GLOBALE

### Violations Par Type

| Violation | Count | Fichiers |
|-----------|-------|----------|
| **Méthodes Inject()** | 5 | Tous les UserControls sauf Dialogs |
| **Event Handlers _Click** | 20+ | MainWindow, tous UCs |
| **Event Handlers _SelectionChanged** | 10+ | Tous les UCs |
| **Méthodes privées métier** | 30+ | Partout |
| **File I/O dans code-behind** | 15+ | CRUDGenerator, OptionGenerator |
| **MessageBox.Show direct** | 10+ | MainWindow, tous UCs |

---

### Effort de Refactorisation Estimé

| Fichier | Lignes Actuelles | Lignes Cible | À Déplacer | Effort |
|---------|------------------|--------------|------------|--------|
| MainWindow.xaml.cs | 534 | 50 | 484 | 1j |
| CRUDGeneratorUC.xaml.cs | 706 | 30 | 676 | 1j |
| OptionGeneratorUC.xaml.cs | 488 | 30 | 458 | 1j |
| DtoGeneratorUC.xaml.cs | 199 | 25 | 174 | 0.5j |
| ModifyProjectUC.xaml.cs | 400 | 30 | 370 | 0.5j |
| VersionAndOptionUC.xaml.cs | 233 | 30 | 203 | 0.5j |
| **TOTAL** | **2,560** | **195** | **2,365** | **4.5j** |

---

## ✅ Pattern Cible Par Fichier

### Exemple: CRUDGeneratorUC.xaml.cs APRÈS refactorisation

```csharp
// Code-Behind (30 lignes)
public partial class CRUDGeneratorUC : UserControl
{
    public CRUDGeneratorUC(CRUDGeneratorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

```csharp
// ViewModel (300+ lignes)
public partial class CRUDGeneratorViewModel : ObservableObject
{
    private readonly CRUDGeneratorHelper helper;
    private readonly IDialogService dialogService;
    
    public CRUDGeneratorViewModel(
        CRUDGeneratorHelper helper,
        IDialogService dialogService
    )
    {
        this.helper = helper;
        this.dialogService = dialogService;
    }
    
    [ObservableProperty]
    private DtoEntity selectedDto;
    
    [ObservableProperty]
    private ObservableCollection<DtoEntity> dtoList;
    
    partial void OnSelectedDtoChanged(DtoEntity value)
    {
        LoadDtoDetailsAsync(value).FireAndForget();
    }
    
    [RelayCommand]
    private async Task GenerateAsync()
    {
        try
        {
            IsGenerating = true;
            await helper.GenerateCRUDAsync(SelectedDto);
        }
        finally
        {
            IsGenerating = false;
        }
    }
    
    [RelayCommand]
    private async Task RefreshDtoListAsync()
    {
        DtoList = await helper.LoadDtoFilesAsync();
    }
    
    [RelayCommand]
    private async Task DeleteLastGenerationAsync()
    {
        var confirmed = await dialogService.ConfirmAsync(
            "Are you sure?", 
            "Delete Generation"
        );
        
        if (confirmed)
        {
            await helper.DeleteLastGenerationAsync();
        }
    }
}
```

---

*Document créé le 22 janvier 2026*  
*Prêt pour exécution Phases 4-6*
