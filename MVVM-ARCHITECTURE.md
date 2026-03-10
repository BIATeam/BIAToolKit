# 🏗️ Architecture MVVM — BIAToolKit

## 📌 Contexte

BIAToolKit est une application WPF .NET dont l'architecture a été refactorisée pour suivre le patron MVVM (Model-View-ViewModel). Ce document décrit les conventions, la structure, et les patterns de communication adoptés.

---

## 📁 Structure du projet

```
BIA.ToolKit.Application/
├── Services/                    ← Services métier (logique applicative)
│   ├── CSharpParserService.cs
│   ├── SettingsService.cs
│   ├── UpdateService.cs
│   └── ...
├── ViewModel/
│   ├── Interfaces/              ← Contrats MVVM
│   │   ├── IViewModel.cs        ← Initialize() + Cleanup()
│   │   └── IMessenger.cs        ← Pub/sub typé
│   ├── Base/
│   │   └── ViewModelBase.cs     ← Classe de base abstraite
│   ├── Messaging/
│   │   ├── Messenger.cs         ← Implémentation thread-safe
│   │   └── Messages/            ← Messages typés
│   │       ├── IMessage.cs
│   │       ├── ProjectChangedMessage.cs
│   │       ├── SettingsUpdatedMessage.cs
│   │       ├── SolutionParsedMessage.cs
│   │       ├── NewVersionAvailableMessage.cs
│   │       └── ...
│   ├── ModifyProjectViewModel.cs
│   ├── MainViewModel.cs
│   ├── CRUDGeneratorViewModel.cs
│   ├── DtoGeneratorViewModel.cs
│   ├── OptionGeneratorViewModel.cs
│   └── VersionAndOptionViewModel.cs

BIA.ToolKit/
├── App.xaml.cs                  ← Configuration IoC (DI centralisée)
├── MainWindow.xaml              ← View principale
├── MainWindow.xaml.cs           ← Code-behind minimal (lifecycle seulement)
└── UserControls/
    ├── ModifyProjectUC.xaml.cs  ← ~70 lignes (lifecycle + délégation VM)
    ├── CRUDGeneratorUC.xaml.cs  ← ~162 lignes (lifecycle + délégation VM)
    ├── DtoGeneratorUC.xaml.cs   ← ~146 lignes (lifecycle + délégation VM)
    ├── OptionGeneratorUC.xaml.cs← ~145 lignes (lifecycle + délégation VM)
    └── VersionAndOptionUserControl.xaml.cs
```

---

## 🎯 Principes fondamentaux

### 1. Code-behind minimal
Le code-behind ne doit contenir que :
- `InitializeComponent()` (constructeur)
- Configuration du `DataContext` (depuis injection)
- `Loaded` / `Unloaded` pour le cycle de vie du ViewModel
- Gestion des événements purement UI (redimensionnement colonnes, DialogBox, etc.)

### 2. ViewModels responsables
Toute la logique métier et de présentation réside dans le ViewModel :
- Accès aux services
- Réaction aux messages (IMessenger)
- Commandes (ICommand)
- État de la vue (propriétés observables)

### 3. Communication via messages typés
Les ViewModels communiquent entre eux via des messages typés (IMessenger), **non plus via UIEventBroker**.

### 4. Injection de dépendances centralisée
Tous les services et ViewModels sont enregistrés dans `App.xaml.cs` via Microsoft.Extensions.DependencyInjection.

---

## 🔧 Pattern ViewModelBase

```csharp
public class MyFeatureViewModel : ViewModelBase
{
    private readonly IMyService myService;

    public MyFeatureViewModel(IMessenger messenger, IMyService myService)
        : base(messenger)
    {
        this.myService = myService;
    }

    public override void Initialize()
    {
        Messenger.Subscribe<ProjectChangedMessage>(OnProjectChanged);
        Messenger.Subscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
    }

    public override void Cleanup()
    {
        Messenger.Unsubscribe<ProjectChangedMessage>(OnProjectChanged);
        Messenger.Unsubscribe<SettingsUpdatedMessage>(OnSettingsUpdated);
    }

    private void OnProjectChanged(ProjectChangedMessage msg)
    {
        // Traiter le changement de projet
    }

    private void OnSettingsUpdated(SettingsUpdatedMessage msg)
    {
        // Réagir aux changements de paramètres
    }
}
```

---

## 📨 Messages disponibles

| Message | Quand est-il émis ? | Données |
|---------|--------------------|---------| 
| `ProjectChangedMessage` | Changement de projet courant (ModifyProjectViewModel) | `Project Project` |
| `SettingsUpdatedMessage` | Changement de paramètres (SettingsService) | `IBIATKSettings Settings` |
| `SolutionParsedMessage` | Fin de parsing de la solution C# (CSharpParserService) | _(aucune)_ |
| `NewVersionAvailableMessage` | Nouvelle version disponible (UpdateService) | _(aucune)_ |
| `NotificationMessage` | Notification utilisateur | `string Message`, `NotificationType Type` |

---

## 🔄 Pattern UserControl (code-behind)

```csharp
public partial class MyFeatureUC : UserControl
{
    private MyFeatureViewModel vm;
    private UIEventBroker uiEventBroker; // conservé pour RequestExecuteActionWithWaiter

    public MyFeatureUC()
    {
        InitializeComponent();
    }

    public void Inject(/* services nécessaires */, UIEventBroker uiEventBroker, MyFeatureViewModel viewModel)
    {
        this.uiEventBroker = uiEventBroker;
        this.vm = viewModel;
        DataContext = vm;

        Loaded += (_, _) => vm.Initialize();
        Unloaded += (_, _) => vm.Cleanup();
    }

    // Seuls les handlers UI purs restent ici
    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        uiEventBroker.RequestExecuteActionWithWaiter(vm.GenerateAsync);
    }
}
```

---

## 🗂️ IoC — Enregistrement des dépendances

Dans `App.xaml.cs`, tous les services et ViewModels sont enregistrés :

```csharp
private void ConfigureServices(ServiceCollection services)
{
    // Infrastructure MVVM
    services.AddSingleton<IMessenger, Messenger>();
    services.AddSingleton<UIEventBroker>();

    // ViewModels (singletons car ils vivent toute la session)
    services.AddSingleton<MainViewModel>(); // créé manuellement (Version arg)
    services.AddSingleton<ModifyProjectViewModel>();
    services.AddSingleton<CRUDGeneratorViewModel>();
    services.AddSingleton<DtoGeneratorViewModel>();
    services.AddSingleton<OptionGeneratorViewModel>();
    // Note : VersionAndOptionViewModel est créé via new() dans Inject()
    //        car plusieurs instances sont nécessaires simultanément.

    // Services applicatifs
    services.AddSingleton<IConsoleWriter, ConsoleWriter>();
    services.AddSingleton<SettingsService>();
    services.AddSingleton<CSharpParserService>();
    services.AddSingleton<FileGeneratorService>();
    services.AddSingleton<UpdateService>();
    // ...

    services.AddLogging();
}
```

---

## ➕ Ajouter une nouvelle fonctionnalité

### 1. Créer le ViewModel

```csharp
// BIA.ToolKit.Application/ViewModel/MyNewViewModel.cs
public class MyNewViewModel : ViewModelBase
{
    public MyNewViewModel(IMessenger messenger, IMyService service)
        : base(messenger) { }

    public override void Initialize()
    {
        Messenger.Subscribe<ProjectChangedMessage>(OnProjectChanged);
    }

    public override void Cleanup()
    {
        Messenger.Unsubscribe<ProjectChangedMessage>(OnProjectChanged);
    }
}
```

### 2. Créer le UserControl

```xaml
<!-- MyNewUC.xaml - DataContext défini en code-behind, PAS en XAML -->
<UserControl x:Class="BIA.ToolKit.UserControls.MyNewUC" ...>
    <!-- Contenu XAML lié au ViewModel via bindings -->
</UserControl>
```

```csharp
// MyNewUC.xaml.cs - code-behind minimal
public partial class MyNewUC : UserControl
{
    private MyNewViewModel vm;

    public MyNewUC() { InitializeComponent(); }

    public void Inject(UIEventBroker uiEventBroker, MyNewViewModel viewModel)
    {
        this.vm = viewModel;
        DataContext = vm;
        Loaded += (_, _) => vm.Initialize();
        Unloaded += (_, _) => vm.Cleanup();
    }
}
```

### 3. Enregistrer dans l'IoC

```csharp
// App.xaml.cs
services.AddSingleton<MyNewViewModel>();
```

### 4. Passer par injection

Dans le parent (ex: MainWindow ou ModifyProjectUC), recevoir le ViewModel via le constructeur DI et le passer à `UC.Inject(...)`.

---

## 📤 Envoyer un message

```csharp
// Depuis un service :
messenger.Send(new ProjectChangedMessage { Project = myProject });

// Depuis un ViewModel (accès via Messenger property) :
Messenger.Send(new SettingsUpdatedMessage { Settings = settings });
```

---

## 📥 Recevoir un message

```csharp
public override void Initialize()
{
    Messenger.Subscribe<ProjectChangedMessage>(OnProjectChanged);
}

public override void Cleanup()
{
    Messenger.Unsubscribe<ProjectChangedMessage>(OnProjectChanged);
}

private void OnProjectChanged(ProjectChangedMessage msg)
{
    CurrentProject = msg.Project;
}
```

---

## ✅ Checklist de validation lors d'un refactoring

- [ ] Le ViewModel hérite de `ViewModelBase`
- [ ] Les services sont injectés par constructeur (pas de méthode `Inject()`)
- [ ] `Initialize()` souscrit aux messages nécessaires
- [ ] `Cleanup()` désouscrit des mêmes messages
- [ ] Le DataContext est défini en code-behind (pas dans le XAML)
- [ ] `Loaded` → `vm.Initialize()` et `Unloaded` → `vm.Cleanup()` sont câblés
- [ ] Le ViewModel est enregistré comme singleton dans `App.xaml.cs`
- [ ] Aucun `UIEventBroker.OnProjectChanged` ou `OnSolutionClassesParsed` en code-behind
- [ ] Le code-behind ne contient pas de logique métier

---

## 🔗 Références

- [Microsoft WPF MVVM Patterns](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- Issue EPIC : [#65 – Plan de Refactorisation MVVM – BIAToolKit](https://github.com/BIATeam/BIAToolKit/issues/65)
