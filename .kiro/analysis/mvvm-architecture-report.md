# État des Lieux - Architecture MVVM de BIA.ToolKit

## Vue d'ensemble

BIA.ToolKit est une application WPF .NET 9.0 qui utilise une architecture MVVM (Model-View-ViewModel) avec une implémentation custom appelée **MicroMvvm**.

## Architecture Actuelle

### 1. Framework MVVM Utilisé

**MicroMvvm** - Implémentation légère et custom basée sur les articles de Josh Smith (MSDN Magazine)

#### Composants principaux :

**ObservableObject** (`BIA.ToolKit.Application/ViewModel/MicroMvvm/ObservableObject.cs`)
- Classe de base abstraite pour tous les ViewModels
- Implémente `INotifyPropertyChanged`
- Méthodes disponibles :
  - `RaisePropertyChanged(string propertyName)` - Notification manuelle par nom
  - `RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)` - Notification par expression lambda
  - `VerifyPropertyName(string propertyName)` - Validation en mode DEBUG

**RelayCommand** (`BIA.ToolKit/Helper/MicroMvvm/RelayCommand.cs`)
- Implémentation de `ICommand`
- Deux versions : générique `RelayCommand<T>` et non-générique `RelayCommand`
- Support de `CanExecute` optionnel
- Utilise `CommandManager.RequerySuggested` pour la réévaluation automatique

### 2. ViewModels Existants

Le projet contient 11 ViewModels principaux :

1. **MainViewModel** - ViewModel principal de l'application
2. **CRUDGeneratorViewModel** - Génération de CRUD
3. **DtoGeneratorViewModel** - Génération de DTO
4. **ModifyProjectViewModel** - Modification de projet
5. **OptionGeneratorViewModel** - Génération d'options
6. **VersionAndOptionViewModel** - Gestion versions/options
7. **RepositoryViewModel** (abstrait) - Base pour les repositories
8. **RepositoryGitViewModel** - Repository Git
9. **RepositoryFolderViewModel** - Repository dossier
10. **RepositoryFormViewModel** - Formulaire repository
11. **FeatureSettingViewModel** - Paramètres de features

### 3. Pattern d'Utilisation Actuel

#### Exemple typique (MainViewModel) :

```csharp
public class MainViewModel : ObservableObject
{
    // Propriété avec notification
    private bool _updateAvailable;
    public bool UpdateAvailable
    {
        get => _updateAvailable;
        set
        {
            _updateAvailable = value;
            RaisePropertyChanged(nameof(UpdateAvailable));
        }
    }

    // Commande avec lambda
    public ICommand OpenToolkitRepositorySettingsCommand => 
        new RelayCommand((_) => eventBroker.RequestOpenRepositoryForm(...));
}
```

#### Points observés :
- ✅ Notification manuelle avec `nameof()` (type-safe)
- ✅ Commandes déclarées comme propriétés en lecture seule
- ✅ Injection de dépendances via constructeur
- ❌ Pas de binding bidirectionnel automatique
- ❌ Répétition du pattern getter/setter pour chaque propriété
- ❌ Commandes créées à chaque accès (pas de cache)

### 4. Code-Behind

Les UserControls (`.xaml.cs`) contiennent beaucoup de logique métier :
- Gestion d'événements UI
- Manipulation directe du ViewModel
- Logique de validation
- Appels de services

**Exemple** : `CRUDGeneratorUC.xaml.cs` contient ~800 lignes avec :
- Injection de services
- Logique de génération
- Gestion d'historique
- Manipulation de fichiers

## Points Forts de l'Implémentation Actuelle

✅ **Légèreté** - Pas de dépendance externe lourde
✅ **Simplicité** - Code facile à comprendre
✅ **Contrôle total** - Pas de "magie" cachée
✅ **Performance** - Overhead minimal
✅ **Compatibilité** - Fonctionne avec .NET 9.0

## Points d'Amélioration Identifiés

### 1. Boilerplate Code

❌ **Problème** : Répétition du pattern getter/setter pour chaque propriété

```csharp
// Code actuel - répétitif
private string _value;
public string Value
{
    get => _value;
    set
    {
        _value = value;
        RaisePropertyChanged(nameof(Value));
    }
}
```

💡 **Solution** : Utiliser des attributs source generators ou une méthode helper

### 2. Commandes Non-Cachées

❌ **Problème** : Les commandes sont recréées à chaque accès

```csharp
// Code actuel - nouvelle instance à chaque fois
public ICommand MyCommand => new RelayCommand(() => DoSomething());
```

💡 **Solution** : Lazy initialization ou backing field

### 3. Logique dans Code-Behind

❌ **Problème** : Beaucoup de logique métier dans les `.xaml.cs`
- Difficile à tester unitairement
- Couplage fort avec l'UI
- Violation du principe MVVM

💡 **Solution** : Déplacer la logique vers les ViewModels

### 4. Pas de Messenger/EventAggregator Standard

⚠️ **Observation** : Utilisation d'un `UIEventBroker` custom
- Fonctionne mais non-standard
- Difficile à découvrir pour les nouveaux développeurs

### 5. Validation

❌ **Manque** : Pas d'implémentation de `IDataErrorInfo` ou `INotifyDataErrorInfo`
- Validation manuelle dans le code-behind
- Pas de binding automatique des erreurs

## Recommandations

### Option 1 : Améliorer MicroMvvm (Recommandé pour court terme)

**Avantages** :
- Pas de breaking changes
- Évolution progressive
- Garde le contrôle

**Améliorations suggérées** :

1. **Ajouter une méthode SetProperty** :
```csharp
protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
{
    if (EqualityComparer<T>.Default.Equals(field, value))
        return false;
    
    field = value;
    RaisePropertyChanged(propertyName);
    return true;
}
```

Usage :
```csharp
private string _value;
public string Value
{
    get => _value;
    set => SetProperty(ref _value, value);
}
```

2. **Cacher les commandes** :
```csharp
private RelayCommand _myCommand;
public ICommand MyCommand => _myCommand ??= new RelayCommand(() => DoSomething());
```

3. **Ajouter AsyncRelayCommand** pour les opérations async :
```csharp
public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool> _canExecute;
    private bool _isExecuting;
    
    // Implementation...
}
```

### Option 2 : Adopter CommunityToolkit.Mvvm (Recommandé pour long terme)

**Avantages** :
- Standard Microsoft officiel
- Source generators (moins de boilerplate)
- Async commands built-in
- Messenger pattern intégré
- Validation intégrée
- Bien documenté et maintenu

**Package** : `CommunityToolkit.Mvvm` (anciennement MVVM Light Toolkit)

**Migration progressive possible** :
```csharp
// Avant
public class MyViewModel : ObservableObject
{
    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            RaisePropertyChanged(nameof(Name));
        }
    }
}

// Après avec CommunityToolkit
public partial class MyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;
    
    // Le source generator crée automatiquement la propriété publique Name
}
```

**Commandes** :
```csharp
// Avant
public ICommand SaveCommand => new RelayCommand(() => Save());

// Après
[RelayCommand]
private void Save()
{
    // Logic
}
// Génère automatiquement SaveCommand
```

### Option 3 : Hybrid Approach (Recommandé)

**Phase 1** (Court terme - 1-2 semaines) :
1. Améliorer MicroMvvm avec `SetProperty` et cache de commandes
2. Extraire la logique des code-behind vers les ViewModels
3. Ajouter `AsyncRelayCommand`

**Phase 2** (Moyen terme - 1-2 mois) :
1. Installer `CommunityToolkit.Mvvm` en parallèle
2. Migrer les nouveaux ViewModels vers CommunityToolkit
3. Migrer progressivement les ViewModels existants

**Phase 3** (Long terme - 3-6 mois) :
1. Supprimer MicroMvvm une fois tous les ViewModels migrés
2. Adopter les patterns avancés (Messenger, Validation)

## Dépendances Actuelles

```xml
<PackageReference Include="MaterialDesignThemes" Version="5.2.1" />
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.135" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.6" />
```

**À ajouter pour CommunityToolkit** :
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
```

## Métriques du Code

- **ViewModels** : 11 classes
- **UserControls** : 7 contrôles avec code-behind
- **Lignes de code MVVM** : ~5000 lignes estimées
- **Commandes** : ~30 commandes identifiées
- **Propriétés notifiables** : ~150 propriétés estimées

## Conclusion

L'architecture MVVM actuelle de BIA.ToolKit est **fonctionnelle mais pourrait bénéficier de modernisation** :

**Forces** :
- Architecture claire et compréhensible
- Pas de sur-engineering
- Fonctionne bien pour l'application actuelle

**Faiblesses** :
- Beaucoup de boilerplate code
- Logique métier dans les code-behind
- Pas de support async moderne
- Difficile à tester unitairement

**Recommandation finale** : 
Adopter l'approche hybride avec migration progressive vers `CommunityToolkit.Mvvm`. Cela permettra de :
- Réduire le boilerplate de 60-70%
- Améliorer la testabilité
- Moderniser le code sans tout réécrire
- Bénéficier du support Microsoft officiel

## Prochaines Étapes Suggérées

1. ✅ Créer une spec pour la modernisation MVVM
2. Commencer par améliorer MicroMvvm (quick wins)
3. Créer un ViewModel pilote avec CommunityToolkit
4. Planifier la migration progressive
