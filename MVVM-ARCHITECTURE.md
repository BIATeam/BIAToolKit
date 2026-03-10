# 🏗️ Architecture MVVM — BIAToolKit

## 📌 Contexte

BIAToolKit est une application WPF .NET dont l'architecture a été refactorisée pour suivre le patron MVVM (Model-View-ViewModel). Ce document décrit les conventions, la structure, et les patterns de communication adoptés.

---

## 📁 Structure du projet

```
BIA.ToolKit.Application/          ← Couche Application
├── Services/                    ← Services métier (logique applicative)
│   ├── CSharpParserService.cs
│   ├── SettingsService.cs
│   ├── UpdateService.cs
│   └── ...
└── ViewModel/                   ← Infrastructure MVVM uniquement
    ├── Interfaces/              ← Contrats MVVM
    │   ├── IViewModel.cs        ← Initialize() + Cleanup()
    │   └── IMessenger.cs        ← Pub/sub typé
    ├── Base/
    │   └── ViewModelBase.cs     ← Classe de base abstraite
    ├── MicroMvvm/               ← ObservableObject, RelayCommand
    ├── Messaging/
    │   ├── Messenger.cs         ← Implémentation thread-safe
    │   └── Messages/            ← Messages partagés (services → ViewModels)
    │       ├── IMessage.cs
    │       ├── SettingsUpdatedMessage.cs
    │       ├── SolutionParsedMessage.cs
    │       └── NewVersionAvailableMessage.cs
    └── MappingEntityProperty.cs ← Modèle de propriété DTO

BIA.ToolKit/                      ← Couche Présentation (WPF)
├── App.xaml.cs                  ← Configuration IoC (DI centralisée)
├── MainWindow.xaml              ← View principale
├── MainWindow.xaml.cs           ← Code-behind (lifecycle + dialog requests)
├── ViewModels/                  ← ViewModels de fonctionnalités
│   ├── MainViewModel.cs
│   ├── ModifyProjectViewModel.cs
│   ├── CRUDGeneratorViewModel.cs
│   ├── DtoGeneratorViewModel.cs
│   ├── OptionGeneratorViewModel.cs
│   ├── VersionAndOptionViewModel.cs
│   ├── RepositoryViewModel.cs
│   ├── RepositoryGitViewModel.cs
│   ├── RepositoryFolderViewModel.cs
│   ├── RepositoryFormViewModel.cs
│   ├── RepositoryFormMode.cs
│   ├── RepositoriesSettingsVM.cs
│   ├── RepositorySettingsVM.cs
│   ├── FeatureSettingViewModel.cs
│   ├── VersionAndOptionMapper.cs
│   └── Messaging/
│       └── Messages/            ← Messages UI (entre ViewModels de présentation)
│           ├── ExecuteWithWaiterMessage.cs
│           ├── ProjectChangedMessage.cs
│           ├── ClassesParsedMessage.cs
│           ├── NotificationMessage.cs
│           ├── OriginFeatureSettingsChangedMessage.cs
│           ├── OpenRepositoryFormRequestMessage.cs
│           ├── RepositoriesUpdatedMessage.cs
│           ├── RepositoryViewModelAddedMessage.cs
│           ├── RepositoryViewModelChangedMessage.cs
│           ├── RepositoryViewModelDeletedMessage.cs
│           ├── RepositoryViewModelReleaseDataUpdatedMessage.cs
│           └── RepositoryViewModelVersionXYZChangedMessage.cs
└── UserControls/
    ├── ModifyProjectUC.xaml.cs
    ├── CRUDGeneratorUC.xaml.cs
    ├── DtoGeneratorUC.xaml.cs
    ├── OptionGeneratorUC.xaml.cs
    └── VersionAndOptionUserControl.xaml.cs
```

### Séparation des namespaces

| Namespace | Contenu |
|-----------|---------|
| `BIA.ToolKit.Application.ViewModel` | Infrastructure MVVM : `IMessenger`, `IViewModel`, `ViewModelBase`, `MicroMvvm`, `Messenger`, `MappingEntityProperty` |
| `BIA.ToolKit.Application.ViewModel.Messaging.Messages` | Messages partagés avec la couche Application : `IMessage`, `SettingsUpdatedMessage`, `SolutionParsedMessage`, `NewVersionAvailableMessage` |
| `BIA.ToolKit.ViewModel` | ViewModels de fonctionnalités (couche présentation) |
| `BIA.ToolKit.ViewModel.Messaging.Messages` | Messages UI entre ViewModels de présentation |

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

### Messages Application (`BIA.ToolKit.Application.ViewModel.Messaging.Messages`)
Ces messages sont émis par les services de la couche Application et reçus par les ViewModels.

| Message | Quand est-il émis ? | Données |
|---------|--------------------|---------| 
| `SettingsUpdatedMessage` | Changement de paramètres (SettingsService) | `IBIATKSettings Settings` |
| `SolutionParsedMessage` | Fin de parsing de la solution C# (CSharpParserService) | _(aucune)_ |
| `NewVersionAvailableMessage` | Nouvelle version disponible (UpdateService) | _(aucune)_ |

### Messages Présentation (`BIA.ToolKit.ViewModel.Messaging.Messages`)
Ces messages sont émis et reçus uniquement dans la couche présentation (entre ViewModels).

| Message | Quand est-il émis ? | Données |
|---------|--------------------|---------| 
| `ProjectChangedMessage` | Changement de projet courant (ModifyProjectViewModel) | `Project Project` |
| `NotificationMessage` | Notification utilisateur | `string Message`, `NotificationType Type` |
| `ExecuteWithWaiterMessage` | Exécution d'une tâche async avec waiter UI | `Func<Task> Task` |
| `ClassesParsedMessage` | Classes parsées depuis le projet | _(aucune)_ |
| `OpenRepositoryFormRequestMessage` | Ouverture du formulaire repository | `RepositoryViewModel`, `RepositoryFormMode` |
| `RepositoriesUpdatedMessage` | Collection de repositories modifiée | _(aucune)_ |
| `RepositoryViewModelAddedMessage` | Nouveau repository créé | `RepositoryViewModel` |
| `RepositoryViewModelChangedMessage` | Repository modifié | `OldRepository`, `NewRepository` |
| `RepositoryViewModelDeletedMessage` | Repository supprimé | `RepositoryViewModel` |
| `RepositoryViewModelReleaseDataUpdatedMessage` | Données de release mises à jour | `RepositoryViewModel` |
| `RepositoryViewModelVersionXYZChangedMessage` | Flag IsVersionXYZ modifié | `RepositoryViewModel` |

---

## 🔄 Pattern UserControl (code-behind)

```csharp
public partial class MyFeatureUC : UserControl
{
    private MyFeatureViewModel vm;

    public MyFeatureUC()
    {
        InitializeComponent();
    }

    public void Inject(/* services nécessaires */, IMessenger messenger, MyFeatureViewModel viewModel)
    {
        this.vm = viewModel;
        DataContext = vm;

        Loaded += (_, _) => vm.Initialize();
        Unloaded += (_, _) => vm.Cleanup();
    }

    // Seuls les handlers UI purs restent ici (dialogs de confirmation, opérations UI)
    private async void DeleteWithConfirmation_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Confirmer ?", "Attention", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
        {
            await vm.DeleteAsync();
        }
    }
}
```

Les actions métier sont exposées comme `ICommand` dans le ViewModel et liées directement en XAML :

```xaml
<Button Command="{Binding GenerateCommand}" IsEnabled="{Binding IsGenerationEnabled}" Content="Generate"/>
```

---

## 🗂️ IoC — Enregistrement des dépendances

Dans `App.xaml.cs`, tous les services et ViewModels sont enregistrés :

```csharp
private void ConfigureServices(ServiceCollection services)
{
    // Infrastructure MVVM
    services.AddSingleton<IMessenger, Messenger>();

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
// BIA.ToolKit/ViewModels/MyNewViewModel.cs
namespace BIA.ToolKit.ViewModel
{
    using BIA.ToolKit.Application.ViewModel.Base;
    using BIA.ToolKit.Application.ViewModel.Interfaces;
    using BIA.ToolKit.Application.ViewModel.Messaging.Messages;
    using BIA.ToolKit.ViewModel.Messaging.Messages;

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
}
```

### 2. Créer le UserControl

```xaml
<!-- MyNewUC.xaml - DataContext défini en code-behind, PAS en XAML -->
<UserControl x:Class="BIA.ToolKit.UserControls.MyNewUC" ...>
    <!-- Contenu XAML lié au ViewModel via bindings -->
    <Button Command="{Binding DoSomethingCommand}" Content="Action"/>
</UserControl>
```

```csharp
// MyNewUC.xaml.cs - code-behind minimal
public partial class MyNewUC : UserControl
{
    private MyNewViewModel vm;

    public MyNewUC() { InitializeComponent(); }

    public void Inject(IMessenger messenger, MyNewViewModel viewModel)
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

## ⚡ Pattern Commands dans un ViewModel

Les actions déclenchées par les boutons UI sont exposées comme `ICommand` dans le ViewModel. Pour les opérations asynchrones avec indicateur de chargement (waiter), utiliser `ExecuteWithWaiterMessage` :

```csharp
public ICommand GenerateCrudCommand => new RelayCommand(_ =>
    Messenger.Send(new ExecuteWithWaiterMessage { Task = GenerateCRUDAsync }));

public ICommand DeleteLastGenerationCommand => new RelayCommand(_ =>
{
    var history = GetCurrentDtoHistory();
    Messenger.Send(new ExecuteWithWaiterMessage
    {
        Task = async () => { await DeleteLastGenerationAsync(history); }
    });
});
```

Pour les opérations synchrones simples :

```csharp
public ICommand RefreshProjectsListCommand => new RelayCommand(_ => RefreshProjetsList());
```

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
- [ ] Les actions métier sont exposées comme `ICommand` dans le ViewModel
- [ ] Les boutons sont liés via `Command="{Binding ...}"` dans le XAML
- [ ] Le code-behind ne contient pas de logique métier (uniquement UI : dialogs, drag-drop, redimensionnement)
- [ ] Les ViewModels de fonctionnalités sont dans `BIA.ToolKit/ViewModels/` (namespace `BIA.ToolKit.ViewModel`)
- [ ] Les messages UI sont dans `BIA.ToolKit/ViewModels/Messaging/Messages/` (namespace `BIA.ToolKit.ViewModel.Messaging.Messages`)

---

## 🔗 Références

- [Microsoft WPF MVVM Patterns](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- Issue EPIC : [#65 – Plan de Refactorisation MVVM – BIAToolKit](https://github.com/BIATeam/BIAToolKit/issues/65)

