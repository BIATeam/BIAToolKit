# MVVM Guidelines — BIAToolKit

## Architecture

```
BIA.ToolKit (UI)                  BIA.ToolKit.Application (Logic)
├── Views / UserControls           ├── ViewModels (ObservableObject)
├── Infrastructure/                ├── Services
│   ├── DialogService.cs           ├── Helper/
│   └── ViewModelLocator.cs        │   ├── IDialogService.cs
├── Helper/                        │   └── IConsoleWriter.cs
│   └── ConsoleWriter.cs           └── Mapper/
└── App.xaml.cs (DI root)
```

---

## 1. ViewModel Lifecycle

| Scénario | Lifetime DI | Raison |
|---|---|---|
| VM utilisé par un seul parent ou une seule fenêtre | `Transient` | Chaque instance de la View possède sa propre instance du VM |
| VM partagé entre plusieurs vues simultanément | `Singleton` | État partagé (rare, préférer l'EventBroker) |
| VM de dialogue (fenêtre modale) | `Transient` | Nouvelle instance à chaque ouverture |

**Règle :** un VM qui hérite d'`ObservableObject` ne doit **jamais** être Singleton si plusieurs instances de sa View peuvent coexister (ex : `VersionAndOptionUserControl` × 2 dans ModifyProjectUC).

---

## 2. Injection du DataContext

### Pour les contrôles enfants (UserControl)

Le **parent** crée et assigne le DataContext. Le XAML du UserControl ne doit PAS résoudre son propre VM.

```csharp
// ❌ Interdit — crée un couplage global
DataContext="{Binding Source={StaticResource Locator}, Path=MyViewModel}"

// ✅ Correct — le parent assigne
public void Init()
{
    MyChildControl.DataContext = App.GetService<ChildViewModel>();
}
```

### Pour les fenêtres modales (Window / Dialog)

Le `ViewModelLocator` peut être utilisé car chaque instance de Window est éphémère et unique.

```xml
<!-- ✅ OK pour les dialogues -->
DataContext="{Binding Source={StaticResource Locator}, Path=LogDetailViewModel}"
```

---

## 3. Code-behind : ce qui est autorisé / interdit

### Autorisé (pure UI)
- Manipulation de focus, animations, scroll
- Rendu UI non-bindable (ex : `TextBlock.Inlines` avec des couleurs)
- Appels à `ShowDialog()` derrière un `IDialogService`

### Interdit
- **Méthodes publiques qui délèguent au VM** (proxy pattern)
- Logique métier, accès fichier, accès réseau
- Exposition d'une API publique que d'autres contrôles appellent

```csharp
// ❌ Anti-pattern proxy
public void SelectVersion(string v) { ViewModel.SelectVersion(v); }

// ✅ L'appelant accède directement au VM
originVM.SelectVersion(version);
```

---

## 4. Communication inter-composants

### Consommateur → VM enfant
L'appelant accède au VM directement, pas à travers la View.

```csharp
// Le parent stocke une référence au VM (pas au UserControl)
private VersionAndOptionViewModel OriginVM => OriginControl.ViewModel;

// Puis :
OriginVM.SelectVersion("3.0.0");
```

### VM → VM (événements)
Utiliser le `UIEventBroker` pour la communication découplée.

### VM → UI (dialogues)
Utiliser `IDialogService` (injecté dans le VM ou le service appelant).

```csharp
// ❌ Le VM instancie une fenêtre
var dialog = new LogDetailUC();

// ✅ Le VM utilise une abstraction
dialogService.ShowLogDetail(messages);
```

---

## 5. Gestion de la mémoire

### Règle absolue : tout abonnement = désabonnement

Les VMs qui s'abonnent au `UIEventBroker` (ou tout autre événement de durée de vie supérieure) **doivent** implémenter `IDisposable`.

```csharp
public partial class MyViewModel : ObservableObject, IDisposable
{
    private bool disposed;

    public MyViewModel(UIEventBroker broker)
    {
        broker.OnSomething += HandleSomething;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        broker.OnSomething -= HandleSomething;
    }
}
```

L'appelant (parent View ou conteneur) est responsable d'appeler `Dispose()`.

---

## 6. Property setters : pas d'effets de bord complexes

```csharp
// ❌ Interdit — I/O, async, cascade dans un setter
public WorkRepository WorkCompanyFile
{
    set {
        eventBroker.RequestExecuteActionWithWaiter(async () => {
            File.ReadAllText(...); // Bloquant !
        });
    }
}

// ✅ Correct — setter simple, logique dans une méthode dédiée
public WorkRepository WorkCompanyFile
{
    set {
        if (field != value) {
            field = value;
            OnPropertyChanged();
            if (value != null) eventBroker.RequestExecuteActionWithWaiter(
                () => LoadCompanyFileSettingsAsync());
        }
    }
}

private async Task LoadCompanyFileSettingsAsync() { /* ... */ }
```

---

## 7. CommunityToolkit.Mvvm — conventions

| Besoin | Outil |
|---|---|
| Propriété observable simple | `[ObservableProperty]` |
| Propriété avec logique dans le setter | Property manuelle + `OnPropertyChanged()` |
| Commande (bouton, etc.) | `[RelayCommand]` sur méthode privée |
| Commande async | `[RelayCommand]` sur `async Task` |

Ne **pas** mélanger `[ObservableProperty]` et property manuelle pour la même donnée dans un même VM.

---

## 8. Checklist migration d'un composant

1. [ ] Créer le VM dans `BIA.ToolKit.Application/ViewModel/` (ou `BIA.ToolKit/ViewModels/` si dépend du UI layer)
2. [ ] Enregistrer en `Transient` dans `App.ConfigureServices()`
3. [ ] Supprimer le `DataContext` statique du XAML (sauf dialogues) : `<UserControl.DataContext><local:XxxViewModel /></UserControl.DataContext>`
4. [ ] Le parent assigne `DataContext = App.GetService<MyVM>()`
5. [ ] Supprimer toute méthode publique de type proxy dans le code-behind
6. [ ] Implémenter `IDisposable` si le VM souscrit à des événements
7. [ ] Remplacer `System.Windows.Forms.Clipboard` par `System.Windows.Clipboard`
8. [ ] Vérifier qu'aucun setter ne contient de logique async/I/O
9. [ ] Remplacer le pattern `Inject()` sur le VM par une injection de constructeur DI (→ section 10)
10. [ ] Migrer les handlers `TextChanged`/`SelectionChanged` qui appelaient des méthodes VM vers des bindings `UpdateSourceTrigger=PropertyChanged` + méthode partielle (→ section 11)
11. [ ] Supprimer les abonnements UIEventBroker du code-behind → les déplacer dans le VM avec `IDisposable`
12. [ ] Supprimer les méthodes proxy broker dans le code-behind (`UiEventBroker_OnProjectChanged` → `vm.SetCurrentProject(p)` etc.)
13. [ ] Conserver les handlers drag-drop qui délèguent entièrement à un `Behavior` XAML (UI pure — exemption)

---

## 9. État du projet & tâches de migration

### 9.1 Vue d'ensemble de la dette technique

| Composant | Fichier | Sévérité | Nature du problème |
|---|---|---|---|
| OptionGeneratorUC | `BIA.ToolKit/UserControls/OptionGeneratorUC.xaml.cs` | **CRITIQUE** | 546 lignes, logique métier complète, broker sans IDisposable, Inject() sur code-behind |
| DtoGeneratorUC | `BIA.ToolKit/UserControls/DtoGeneratorUC.xaml.cs` | **CRITIQUE** | TextChanged/SelectionChanged → appels VM, broker sans IDisposable, Inject() sur code-behind |
| CRUDGeneratorUC | `BIA.ToolKit/UserControls/CRUDGeneratorUC.xaml.cs` | **HAUT** | Proxy delegates broker → VM |
| OptionGeneratorUC.xaml | `BIA.ToolKit/UserControls/OptionGeneratorUC.xaml` | **HAUT** | DataContext statique dans XAML |
| DtoGeneratorUC.xaml | `BIA.ToolKit/UserControls/DtoGeneratorUC.xaml` | **HAUT** | DataContext statique dans XAML |
| CRUDGeneratorUC.xaml | `BIA.ToolKit/UserControls/CRUDGeneratorUC.xaml` | **HAUT** | DataContext statique dans XAML |
| CRUDGeneratorViewModel | `BIA.ToolKit.Application/ViewModel/CRUDGeneratorViewModel.cs` | **MOYEN** | Inject() au lieu de constructeur DI |
| DtoGeneratorViewModel | `BIA.ToolKit.Application/ViewModel/DtoGeneratorViewModel.cs` | **MOYEN** | Inject() au lieu de constructeur DI |
| MainViewModel | `BIA.ToolKit.Application/ViewModel/MainViewModel.cs` | **MOYEN** | 4 abonnements broker sans IDisposable |

**Modèles à suivre (déjà conformes) :**
- `VersionAndOptionUserControl.xaml.cs` → code-behind vide, propriété `ViewModel` uniquement
- `VersionAndOptionViewModel.cs` → constructeur DI complet, `IDisposable`, `readonly` fields
- `ModifyProjectUC.xaml.cs` → parent assigne les DataContexts via `App.GetService<>()`
- `LogDetailViewModel.cs` → `[ObservableProperty]` + `[RelayCommand]` corrects

---

### 9.2 Tâche CRITIQUE-1 : OptionGeneratorUC — extraction logique métier ✅ TERMINÉE

**Statut:** ✅ Complétée le 01/04/2026

**Fichier :** `BIA.ToolKit/UserControls/OptionGeneratorUC.xaml.cs`

**Problème :** le code-behind stocke 7 champs de services (`CSharpParserService`, `ZipParserService`, `GenerateCrudService`, `CRUDSettings`, `UIEventBroker`, `FileGeneratorService`, `IConsoleWriter`) injectés via une méthode `Inject()` publique. Il souscrit directement à `OnProjectChanged` et `OnSolutionClassesParsed` sans désabonnement. Toute la logique métier (parsing, historique, génération) est dans le code-behind.

**Actions :**

1. Migrer tous les champs de services et la logique métier dans `OptionGeneratorViewModel` via constructeur DI
2. `Generate_Click`, `DeleteLastGeneration_Click`, `DeleteBIAToolkitAnnotations_Click` → `[RelayCommand]` dans le VM
3. `ModifyEntity_SelectionChange` → binding `SelectedItem="{Binding Entity, Mode=TwoWay}"` + `partial void OnEntityChanged()` dans le VM, ou `[RelayCommand]` via `EventTrigger`/`InvokeCommandAction` si chargement secondaire nécessaire
4. `BIAFront_SelectionChanged` → binding `SelectedValue="{Binding BiaFront, Mode=TwoWay}"`
5. Manipulations de visibilité directe sur les contrôles → `Visibility="{Binding IsXxx, Converter={StaticResource BoolToVisConverter}}"`
6. Abonnements broker (`OnProjectChanged`, `OnSolutionClassesParsed`) → déplacés dans le VM, VM implémente `IDisposable`
7. Supprimer `<UserControl.DataContext><local:OptionGeneratorViewModel /></UserControl.DataContext>` du XAML
8. Enregistrer `OptionGeneratorViewModel` en `Transient` dans `App.ConfigureServices()`
9. Mettre à jour `ModifyProjectUC` pour assigner le DataContext via DI

**Avant (code-behind, à supprimer) :**
```csharp
// OptionGeneratorUC.xaml.cs
public void Inject(CSharpParserService csp, ..., UIEventBroker broker, ...)
{
    this.uiEventBroker = broker;
    broker.OnProjectChanged += UIEventBroker_OnProjectChanged;
    broker.OnSolutionClassesParsed += UiEventBroker_OnSolutionClassesParsed;
}

private void UIEventBroker_OnProjectChanged(Project project)
{
    SetCurrentProject(project); // logique métier dans le code-behind ❌
}

private void Generate_Click(object sender, RoutedEventArgs e)
{
    uiEventBroker.RequestExecuteActionWithWaiter(async () =>
        await fileGeneratorService.GenerateOptionAsync(...)); // ❌
}
```

**Après (VM) :**
```csharp
// OptionGeneratorViewModel.cs
public partial class OptionGeneratorViewModel : ObservableObject, IDisposable
{
    private readonly FileGeneratorService fileGeneratorService;
    private readonly UIEventBroker uiEventBroker;
    private bool disposed;

    public OptionGeneratorViewModel(
        CSharpParserService parserService,
        FileGeneratorService fileGeneratorService,
        UIEventBroker uiEventBroker,
        IConsoleWriter consoleWriter,
        ZipParserService zipService,
        GenerateCrudService crudService,
        SettingsService settingsService)
    {
        this.fileGeneratorService = fileGeneratorService;
        this.uiEventBroker = uiEventBroker;
        // ...
        uiEventBroker.OnProjectChanged += OnProjectChanged;
        uiEventBroker.OnSolutionClassesParsed += OnSolutionClassesParsed;
    }

    [RelayCommand]
    private void Generate()
        => uiEventBroker.RequestExecuteActionWithWaiter(async () =>
            await fileGeneratorService.GenerateOptionAsync(...));

    private void OnProjectChanged(Project project) => SetCurrentProject(project);

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        uiEventBroker.OnProjectChanged -= OnProjectChanged;
        uiEventBroker.OnSolutionClassesParsed -= OnSolutionClassesParsed;
    }
}
```

**Après (code-behind résiduel, modèle `VersionAndOptionUserControl`) :**
```csharp
// OptionGeneratorUC.xaml.cs
public partial class OptionGeneratorUC : UserControl
{
    public OptionGeneratorViewModel ViewModel => DataContext as OptionGeneratorViewModel;

    public OptionGeneratorUC()
    {
        InitializeComponent();
    }
}
```

---

### 9.3 Tâche CRITIQUE-2 : DtoGeneratorUC — handlers TextChanged/SelectionChanged

**Fichier :** `BIA.ToolKit/UserControls/DtoGeneratorUC.xaml.cs`

**Problème 1 :** `MappingPropertyTextBox_TextChanged` et `MappingOptionId_SelectionChanged` appellent `vm.ComputePropertiesValidity()` — ces appels doivent être remplacés par des bindings réactifs (→ section 11).

**Problème 2 :** `Inject()` sur le code-behind souscrit à `OnProjectChanged` et `OnSolutionClassesParsed` sans désabonnement — migrer dans `DtoGeneratorViewModel`.

**À conserver dans le code-behind (UI pure) :**
- `DragHandle_PreviewMouseLeftButtonDown` et `DragHandle_MouseMove` → délèguent entièrement à `ListViewDragDropBehavior`
- `ResetMappingColumnsWidths()` → manipulation de colonnes `GridView` non-bindable

**Actions :**

1. Supprimer `TextChanged="MappingPropertyTextBox_TextChanged"` du XAML → ajouter `UpdateSourceTrigger=PropertyChanged` au binding existant
2. Supprimer `SelectionChanged="MappingOptionId_SelectionChanged"` du XAML → binding `Mode=TwoWay`
3. Dans `DtoGeneratorViewModel`, ajouter `partial void OnMappingNameChanged(string value)` (ou `partial void OnOptionEntityIdPropertyChanged()`) pour déclencher `ComputePropertiesValidity()`
4. Déplacer les abonnements broker (`OnProjectChanged`, `OnSolutionClassesParsed`) dans `DtoGeneratorViewModel` via constructeur DI + `IDisposable`
5. Supprimer `<UserControl.DataContext>` du XAML
6. Enregistrer `DtoGeneratorViewModel` en `Transient` dans `App.ConfigureServices()`

---

### 9.4 Tâche HAUT-1 : CRUDGeneratorUC — proxy delegates

**Fichier :** `BIA.ToolKit/UserControls/CRUDGeneratorUC.xaml.cs`

**Problème :** le code-behind s'abonne à `OnProjectChanged` et `OnSolutionClassesParsed` puis proxy-délègue au VM :

```csharp
// ❌ À SUPPRIMER du code-behind
private void UiEventBroker_OnSolutionClassesParsed()
{
    vm.OnSolutionClassesParsed();
}

private void UiEventBroker_OnProjectChanged(Project project)
{
    vm.SetCurrentProject(project);
}
```

**Actions :**

1. Déplacer les abonnements `OnProjectChanged` et `OnSolutionClassesParsed` directement dans `CRUDGeneratorViewModel`
2. `CRUDGeneratorViewModel` implémente `IDisposable` (il acquiert de nouveaux abonnements broker)
3. Migrer `CRUDGeneratorViewModel.Inject()` vers constructeur DI (→ section 10, tâche 9.6)
4. Après migration : code-behind réduit au modèle `VersionAndOptionUserControl.xaml.cs`

---

### 9.5 Tâche HAUT-2 : Supprimer les DataContext statiques XAML

**Fichiers :**
- `BIA.ToolKit/UserControls/CRUDGeneratorUC.xaml`
- `BIA.ToolKit/UserControls/DtoGeneratorUC.xaml`
- `BIA.ToolKit/UserControls/OptionGeneratorUC.xaml`

**Avant (dans chaque fichier XAML) :**
```xml
<!-- ❌ À SUPPRIMER -->
<UserControl.DataContext>
    <local:CRUDGeneratorViewModel />
</UserControl.DataContext>
```

**Après — mettre à jour `ModifyProjectUC.xaml.cs` (méthode `Inject()`) :**
```csharp
// ✅ Le parent assigne via DI
CRUDGenerator.DataContext = App.GetService<CRUDGeneratorViewModel>();
OptionGenerator.DataContext = App.GetService<OptionGeneratorViewModel>();
DtoGenerator.DataContext = App.GetService<DtoGeneratorViewModel>();
```

---

### 9.6 Tâche MOYEN-1 : CRUDGeneratorViewModel — Inject() → constructeur DI

**Fichier :** `BIA.ToolKit.Application/ViewModel/CRUDGeneratorViewModel.cs`

Services à migrer depuis `Inject()` : `CSharpParserService`, `ZipParserService`, `GenerateCrudService`, `SettingsService`, `IConsoleWriter`, `UIEventBroker`, `FileGeneratorService`, `IDialogService`.

Note : après avoir repris les abonnements broker depuis `CRUDGeneratorUC`, implémenter `IDisposable`.

Enregistrer en `Transient` dans `App.ConfigureServices()`.

Voir section 10 pour le guide de migration mécanique.

---

### 9.7 Tâche MOYEN-2 : DtoGeneratorViewModel — Inject() → constructeur DI

**Fichier :** `BIA.ToolKit.Application/ViewModel/DtoGeneratorViewModel.cs`

Services à migrer depuis `Inject()` : `FileGeneratorService`, `IConsoleWriter`, `SettingsService`, `UIEventBroker`.

La méthode `SetProject(Project)` reste publique (appelée par le VM lui-même après abonnement broker).

Enregistrer en `Transient` dans `App.ConfigureServices()`.

---

### 9.8 Tâche MOYEN-3 : MainViewModel — IDisposable manquant

**Fichier :** `BIA.ToolKit.Application/ViewModel/MainViewModel.cs`

Le constructeur souscrit à 4 événements broker (`OnSettingsUpdated`, `OnRepositoryViewModelChanged`, `OnRepositoryViewModelDeleted`, `OnRepositoryViewModelAdded`) sans `Dispose()`.

Ajouter `IDisposable` + `bool disposed` + méthode `Dispose()` sur le modèle de `VersionAndOptionViewModel.cs`.

---

## 10. Guide : pattern Inject() → constructeur DI

Ce guide est applicable mécaniquement à `CRUDGeneratorViewModel`, `DtoGeneratorViewModel`, et tout futur VM qui utilise encore `Inject()`.

**Étape 1 — Identifier les services injectés**

Lister chaque paramètre de la méthode `Inject()` publique du VM.

**Étape 2 — Convertir les champs en `readonly`, créer le constructeur DI**

```csharp
// ❌ Avant
private CSharpParserService parserService;
private IConsoleWriter consoleWriter;

public void Inject(CSharpParserService parserService, IConsoleWriter consoleWriter, ...)
{
    this.parserService = parserService;
    this.consoleWriter = consoleWriter;
}
```

```csharp
// ✅ Après
private readonly CSharpParserService parserService;
private readonly IConsoleWriter consoleWriter;

public MyViewModel(
    CSharpParserService parserService,
    IConsoleWriter consoleWriter,
    FileGeneratorService fileGeneratorService,
    SettingsService settingsService,
    UIEventBroker uiEventBroker)
{
    this.parserService = parserService;
    this.consoleWriter = consoleWriter;
    this.fileGeneratorService = fileGeneratorService;
    this.uiEventBroker = uiEventBroker;
}
```

**Étape 3 — Supprimer la méthode `Inject()` publique**

**Étape 4 — Enregistrer le VM dans `App.ConfigureServices()`**

```csharp
// App.xaml.cs — dans ConfigureServices()
services.AddTransient<CRUDGeneratorViewModel>();
services.AddTransient<DtoGeneratorViewModel>();
services.AddTransient<OptionGeneratorViewModel>();
```

**Étape 5 — Mettre à jour le parent qui appelait `Inject()`**

```csharp
// ❌ Avant (ModifyProjectUC ou autre parent)
CRUDGenerator.Inject(cSharpParserService, zipService, ...);

// ✅ Après
CRUDGenerator.DataContext = App.GetService<CRUDGeneratorViewModel>();
```

**Étape 6 — Si le code-behind (UC) avait aussi sa propre méthode `Inject()`**

Supprimer la méthode `Inject()` du code-behind. Les services qu'il utilisait directement doivent être migrés dans le VM ou devenus accessibles via le VM.

**Modèle de référence :** `VersionAndOptionViewModel.cs` — constructeur DI complet, `IDisposable`, `readonly` fields.

---

## 11. Bindings XAML : remplacer les event handlers par INotifyPropertyChanged

**Règle :** tout event handler `TextChanged`, `SelectionChanged`, ou `LostFocus` dont le seul effet est d'appeler une méthode sur le VM doit être supprimé et remplacé par un binding réactif.

### 11.1 TextChanged

```xml
<!-- ❌ Avant -->
<TextBox Text="{Binding MappingName}" TextChanged="MappingPropertyTextBox_TextChanged" />

<!-- ✅ Après — UpdateSourceTrigger=PropertyChanged déclenche la mise à jour à chaque frappe -->
<TextBox Text="{Binding MappingName, UpdateSourceTrigger=PropertyChanged, Mode=TwoWay}" />
```

```csharp
// ❌ Avant — code-behind
private void MappingPropertyTextBox_TextChanged(object sender, TextChangedEventArgs e)
{
    vm.ComputePropertiesValidity();
}
```

```csharp
// ✅ Après — VM, méthode partielle déclenchée automatiquement
[ObservableProperty]
private string mappingName;

partial void OnMappingNameChanged(string value)
{
    ComputeValidity(); // remplace l'appel externe à ComputePropertiesValidity()
}
```

### 11.2 SelectionChanged (simple)

```xml
<!-- ❌ Avant -->
<ComboBox SelectedValue="{Binding OptionEntityIdProperty}"
          SelectionChanged="MappingOptionId_SelectionChanged" />

<!-- ✅ Après -->
<ComboBox SelectedValue="{Binding OptionEntityIdProperty, UpdateSourceTrigger=PropertyChanged, Mode=TwoWay}" />
```

```csharp
// ✅ VM
[ObservableProperty]
private string optionEntityIdProperty;

partial void OnOptionEntityIdPropertyChanged(string value)
{
    ComputeValidity();
}
```

### 11.3 SelectionChanged avec logique complexe (chargement secondaire)

Utiliser `EventTrigger` + `InvokeCommandAction` (namespace `i:` = `Microsoft.Xaml.Behaviors`) :

```xml
<ComboBox SelectedItem="{Binding Entity, Mode=TwoWay}">
    <i:Interaction.Triggers>
        <i:EventTrigger EventName="SelectionChanged">
            <i:InvokeCommandAction Command="{Binding OnEntitySelectionChangedCommand}" />
        </i:EventTrigger>
    </i:Interaction.Triggers>
</ComboBox>
```

```csharp
// VM
[RelayCommand]
private void OnEntitySelectionChanged()
{
    ParseEntityFile(); // logique déclenchée à la sélection
}
```

### 11.4 Cas légitimes dans le code-behind (ne pas migrer)

| Handler | Verdict | Raison |
|---|---|---|
| `DragHandle_PreviewMouseLeftButtonDown` → délègue à `ListViewDragDropBehavior` | **CONSERVER** | UI pure, aucun accès au VM |
| `ResetMappingColumnsWidths()` sur colonnes `GridView` | **CONSERVER** | Manipulation de contrôle non-bindable |
| `TextBlock.Inlines` coloré dans `LogDetailUC` | **CONSERVER** | Rendu non-bindable |
| `DialogResult = true/false` dans les boutons de dialogue | **CONSERVER** | Pattern Window accepté |

---

## 12. Références rapides

| Pattern cible | Modèle à suivre | Fichiers à migrer |
|---|---|---|
| Code-behind vide | `VersionAndOptionUserControl.xaml.cs` | `OptionGeneratorUC.xaml.cs`, `DtoGeneratorUC.xaml.cs`, `CRUDGeneratorUC.xaml.cs` |
| Constructeur DI + `IDisposable` | `VersionAndOptionViewModel.cs` | `CRUDGeneratorViewModel.cs`, `DtoGeneratorViewModel.cs`, `OptionGeneratorViewModel.cs` |
| Parent assigne DataContext | `ModifyProjectUC.xaml.cs` | Supprimer DataContext statique dans les 3 XAML |
| `[ObservableProperty]` + partial method | `LogDetailViewModel.cs` | Toutes propriétés simples sans setter logic |
| `[RelayCommand]` | `CRUDGeneratorViewModel.cs` | Tous les boutons encore liés à Click handlers |
| Dialogue via `IDialogService` | `CRUDGeneratorViewModel.cs` | Remplacer tout `MessageBox.Show()` direct dans les VMs |
| `IDisposable` sur VM avec broker | `VersionAndOptionViewModel.cs`, `ModifyProjectViewModel.cs` | `MainViewModel.cs`, `CRUDGeneratorViewModel.cs`, `DtoGeneratorViewModel.cs`, `OptionGeneratorViewModel.cs` |

---

## Vérification post-migration

```bash
# 0 résultat attendu dans les UserControls
grep -r "Inject(" BIA.ToolKit/UserControls/

# 0 DataContext statique dans les 3 XAML
grep -n "UserControl.DataContext" BIA.ToolKit/UserControls/CRUDGeneratorUC.xaml
grep -n "UserControl.DataContext" BIA.ToolKit/UserControls/DtoGeneratorUC.xaml
grep -n "UserControl.DataContext" BIA.ToolKit/UserControls/OptionGeneratorUC.xaml

# 0 handler event vers VM dans DtoGeneratorUC et OptionGeneratorUC
grep -n "TextChanged=\|SelectionChanged=" BIA.ToolKit/UserControls/DtoGeneratorUC.xaml
grep -n "Click=\|SelectionChanged=" BIA.ToolKit/UserControls/OptionGeneratorUC.xaml

# IDisposable présent dans tous les VMs qui souscrivent au broker
grep -rn "IDisposable" BIA.ToolKit.Application/ViewModel/
```

Build complet sans erreur = migration réussie.
