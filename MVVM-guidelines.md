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
3. [ ] Supprimer le `DataContext` statique du XAML (sauf dialogues)
4. [ ] Le parent assigne `DataContext = App.GetService<MyVM>()`
5. [ ] Supprimer toute méthode publique de type proxy dans le code-behind
6. [ ] Implémenter `IDisposable` si le VM souscrit à des événements
7. [ ] Remplacer `System.Windows.Forms.Clipboard` par `System.Windows.Clipboard`
8. [ ] Vérifier qu'aucun setter ne contient de logique async/I/O
