# Guide des Patterns de Refactoring - Code-Behind → ViewModel

**Date**: 19 janvier 2026  
**Objectif**: Fournir des patterns réutilisables pour convertir les code-behind en ViewModels

---

## 🔄 Pattern 1: Event Handler → RelayCommand

### ❌ Avant (Anti-pattern)

```xaml
<Button Content="Submit" Click="SubmitButton_Click" />
<TextBox Text="{Binding EntityName}" TextChanged="EntityName_TextChanged" />
```

```csharp
public partial class MyUserControl : UserControl
{
    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(vm.EntityName))
        {
            MessageBox.Show("Enter entity name");
            return;
        }
        
        service.Submit(vm.EntityName);
        vm.IsSubmitted = true;
    }
    
    private void EntityName_TextChanged(object sender, TextChangedEventArgs e)
    {
        vm.IsNameValid = !string.IsNullOrEmpty(vm.EntityName);
    }
}
```

### ✅ Après (Pattern correct)

```csharp
// ViewModel
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty] string entityName;
    [ObservableProperty] bool isSubmitted;
    [ObservableProperty] bool isNameValid;
    
    public RelayCommand SubmitCommand { get; }
    
    private readonly IMyService service;
    
    public MyViewModel(IMyService service)
    {
        this.service = service;
        SubmitCommand = new RelayCommand(Submit, CanSubmit);
    }
    
    partial void OnEntityNameChanged(string value)
    {
        // Auto-appelé quand EntityName change
        IsNameValid = !string.IsNullOrEmpty(value);
    }
    
    private void Submit()
    {
        service.Submit(EntityName);
        IsSubmitted = true;
    }
    
    private bool CanSubmit()
    {
        return !string.IsNullOrEmpty(EntityName);
    }
}
```

```xaml
<!-- UserControl -->
<StackPanel>
    <TextBox Text="{Binding EntityName, UpdateSourceTrigger=PropertyChanged}" />
    <TextBlock Text="{Binding IsNameValid, StringFormat='Valid: {0}'}" />
    <Button Content="Submit" 
            Command="{Binding SubmitCommand}" 
            IsEnabled="{Binding CanSubmit}" />
</StackPanel>
```

### 📋 Checklist

- [ ] Command défini dans ViewModel avec `AsyncRelayCommand` ou `RelayCommand`
- [ ] `OnPropertyNameChanged()` pour les actions lors du changement de propriété
- [ ] `CanExecute` (paramètre du RelayCommand) pour validation
- [ ] XAML utilise `Command="{Binding ...Command}"`
- [ ] Pas de code-behind (ou minimal)

---

## 🔄 Pattern 2: File/Folder Browse Dialog

### ❌ Avant (Anti-pattern)

```csharp
private void BrowseButton_Click(object sender, RoutedEventArgs e)
{
    var path = FileDialog.BrowseFolder(
        vm.SelectedPath, 
        "Choose folder");
    
    if (!string.IsNullOrEmpty(path))
    {
        vm.SelectedPath = path;
    }
}
```

### ✅ Après (Pattern correct)

```csharp
// Interface (Infrastructure)
public interface IFileDialogService
{
    string BrowseFolder(string initialPath, string title);
    string BrowseFile(string filter);
    string SaveFile(string fileName, string filter);
}

// Implémentation
public class FileDialogService : IFileDialogService
{
    public string BrowseFolder(string initialPath, string title)
    {
        try
        {
            return FileDialog.BrowseFolder(initialPath, title);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Browse folder failed: {ex.Message}");
            return null;
        }
    }
}

// ViewModel
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty] string selectedPath;
    
    public RelayCommand BrowseFolderCommand { get; }
    
    private readonly IFileDialogService fileDialogService;
    
    public MyViewModel(IFileDialogService fileDialogService)
    {
        this.fileDialogService = fileDialogService;
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
    }
    
    private void BrowseFolder()
    {
        var path = fileDialogService.BrowseFolder(
            SelectedPath, 
            "Choose source folder");
        
        if (!string.IsNullOrEmpty(path))
        {
            SelectedPath = path;
        }
    }
}
```

```xaml
<Button Content="Browse..." Command="{Binding BrowseFolderCommand}" />
<TextBlock Text="{Binding SelectedPath}" />
```

### 📋 Checklist

- [ ] `IFileDialogService` créée dans Infrastructure
- [ ] Service enregistré dans DI
- [ ] ViewModel injecte l'interface (pas la concrètion)
- [ ] Command expose l'action dans ViewModel
- [ ] Pas de référence à FileDialog dans ViewModel

---

## 🔄 Pattern 3: Async Operations avec Progress

### ❌ Avant (Anti-pattern)

```csharp
private async void GenerateCrud_Click(object sender, RoutedEventArgs e)
{
    try
    {
        LoadingSpinner.Visibility = Visibility.Visible;
        StatusLabel.Content = "Generating...";
        
        var result = await crudService.GenerateAsync(vm.Configuration);
        
        StatusLabel.Content = $"Generated {result.Count} files";
        LoadingSpinner.Visibility = Visibility.Hidden;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error: {ex.Message}");
        LoadingSpinner.Visibility = Visibility.Hidden;
    }
}
```

### ✅ Après (Pattern correct)

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty] bool isLoading;
    [ObservableProperty] string status = "Ready";
    [ObservableProperty] int generatedCount;
    
    public AsyncRelayCommand GenerateCrudCommand { get; }
    
    private readonly GenerateCrudService crudService;
    
    public MyViewModel(GenerateCrudService crudService)
    {
        this.crudService = crudService;
        GenerateCrudCommand = new AsyncRelayCommand(
            GenerateCrudAsync, 
            CanGenerateCrud);
    }
    
    private async Task GenerateCrudAsync()
    {
        try
        {
            IsLoading = true;
            Status = "Generating...";
            GeneratedCount = 0;
            
            var result = await crudService.GenerateAsync(Configuration);
            
            GeneratedCount = result.Count;
            Status = $"✅ Generated {result.Count} files";
        }
        catch (OperationCanceledException)
        {
            Status = "⚠️ Cancelled by user";
        }
        catch (Exception ex)
        {
            Status = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private bool CanGenerateCrud()
    {
        return !IsLoading && Configuration != null;
    }
}
```

```xaml
<StackPanel>
    <ProgressBar IsIndeterminate="True" 
                 Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}" />
    
    <TextBlock Text="{Binding Status}" Foreground="Gray" />
    
    <Button Content="Generate CRUD" 
            Command="{Binding GenerateCrudCommand}"
            IsEnabled="{Binding GenerateCrudCommand.IsRunning, Converter={StaticResource InverseBooleanConverter}}" />
    
    <TextBlock Text="{Binding GeneratedCount, StringFormat='Files: {0}'}" />
</StackPanel>
```

### 📋 Checklist

- [ ] `AsyncRelayCommand` pour opérations asynchrones
- [ ] `IsLoading` booléen pour gérer UI (button disabled, spinner visible)
- [ ] `Status` string pour messages utilisateur
- [ ] Try-catch avec gestion des différents cas d'erreur
- [ ] Command binding pour exécution, pas Click handler
- [ ] `IsRunning` du Command pour désactiver le bouton

---

## 🔄 Pattern 4: Collection Management (CRUD List)

### ❌ Avant (Anti-pattern)

```csharp
private void AddButton_Click(object sender, RoutedEventArgs e)
{
    var item = vm.CreateNewItem();
    vm.Items.Add(item);
    ItemsListBox.SelectedItem = item;
}

private void EditButton_Click(object sender, RoutedEventArgs e)
{
    if (ItemsListBox.SelectedItem is MyItem item)
    {
        // Edit logic...
        ItemsListBox.Items.Refresh();
    }
}

private void DeleteButton_Click(object sender, RoutedEventArgs e)
{
    if (ItemsListBox.SelectedItem is MyItem item)
    {
        vm.Items.Remove(item);
    }
}
```

### ✅ Après (Pattern correct)

```csharp
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty] ObservableCollection<MyItem> items = new();
    [ObservableProperty] MyItem selectedItem;
    
    public RelayCommand AddItemCommand { get; }
    public RelayCommand<MyItem> EditItemCommand { get; }
    public RelayCommand<MyItem> DeleteItemCommand { get; }
    
    private readonly IMyService service;
    
    public MyViewModel(IMyService service)
    {
        this.service = service;
        
        AddItemCommand = new RelayCommand(AddItem);
        EditItemCommand = new RelayCommand<MyItem>(EditItem);
        DeleteItemCommand = new RelayCommand<MyItem>(DeleteItem);
    }
    
    private void AddItem()
    {
        var item = service.CreateNewItem();
        Items.Add(item);
        SelectedItem = item;
    }
    
    private void EditItem(MyItem item)
    {
        if (item != null)
        {
            // Edit logic
            service.Update(item);
            // ObservableCollection handle refresh automatically
        }
    }
    
    private void DeleteItem(MyItem item)
    {
        if (item != null && Items.Remove(item))
        {
            SelectedItem = Items.FirstOrDefault();
        }
    }
}
```

```xaml
<ListBox ItemsSource="{Binding Items}" 
         SelectedItem="{Binding SelectedItem}" />

<StackPanel>
    <Button Content="Add" Command="{Binding AddItemCommand}" />
    <Button Content="Edit" 
            Command="{Binding EditItemCommand}" 
            CommandParameter="{Binding SelectedItem}" />
    <Button Content="Delete" 
            Command="{Binding DeleteItemCommand}" 
            CommandParameter="{Binding SelectedItem}" />
</StackPanel>
```

### 📋 Checklist

- [ ] `ObservableCollection<T>` pour listes
- [ ] Commands pour Add/Edit/Delete
- [ ] `CommandParameter` pour passer l'item sélectionné
- [ ] Service gère la persistence
- [ ] Pas de `.Refresh()` ou `.Clear()` dans code-behind
- [ ] SelectedItem binding à deux sens

---

## 🔄 Pattern 5: Validation avec Feedback

### ❌ Avant (Anti-pattern)

```csharp
private void EntityName_TextChanged(object sender, TextChangedEventArgs e)
{
    var text = entityNameTextBox.Text;
    
    if (string.IsNullOrEmpty(text))
    {
        validationLabel.Text = "Name is required";
        validationLabel.Foreground = Brushes.Red;
        submitButton.IsEnabled = false;
    }
    else if (text.Length > 100)
    {
        validationLabel.Text = "Name too long (max 100 chars)";
        validationLabel.Foreground = Brushes.Orange;
        submitButton.IsEnabled = false;
    }
    else if (service.NameExists(text))
    {
        validationLabel.Text = "This name already exists";
        validationLabel.Foreground = Brushes.Red;
        submitButton.IsEnabled = false;
    }
    else
    {
        validationLabel.Text = "✓ Name valid";
        validationLabel.Foreground = Brushes.Green;
        submitButton.IsEnabled = true;
    }
}
```

### ✅ Après (Pattern correct)

```csharp
// Value Object pour validation result
public record ValidationResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; }
    public SeverityLevel Severity { get; init; }
}

public enum SeverityLevel
{
    Info,
    Warning,
    Error
}

// Service de validation
public interface IValidationService
{
    ValidationResult ValidateEntityName(string name);
}

public class ValidationService : IValidationService
{
    private readonly IMyService service;
    
    public ValidationResult ValidateEntityName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return new() 
            { 
                IsValid = false,
                Message = "Name is required",
                Severity = SeverityLevel.Error 
            };
        
        if (name.Length > 100)
            return new() 
            { 
                IsValid = false,
                Message = "Name too long (max 100 chars)",
                Severity = SeverityLevel.Warning 
            };
        
        if (service.NameExists(name))
            return new() 
            { 
                IsValid = false,
                Message = "This name already exists",
                Severity = SeverityLevel.Error 
            };
        
        return new() 
        { 
            IsValid = true,
            Message = "✓ Name valid",
            Severity = SeverityLevel.Info 
        };
    }
}

// ViewModel
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty] string entityName;
    [ObservableProperty] ValidationResult nameValidation;
    
    public RelayCommand SubmitCommand { get; }
    
    private readonly IValidationService validationService;
    
    public MyViewModel(IValidationService validationService)
    {
        this.validationService = validationService;
        SubmitCommand = new RelayCommand(Submit, CanSubmit);
    }
    
    partial void OnEntityNameChanged(string value)
    {
        NameValidation = validationService.ValidateEntityName(value);
        // RelayCommand.CanExecuteChanged déclenché automatiquement
    }
    
    private void Submit()
    {
        // Submit logic
    }
    
    private bool CanSubmit()
    {
        return NameValidation?.IsValid == true;
    }
}
```

```xaml
<StackPanel>
    <TextBox Text="{Binding EntityName, UpdateSourceTrigger=PropertyChanged}"
             Watermark="Entity name" />
    
    <TextBlock Text="{Binding NameValidation.Message}"
               Foreground="{Binding NameValidation.Severity, Converter={StaticResource SeverityToBrushConverter}}" />
    
    <Button Content="Submit"
            Command="{Binding SubmitCommand}" />
</StackPanel>
```

### 📋 Checklist

- [ ] `ValidationResult` value object pour encapsuler validation
- [ ] `IValidationService` avec logique métier
- [ ] `partial void OnPropertyChanged()` pour trigger validation
- [ ] Message + Severity dans résultat
- [ ] Converter pour afficher couleur basée sur Severity
- [ ] Pas de validation dans code-behind
- [ ] Command.CanExecute basé sur IsValid

---

## 🔄 Pattern 6: Dialog Communication avec Messages

### ❌ Avant (Anti-pattern)

```csharp
public partial class RepositoryFormUC : Window
{
    public RepositoryFormUC(RepositoryViewModel repository)
    {
        DataContext = new RepositoryFormViewModel(repository);
        InitializeComponent();
    }
    
    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

// Parent window
private void EditRepository_Click(object sender, RoutedEventArgs e)
{
    var dialog = new RepositoryFormUC(selectedRepository);
    if (dialog.ShowDialog() == true)
    {
        // Update UI
        RefreshRepositoryList();
    }
}
```

### ✅ Après (Pattern correct)

```csharp
// Message
public class DialogClosedMessage
{
    public DialogClosedMessage(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }
    
    public bool IsSuccess { get; }
}

// Dialog ViewModel
public partial class RepositoryFormViewModel : ObservableObject
{
    [ObservableProperty] RepositoryViewModel repository;
    
    public RelayCommand SubmitCommand { get; }
    public RelayCommand CancelCommand { get; }
    
    private readonly IMessenger messenger;
    
    public RepositoryFormViewModel(
        RepositoryViewModel repository,
        IMessenger messenger)
    {
        this.repository = repository;
        this.messenger = messenger;
        
        SubmitCommand = new RelayCommand(Submit);
        CancelCommand = new RelayCommand(Cancel);
    }
    
    private void Submit()
    {
        messenger.Send(new DialogClosedMessage(true));
    }
    
    private void Cancel()
    {
        messenger.Send(new DialogClosedMessage(false));
    }
}

// Dialog code-behind (minimal)
public partial class RepositoryFormUC : Window
{
    public RepositoryFormUC(RepositoryViewModel repository, IMessenger messenger)
    {
        InitializeComponent();
        
        var viewModel = new RepositoryFormViewModel(repository, messenger);
        DataContext = viewModel;
        
        // Subscribe to dialog close message
        messenger.Register<DialogClosedMessage>(this, (r, m) =>
        {
            DialogResult = m.IsSuccess;
            Close();
        });
    }
}

// Parent ViewModel
public partial class MainViewModel : ObservableObject
{
    private readonly IMessenger messenger;
    
    public MainViewModel(IMessenger messenger)
    {
        this.messenger = messenger;
    }
    
    private void OpenRepositoryDialog()
    {
        // Dialog opened externally by View
        // Response handled by message
    }
}

// Parent View code-behind
public partial class MainWindow : Window
{
    private readonly IMessenger messenger;
    
    public MainWindow(MainViewModel viewModel, IMessenger messenger)
    {
        this.messenger = messenger;
        DataContext = viewModel;
        
        // Subscribe to edit repository command/message
        // Open dialog and handle response
    }
    
    private void OpenRepositoryForm()
    {
        var dialog = new RepositoryFormUC(selectedRepository, messenger);
        dialog.ShowDialog();
        
        // Dialog result handled by message subscription
    }
}
```

### 📋 Checklist

- [ ] Message class pour dialog response
- [ ] ViewModel gère logic, pas UI
- [ ] Code-behind minimal (juste Show/Close)
- [ ] `IMessenger.Register()` pour écouter fermeture
- [ ] Parent abonné au message de réponse
- [ ] Pas de `DialogResult` setting dans ViewModel

---

## 🔄 Pattern 7: Cascading Commands (Dépendances entre actions)

### ❌ Avant (Anti-pattern)

```csharp
private void Project_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    currentProject = (Project)ProjectComboBox.SelectedItem;
    
    if (currentProject == null)
    {
        DtoFilesComboBox.ItemsSource = null;
        SelectedDto = null;
        ClearFields();
        return;
    }
    
    LoadDtos();
}

private void Dto_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (currentProject == null)
        return;
    
    selectedDto = (DtoFile)DtoFilesComboBox.SelectedItem;
    
    ParseDtoFile();
    PopulateDtoOptions();
    RefreshUI();
}
```

### ✅ Après (Pattern correct)

```csharp
public partial class CRUDViewModel : ObservableObject
{
    [ObservableProperty] Project currentProject;
    [ObservableProperty] ObservableCollection<DtoFile> availableDtos = new();
    [ObservableProperty] DtoFile selectedDto;
    [ObservableProperty] DtoEntity parsedEntity;
    
    public RelayCommand<Project> ProjectSelectedCommand { get; }
    public RelayCommand<DtoFile> DtoSelectedCommand { get; }
    
    private readonly DtoService dtoService;
    private readonly ParsingService parsingService;
    
    public CRUDViewModel(
        DtoService dtoService,
        ParsingService parsingService)
    {
        this.dtoService = dtoService;
        this.parsingService = parsingService;
        
        ProjectSelectedCommand = new RelayCommand<Project>(SelectProject);
        DtoSelectedCommand = new RelayCommand<DtoFile>(SelectDto);
    }
    
    partial void OnCurrentProjectChanged(Project value)
    {
        if (value == null)
        {
            AvailableDtos.Clear();
            SelectedDto = null;
            ParsedEntity = null;
            return;
        }
        
        LoadDtosForProject(value);
    }
    
    partial void OnSelectedDtoChanged(DtoFile value)
    {
        if (value == null)
        {
            ParsedEntity = null;
            return;
        }
        
        ParseSelectedDto(value);
    }
    
    private void SelectProject(Project project)
    {
        CurrentProject = project;  // Trigger OnCurrentProjectChanged
    }
    
    private void SelectDto(DtoFile dto)
    {
        SelectedDto = dto;  // Trigger OnSelectedDtoChanged
    }
    
    private void LoadDtosForProject(Project project)
    {
        var dtos = dtoService.GetDtosForProject(project);
        AvailableDtos = new ObservableCollection<DtoFile>(dtos);
    }
    
    private void ParseSelectedDto(DtoFile dto)
    {
        ParsedEntity = parsingService.Parse(dto);
    }
}
```

```xaml
<ComboBox ItemsSource="{Binding Projects}"
          SelectedItem="{Binding CurrentProject}"
          DisplayMemberPath="Name" />

<ComboBox ItemsSource="{Binding AvailableDtos}"
          SelectedItem="{Binding SelectedDto}"
          DisplayMemberPath="Name" />

<ItemsControl ItemsSource="{Binding ParsedEntity.Properties}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

### 📋 Checklist

- [ ] `partial void OnPropertyChanged()` pour réagir aux changements
- [ ] Propriétés dépendantes mises à jour en cascade
- [ ] Chaque étape indépendante et testable
- [ ] Pas de state management complexe
- [ ] Commands optionnels (peut être juste binding)
- [ ] Messages pour side-effects entre ViewModels

---

## 📋 Checklist Générale de Refactoring

Pour chaque contrôle refactorisé:

### Architecture

- [ ] ViewModel créé avec `[ObservableProperty]` et `RelayCommand`
- [ ] Services injectés via constructor
- [ ] Code-behind réduit à < 50 lignes (juste bindings)
- [ ] Pas de logique métier dans code-behind
- [ ] Dépendances respectent DIP (interfaces, pas concrétions)

### SOLID Principles

- [ ] **SRP**: Chaque classe a une responsabilité unique
- [ ] **OCP**: Code ouvert à extension, fermé à modification
- [ ] **LSP**: Substitution de types polymorphes fonctionnel
- [ ] **ISP**: Interfaces ciblées, pas génériques
- [ ] **DIP**: Dépend des abstractions, pas des concrétions

### Bonnes Pratiques

- [ ] **DRY**: Pas de code dupliqué
- [ ] **KISS**: Logique simple, lisible
- [ ] **YAGNI**: Pas de code mort ou commenté
- [ ] Tests unitaires possibles (>80% testable)

### Testing

- [ ] ViewModel testable sans UI
- [ ] Services mockables
- [ ] Pas de `[STAThread]` ou threading dans ViewModel
- [ ] Pas de dépendance à System.Windows dans services métier

---

*Patterns générés selon les bonnes pratiques MVVM/SOLID - 19 janvier 2026*
