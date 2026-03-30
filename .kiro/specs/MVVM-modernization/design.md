# Design Document - Modernisation MVVM

## Overview

Ce document décrit l'architecture technique pour la modernisation progressive de l'infrastructure MVVM de BIA.ToolKit. La migration s'effectue en 3 phases sur une période de 3-6 mois, permettant une transition sans rupture depuis l'implémentation custom MicroMvvm vers le standard Microsoft CommunityToolkit.Mvvm.

### Objectifs Techniques

- Réduire le code boilerplate de 60-70% via source generators
- Améliorer la testabilité en extrayant la logique des code-behind
- Adopter les standards Microsoft modernes (CommunityToolkit.Mvvm)
- Maintenir la compatibilité durant toute la migration
- Garantir zéro régression fonctionnelle

### Stratégie de Migration

**Approche Hybride en 3 Phases:**

1. **Phase 1 (1-2 semaines)**: Amélioration de MicroMvvm + Installation CommunityToolkit + ViewModel pilote
2. **Phase 2 (1-2 mois)**: Migration progressive des 11 ViewModels + Extraction logique + Tests
3. **Phase 3 (3-6 mois)**: Finalisation + Documentation + Métriques + Dépréciation MicroMvvm

Cette approche permet de valider chaque étape avant de continuer, minimisant les risques.

## Architecture

### Architecture Actuelle (MicroMvvm)

```
┌─────────────────────────────────────────────────────────────┐
│                        WPF Views                             │
│  (MainWindow.xaml, UserControls/*.xaml, Dialogs/*.xaml)    │
└────────────────────┬────────────────────────────────────────┘
                     │ Data Binding
                     │ Command Binding
┌────────────────────▼────────────────────────────────────────┐
│                    Code-Behind (.xaml.cs)                    │
│  • Event handlers                                            │
│  • Business logic (❌ à extraire)                            │
│  • Service calls                                             │
│  • Validation logic                                          │
└────────────────────┬────────────────────────────────────────┘
                     │ Direct manipulation
┌────────────────────▼────────────────────────────────────────┐
│                      ViewModels                              │
│  • Inherit from MicroMvvm.ObservableObject                  │
│  • Manual property notification                              │
│  • Commands created on each access                           │
│  • ~150 properties, ~30 commands                            │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                   MicroMvvm Framework                        │
│  • ObservableObject (INotifyPropertyChanged)                │
│  • RelayCommand / RelayCommand<T>                           │
│  • Manual RaisePropertyChanged()                            │
└─────────────────────────────────────────────────────────────┘
```

**Problèmes identifiés:**
- ❌ Boilerplate répétitif pour chaque propriété (getter/setter/notification)
- ❌ Commandes recréées à chaque accès (pas de cache)
- ❌ Pas de support async moderne
- ❌ Logique métier dans code-behind (difficile à tester)
- ❌ UIEventBroker custom (non-standard)

### Architecture Cible (CommunityToolkit.Mvvm)

```
┌─────────────────────────────────────────────────────────────┐
│                        WPF Views                             │
│  (MainWindow.xaml, UserControls/*.xaml, Dialogs/*.xaml)    │
└────────────────────┬────────────────────────────────────────┘
                     │ Data Binding
                     │ Command Binding
┌────────────────────▼────────────────────────────────────────┐
│              Code-Behind (.xaml.cs) - Minimal                │
│  • UI logic only (focus, animations, visual states)         │
│  • ~70% reduction in LOC                                    │
└─────────────────────────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                ViewModels (Migrated)                         │
│  • Inherit from CommunityToolkit ObservableObject           │
│  • [ObservableProperty] attributes → auto-generated props   │
│  • [RelayCommand] attributes → auto-generated commands      │
│  • Business logic extracted from code-behind                │
│  • Fully testable without UI dependencies                   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              CommunityToolkit.Mvvm (v8.3.2+)                │
│  • ObservableObject + Source Generators                     │
│  • RelayCommand / AsyncRelayCommand                         │
│  • ObservableValidator (validation)                         │
│  • WeakReferenceMessenger (communication)                   │
└─────────────────────────────────────────────────────────────┘
```

**Avantages:**
- ✅ 60-70% moins de boilerplate via source generators
- ✅ Support async natif avec AsyncRelayCommand
- ✅ Validation intégrée avec ObservableValidator
- ✅ Messenger pattern standardisé
- ✅ Testabilité maximale (logique dans ViewModels)
- ✅ Standard Microsoft officiel et maintenu

### Architecture de Transition (Coexistence)

Durant la Phase 1 et Phase 2, les deux frameworks coexistent:

```
┌─────────────────────────────────────────────────────────────┐
│                      ViewModels                              │
│                                                              │
│  ┌──────────────────────┐  ┌──────────────────────┐        │
│  │  MicroMvvm VMs       │  │ CommunityToolkit VMs │        │
│  │  (Legacy)            │  │ (Migrated)           │        │
│  │  • 10 ViewModels     │  │ • 1 ViewModel        │        │
│  │  • Manual code       │  │ • Source generated   │        │
│  └──────────────────────┘  └──────────────────────┘        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
         │                              │
         │                              │
┌────────▼──────────┐         ┌────────▼──────────────────────┐
│  MicroMvvm        │         │  CommunityToolkit.Mvvm        │
│  (Custom)         │         │  (NuGet Package)              │
└───────────────────┘         └───────────────────────────────┘
```

**Garanties de coexistence:**
- Pas de conflits de namespaces (MicroMvvm vs CommunityToolkit)
- Pas de conflits de noms de types (ObservableObject dans namespaces différents)
- Compilation sans erreurs ni warnings
- Migration ViewModel par ViewModel sans impact sur les autres

## Components and Interfaces

### Phase 1 Components

#### 1.1 Enhanced MicroMvvm.ObservableObject

**Location:** `BIA.ToolKit.Application/ViewModel/MicroMvvm/ObservableObject.cs`

**New Method:**
```csharp
protected bool SetProperty<T>(
    ref T field, 
    T value, 
    [CallerMemberName] string propertyName = null)
{
    if (EqualityComparer<T>.Default.Equals(field, value))
        return false;
    
    field = value;
    RaisePropertyChanged(propertyName);
    return true;
}
```

**Signature:**
- Generic method supporting all types
- `ref T field`: Reference to backing field (modified in-place)
- `T value`: New value to set
- `string propertyName`: Auto-captured via CallerMemberName
- Returns `bool`: true if changed, false if equal

**Behavior:**
- Uses `EqualityComparer<T>.Default` for equality (handles null, value types, reference types)
- Short-circuits if values are equal (performance optimization)
- Raises PropertyChanged only when value actually changes
- Thread-safe for single-threaded WPF dispatcher

#### 1.2 AsyncRelayCommand

**Location:** `BIA.ToolKit.Application/ViewModel/MicroMvvm/AsyncRelayCommand.cs` (new file)

**Interface:**
```csharp
public class AsyncRelayCommand : ICommand
{
    public bool IsExecuting { get; }
    public Exception LastException { get; }
    public Task ExecuteAsync(object parameter);
    public void Cancel();
}
```

**Key Features:**
- Implements `ICommand` for XAML binding
- `IsExecuting` property for UI feedback (spinner, disable button)
- Automatic CanExecute management (false during execution)
- Exception capture and exposure
- CancellationToken support
- Prevents concurrent execution (ignores duplicate calls)

**State Machine:**
```
[Idle] --Execute--> [Executing] --Complete--> [Idle]
                         │
                         └--Cancel--> [Idle]
```

#### 1.3 Migration Pilot ViewModel

**Target:** `LogDetailUC` (lowest complexity, minimal dependencies)

**Before (MicroMvvm):**
```csharp
public class LogDetailViewModel : ObservableObject
{
    private string _logMessage;
    public string LogMessage
    {
        get => _logMessage;
        set
        {
            _logMessage = value;
            RaisePropertyChanged(nameof(LogMessage));
        }
    }
    
    public ICommand CloseCommand => new RelayCommand(() => Close());
}
```

**After (CommunityToolkit):**
```csharp
public partial class LogDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string _logMessage;
    
    [RelayCommand]
    private void Close() { /* ... */ }
}
```

**Generated Code (by Source Generator):**
- Public property `LogMessage` with INotifyPropertyChanged
- Public command `CloseCommand` with ICommand implementation
- Automatic caching and optimization

### Phase 2 Components

#### 2.1 Migrated ViewModels

**Migration Order (11 ViewModels):**
1. LogDetailUC (Pilot - Phase 1)
2. RepositoryResumeUC
3. VersionAndOptionUserControl
4. RepositoryFormUC
5. CustomTemplateRepositorySettingsUC
6. OptionGeneratorUC
7. CustomTemplatesRepositoriesSettingsUC
8. ModifyProjectUC
9. DtoGeneratorUC
10. CRUDGeneratorUC
11. MainWindow

**Migration Pattern per ViewModel:**
```csharp
// Step 1: Change base class
- public class MyViewModel : MicroMvvm.ObservableObject
+ public partial class MyViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject

// Step 2: Convert properties
- private string _name;
- public string Name
- {
-     get => _name;
-     set
-     {
-         _name = value;
-         RaisePropertyChanged(nameof(Name));
-     }
- }
+ [ObservableProperty]
+ private string _name;

// Step 3: Convert commands
- public ICommand SaveCommand => new RelayCommand(() => Save());
+ [RelayCommand]
+ private void Save() { /* ... */ }

// Step 4: Convert async commands
- private RelayCommand _loadCommand;
- public ICommand LoadCommand => _loadCommand ??= new RelayCommand(async () => await LoadAsync());
+ [RelayCommand]
+ private async Task LoadAsync() { /* ... */ }
```

#### 2.2 Messenger System

**Replaces:** Custom `UIEventBroker`

**Implementation:**
```csharp
// Message definition
public class SettingsUpdatedMessage
{
    public IBIATKSettings Settings { get; init; }
}

// Sender (in SettingsService)
WeakReferenceMessenger.Default.Send(new SettingsUpdatedMessage 
{ 
    Settings = newSettings 
});

// Receiver (in ViewModel)
public partial class MainViewModel : ObservableObject, 
    IRecipient<SettingsUpdatedMessage>
{
    public void Receive(SettingsUpdatedMessage message)
    {
        UpdateRepositories(message.Settings);
    }
}
```

**Message Types to Create:**
- `SettingsUpdatedMessage` (replaces OnSettingsUpdated event)
- `RepositoryChangedMessage` (replaces OnRepositoryViewModelChanged)
- `RepositoryDeletedMessage` (replaces OnRepositoryViewModelDeleted)
- `RepositoryAddedMessage` (replaces OnRepositoryViewModelAdded)
- `OpenRepositoryFormMessage` (replaces RequestOpenRepositoryForm)

#### 2.3 Validation System

**For ViewModels with Input Validation:**

```csharp
public partial class RepositoryFormViewModel : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "Le nom est requis")]
    [MinLength(3, ErrorMessage = "Le nom doit contenir au moins 3 caractères")]
    private string _repositoryName;
    
    [ObservableProperty]
    [Required]
    [Url(ErrorMessage = "URL invalide")]
    private string _repositoryUrl;
    
    partial void OnRepositoryNameChanged(string value)
    {
        ValidateProperty(value, nameof(RepositoryName));
    }
}
```

**XAML Binding:**
```xml
<TextBox Text="{Binding RepositoryName, UpdateSourceTrigger=PropertyChanged}" />
<TextBlock Text="{Binding Errors[RepositoryName]}" 
           Visibility="{Binding HasErrors, Converter={StaticResource BoolToVisibility}}" />
```

### Phase 3 Components

#### 3.1 Test Infrastructure

**Project Structure:**
```
BIA.ToolKit.Tests/
├── ViewModels/
│   ├── MainViewModelTests.cs
│   ├── CRUDGeneratorViewModelTests.cs
│   └── ... (11 test files)
├── Commands/
│   ├── AsyncRelayCommandTests.cs
│   └── RelayCommandCachingTests.cs
├── Helpers/
│   └── TestHelpers.cs
└── BIA.ToolKit.Tests.csproj
```

**Test Dependencies:**
```xml
<PackageReference Include="xUnit" Version="2.9.0" />
<PackageReference Include="xUnit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="Moq" Version="4.20.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

## Data Models

### ViewModel Inventory

**11 ViewModels à migrer:**

| ViewModel | Complexité | Propriétés | Commandes | Dépendances | Phase Migration |
|-----------|------------|------------|-----------|-------------|-----------------|
| LogDetailUC | Faible | 5 | 2 | Aucune | Phase 1 (Pilot) |
| RepositoryResumeUC | Faible | 8 | 1 | RepositoryViewModel | Phase 2.1 |
| VersionAndOptionUserControl | Faible | 6 | 3 | SettingsService | Phase 2.1 |
| RepositoryFormUC | Moyenne | 12 | 4 | GitService, Validation | Phase 2.2 |
| CustomTemplateRepositorySettingsUC | Moyenne | 10 | 3 | RepositoryViewModel | Phase 2.2 |
| OptionGeneratorUC | Moyenne | 15 | 5 | FileGenerator | Phase 2.2 |
| CustomTemplatesRepositoriesSettingsUC | Moyenne | 8 | 6 | Collection management | Phase 2.3 |
| ModifyProjectUC | Moyenne | 18 | 7 | Multiple services | Phase 2.3 |
| DtoGeneratorUC | Haute | 25 | 8 | CSharpParser, Roslyn | Phase 2.4 |
| CRUDGeneratorUC | Haute | 30 | 10 | CSharpParser, Roslyn | Phase 2.4 |
| MainWindow | Haute | 20 | 12 | Orchestration globale | Phase 2.5 |

### Migration State Tracking

**Per-ViewModel Migration Checklist:**
```csharp
public class ViewModelMigrationStatus
{
    public string ViewModelName { get; set; }
    public bool BaseClassMigrated { get; set; }
    public int PropertiesMigrated { get; set; }
    public int TotalProperties { get; set; }
    public int CommandsMigrated { get; set; }
    public int TotalCommands { get; set; }
    public bool CodeBehindExtracted { get; set; }
    public bool TestsCreated { get; set; }
    public bool ValidationAdded { get; set; }
    public bool MessengerIntegrated { get; set; }
    public DateTime? MigrationDate { get; set; }
    public bool RegressionTested { get; set; }
}
```

### Message Contracts

**Message Types for Messenger Pattern:**

```csharp
// Settings messages
public record SettingsUpdatedMessage(IBIATKSettings Settings);

// Repository messages
public record RepositoryChangedMessage(
    RepositoryViewModel OldRepository, 
    RepositoryViewModel NewRepository);

public record RepositoryDeletedMessage(RepositoryViewModel Repository);

public record RepositoryAddedMessage(RepositoryViewModel Repository);

// UI navigation messages
public record OpenRepositoryFormMessage(
    RepositoryViewModel Repository, 
    RepositoryFormMode Mode);

public record ExecuteActionWithWaiterMessage(Func<Task> Action);
```

### Performance Baseline Model

**Metrics to Track:**

```csharp
public class PerformanceBaseline
{
    // Startup
    public TimeSpan StartupTime { get; set; }
    public TimeSpan StartupTimeStdDev { get; set; }
    
    // Memory
    public long WorkingSetBytes { get; set; }
    public long PrivateMemoryBytes { get; set; }
    
    // Commands (per command)
    public Dictionary<string, TimeSpan> CommandExecutionTimes { get; set; }
    
    // Property notification
    public TimeSpan PropertyNotificationTime { get; set; }
    
    // Build
    public TimeSpan BuildTime { get; set; }
    
    // Metadata
    public DateTime MeasuredAt { get; set; }
    public string DotNetVersion { get; set; }
    public string HardwareSpecs { get; set; }
}
```

**Thresholds:**
- Startup: +5% max
- Memory: +10% max
- Command execution: +5% max
- Property notification: <1ms absolute
- Build time: +2s max


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After analyzing all acceptance criteria, I identified the following testable properties and performed redundancy elimination:

**Redundancy Analysis:**
- Properties 1.2 and 1.3 (SetProperty behavior) are complementary and both needed - one tests idempotence, the other tests change detection
- Property 2.1 (command caching) is the core property; 2.3 is a weaker version that can be subsumed
- Properties 3.2 and 3.3 (async command CanExecute) form a round-trip and should both be kept
- Property 5.5 (pilot equivalence) and 7.4 (API compatibility) test similar concepts but at different scopes - both needed
- Properties 8.4 and 8.6 (extracted logic testability and behavior) are complementary - both needed
- Properties 11.3 and 11.4 (validation errors) form a round-trip and should both be kept
- Property 14.1 (behavior invariant) subsumes 14.4 (command behavior) - can combine into one comprehensive property

**Eliminated Redundancies:**
- Property 2.3 eliminated (subsumed by 2.1)
- Property 14.4 eliminated (subsumed by 14.1)

### Property 1: SetProperty Idempotence

*For any* ObservableObject instance and any property with a backing field, when SetProperty is called twice with the same value, the second call SHALL return false and SHALL NOT raise PropertyChanged.

**Validates: Requirements 1.2**

**Pattern:** Idempotence - doing the same operation twice has no additional effect

**Test Strategy:** Generate random values of various types (int, string, object, null), call SetProperty twice, verify second call returns false and PropertyChanged event count is 1.

### Property 2: SetProperty Change Detection

*For any* ObservableObject instance and any two different values, when SetProperty is called with a new value different from the current field value, it SHALL update the field, raise PropertyChanged exactly once, and return true.

**Validates: Requirements 1.3**

**Pattern:** State transformation with notification

**Test Strategy:** Generate pairs of different values, call SetProperty, verify field changed, event raised once, and return value is true.

### Property 3: SetProperty Equality Semantics

*For any* type T with custom equality, SetProperty SHALL use EqualityComparer<T>.Default for comparison, ensuring consistency with standard .NET equality semantics.

**Validates: Requirements 1.5**

**Pattern:** Conformance to standard library behavior

**Test Strategy:** Create types with custom IEquatable<T> implementations, verify SetProperty respects custom equality.

### Property 4: SetProperty Refactoring Equivalence

*For any* property migrated from manual RaisePropertyChanged to SetProperty, the PropertyChanged notification behavior SHALL be identical (same event raised, same property name, same timing).

**Validates: Requirements 1.6**

**Pattern:** Metamorphic property - refactoring preserves behavior

**Test Strategy:** Compare notification sequences before and after refactoring for sample properties.

### Property 5: Command Instance Caching

*For any* ViewModel command property, accessing the property multiple times SHALL return the exact same ICommand instance (reference equality).

**Validates: Requirements 2.1, 2.3**

**Pattern:** Idempotence - multiple accesses return same instance

**Test Strategy:** Access command properties multiple times, verify Object.ReferenceEquals returns true for all pairs.

### Property 6: AsyncRelayCommand CanExecute During Execution

*For any* AsyncRelayCommand, while the async operation is executing, CanExecute SHALL return false, and after completion (success or failure), CanExecute SHALL return to its original state.

**Validates: Requirements 3.2, 3.3**

**Pattern:** Round-trip property - state is restored after operation

**Test Strategy:** Create async commands with various CanExecute predicates, verify CanExecute is false during execution and restored after.

### Property 7: AsyncRelayCommand Exception Capture

*For any* AsyncRelayCommand that throws an exception during execution, the exception SHALL be captured and exposed via the LastException property without crashing the application.

**Validates: Requirements 3.4**

**Pattern:** Error condition handling

**Test Strategy:** Create commands that throw various exception types, verify exceptions are captured and exposed.

### Property 8: AsyncRelayCommand Concurrent Execution Prevention

*For any* AsyncRelayCommand, when Execute is called while already executing, the duplicate call SHALL be ignored and SHALL NOT start a second concurrent execution.

**Validates: Requirements 3.7**

**Pattern:** Idempotence - concurrent calls have no additional effect

**Test Strategy:** Call Execute multiple times rapidly, verify only one execution occurs.

### Property 9: AsyncRelayCommand Cancellation

*For any* AsyncRelayCommand supporting cancellation, calling Cancel during execution SHALL trigger the CancellationToken and allow the operation to terminate early.

**Validates: Requirements 3.6**

**Pattern:** Cancellation protocol

**Test Strategy:** Create long-running commands, cancel them mid-execution, verify they terminate early.

### Property 10: Migration Pilot Functional Equivalence

*For any* user interaction with the LogDetailUC view, the behavior SHALL be identical whether using the MicroMvvm or CommunityToolkit implementation (same data displayed, same commands executed, same side effects).

**Validates: Requirements 5.5**

**Pattern:** Metamorphic property - migration preserves behavior

**Test Strategy:** Create integration tests comparing both implementations with same inputs.

### Property 11: Migrated ViewModel API Compatibility

*For any* ViewModel migrated to CommunityToolkit, the public API (property names, property types, command names, command signatures) SHALL remain identical to the MicroMvvm version.

**Validates: Requirements 7.4**

**Pattern:** Interface preservation

**Test Strategy:** Use reflection to compare public members before and after migration.

### Property 12: Migrated ViewModel View Compatibility

*For all* migrated ViewModels, the associated XAML views SHALL function identically without any XAML changes (all bindings work, all commands execute, all data displays correctly).

**Validates: Requirements 7.5**

**Pattern:** Invariant - UI behavior unchanged

**Test Strategy:** Run UI automation tests against migrated ViewModels without changing XAML.

### Property 13: Extracted Logic Testability

*For any* business logic extracted from code-behind to ViewModel, the logic SHALL be testable via unit tests without requiring WPF UI components (no dependencies on UIElement, Window, UserControl, etc.).

**Validates: Requirements 8.4**

**Pattern:** Dependency inversion - logic independent of UI

**Test Strategy:** Analyze ViewModel dependencies, verify no WPF UI types in dependency graph.

### Property 14: Extracted Logic Behavioral Equivalence

*For any* logic extracted from code-behind to ViewModel, the functional behavior SHALL remain identical (same inputs produce same outputs, same side effects occur).

**Validates: Requirements 8.6**

**Pattern:** Metamorphic property - refactoring preserves behavior

**Test Strategy:** Create tests comparing behavior before and after extraction.

### Property 15: Validation Error State Property

*For all* properties with validation attributes, setting an invalid value SHALL populate the Errors collection for that property, and the HasErrors property SHALL be true.

**Validates: Requirements 11.3**

**Pattern:** Error condition detection

**Test Strategy:** Generate invalid values for validated properties, verify Errors collection is populated.

### Property 16: Validation Error Clearing Property

*For all* properties with validation attributes, setting an invalid value followed by a valid value SHALL clear the Errors collection for that property.

**Validates: Requirements 11.4**

**Pattern:** Round-trip property - invalid then valid results in no errors

**Test Strategy:** Set invalid then valid values, verify Errors collection is empty.

### Property 17: Validation Round-Trip Property

*For all* validated properties, the sequence (set valid value → set invalid value → set valid value) SHALL result in no errors and HasErrors = false.

**Validates: Requirements 11.7**

**Pattern:** Round-trip property - returning to valid state clears all errors

**Test Strategy:** Execute the sequence for all validated properties, verify clean state.

### Property 18: Validation Automatic Execution

*For any* validated property, when the property value changes, validation SHALL execute automatically without requiring explicit ValidateProperty calls.

**Validates: Requirements 11.6**

**Pattern:** Automatic side effect

**Test Strategy:** Change validated properties, verify validation executes (Errors collection updates).

### Property 19: Messenger Message Delivery

*For any* message sent via WeakReferenceMessenger, all ViewModels registered as recipients for that message type SHALL receive the message.

**Validates: Requirements 10.2**

**Pattern:** Broadcast delivery

**Test Strategy:** Register multiple recipients, send message, verify all receive it.

### Property 20: Messenger Weak Reference Cleanup

*For any* ViewModel registered with WeakReferenceMessenger, after the ViewModel is disposed and garbage collected, it SHALL NOT receive messages.

**Validates: Requirements 10.3**

**Pattern:** Resource cleanup

**Test Strategy:** Register ViewModel, dispose it, force GC, send message, verify it's not received.

### Property 21: Migration Behavioral Invariant

*For all* features in the application, the functional behavior SHALL remain identical after migration (same user interactions produce same results, same data is displayed, same side effects occur).

**Validates: Requirements 14.1, 14.4**

**Pattern:** Global invariant - migration preserves all behavior

**Test Strategy:** Execute comprehensive regression test suite comparing behavior before and after migration.

### Property 22: Data Binding Correctness

*For all* migrated ViewModels, when a property value changes, the PropertyChanged event SHALL be raised with the correct property name, and the UI SHALL reflect the new value.

**Validates: Requirements 14.5**

**Pattern:** Notification correctness

**Test Strategy:** Change properties, verify PropertyChanged event has correct property name and UI updates.

## Error Handling

### Phase 1 Error Handling

**SetProperty Method:**
- No exceptions thrown for null values (handled by EqualityComparer<T>.Default)
- No exceptions for invalid property names (CallerMemberName guarantees correctness)
- Thread-safe for WPF single-threaded dispatcher model

**AsyncRelayCommand:**
- All exceptions during async execution are captured in LastException property
- Application never crashes due to unhandled async exceptions
- CancellationToken exceptions (OperationCanceledException) are handled gracefully
- IsExecuting property always returns to false even on exception

### Phase 2 Error Handling

**Migration Process:**
- Each ViewModel migration is atomic (all-or-nothing)
- Compilation errors block migration completion
- Regression detection triggers immediate rollback
- Failed migrations documented in migration log

**Rollback Procedure:**
```
1. Detect regression (functional test failure)
2. Document regression with reproduction steps
3. Git revert to last known good commit
4. Analyze root cause
5. Update migration guide with pitfall
6. Re-attempt migration with fix
```

**Validation Errors:**
- Validation exceptions never crash the application
- Invalid validation attributes detected at compile time (source generator)
- Runtime validation errors populate Errors collection
- UI displays validation errors via binding

**Messenger Errors:**
- Unregistered message types are silently ignored (no exception)
- Exceptions in message handlers are isolated (don't affect other recipients)
- Weak reference failures (GC'd recipients) are handled gracefully

### Phase 3 Error Handling

**Performance Threshold Violations:**
- Automated performance tests fail if thresholds exceeded
- CI/CD pipeline blocks merge if performance regresses
- Performance issues investigated before proceeding
- Baseline updated only after explicit approval

**Test Failures:**
- Unit test failures block ViewModel migration completion
- Integration test failures trigger rollback
- Code coverage below 80% blocks PR approval
- Flaky tests are fixed or removed (no ignoring)

## Testing Strategy

### Dual Testing Approach

This project requires both unit tests and property-based tests for comprehensive coverage:

**Unit Tests:**
- Specific examples demonstrating correct behavior
- Edge cases (null, empty, boundary values)
- Error conditions (exceptions, invalid inputs)
- Integration points between components
- Regression tests for discovered bugs

**Property-Based Tests:**
- Universal properties holding for all inputs
- Randomized input generation (100+ iterations per property)
- Metamorphic properties (refactoring equivalence)
- Round-trip properties (serialization, state restoration)
- Invariant properties (behavior preservation)

**Complementary Nature:**
Unit tests catch concrete bugs in specific scenarios. Property tests verify general correctness across all inputs. Together they provide comprehensive coverage.

### Property-Based Testing Configuration

**Library Selection:**
- **C# / .NET**: Use **FsCheck** (mature, well-integrated with xUnit/NUnit)
- Alternative: **CsCheck** (newer, faster, better C# integration)

**Installation:**
```xml
<PackageReference Include="FsCheck" Version="2.16.6" />
<PackageReference Include="FsCheck.Xunit" Version="2.16.6" />
```

**Configuration:**
- Minimum 100 iterations per property test (due to randomization)
- Configurable via `[Property(MaxTest = 100)]` attribute
- Seed-based reproducibility for failed tests
- Shrinking enabled to find minimal failing case

**Test Tagging:**
Each property-based test MUST include a comment referencing the design property:

```csharp
[Property(MaxTest = 100)]
public Property SetProperty_Idempotence_ReturnsFlaseOnSecondCall()
{
    // Feature: mvvm-modernization, Property 1: SetProperty Idempotence
    // For any ObservableObject instance and any property with a backing field,
    // when SetProperty is called twice with the same value, the second call
    // SHALL return false and SHALL NOT raise PropertyChanged.
    
    return Prop.ForAll<int>(value =>
    {
        var vm = new TestViewModel();
        var eventCount = 0;
        vm.PropertyChanged += (s, e) => eventCount++;
        
        var firstResult = vm.SetTestProperty(value);
        var secondResult = vm.SetTestProperty(value);
        
        return secondResult == false && eventCount == 1;
    });
}
```

### Test Organization

**Project Structure:**
```
BIA.ToolKit.Tests/
├── Unit/
│   ├── ViewModels/
│   │   ├── MainViewModelTests.cs
│   │   ├── CRUDGeneratorViewModelTests.cs
│   │   └── ... (11 test files)
│   ├── Commands/
│   │   ├── AsyncRelayCommandTests.cs
│   │   └── RelayCommandCachingTests.cs
│   └── Validation/
│       └── ValidationTests.cs
├── Properties/
│   ├── SetPropertyTests.cs
│   ├── CommandCachingTests.cs
│   ├── AsyncCommandTests.cs
│   ├── MigrationEquivalenceTests.cs
│   ├── ValidationTests.cs
│   └── MessengerTests.cs
├── Integration/
│   ├── ViewModelIntegrationTests.cs
│   └── MessengerIntegrationTests.cs
└── Performance/
    ├── BaselineTests.cs
    └── RegressionTests.cs
```

### Test Coverage Requirements

**Per ViewModel:**
- ✅ Property notification tests (all properties)
- ✅ Command execution tests (all commands)
- ✅ Async command tests (execution, cancellation, exceptions)
- ✅ Validation tests (if applicable)
- ✅ Messenger tests (if applicable)
- ✅ Integration tests (ViewModel + View)

**Coverage Targets:**
- Unit test coverage: ≥80% for ViewModel logic
- Property test coverage: All 22 correctness properties
- Integration test coverage: All 11 ViewModels
- Performance test coverage: All baseline metrics

### Testing Tools

**Unit Testing:**
- xUnit 2.9.0 (test framework)
- Moq 4.20.0 (mocking)
- FluentAssertions 6.12.0 (assertions)

**Property-Based Testing:**
- FsCheck 2.16.6 (property-based testing)
- FsCheck.Xunit 2.16.6 (xUnit integration)

**Performance Testing:**
- BenchmarkDotNet 0.13.12 (microbenchmarks)
- Custom performance harness (startup, memory)

**Code Coverage:**
- Coverlet 6.0.0 (coverage collection)
- ReportGenerator 5.2.0 (coverage reports)

### Test Execution Strategy

**Phase 1:**
1. Create test project
2. Write property tests for SetProperty (Properties 1-4)
3. Write property tests for AsyncRelayCommand (Properties 6-9)
4. Write unit tests for pilot ViewModel
5. Write integration test for pilot ViewModel (Property 10)

**Phase 2:**
1. Write unit tests for each ViewModel before migration
2. Migrate ViewModel
3. Run unit tests (verify no regression)
4. Write property tests for API compatibility (Property 11)
5. Write integration tests for view compatibility (Property 12)
6. Write property tests for extracted logic (Properties 13-14)
7. Write property tests for validation (Properties 15-18)
8. Write property tests for Messenger (Properties 19-20)

**Phase 3:**
1. Run comprehensive regression test suite (Property 21)
2. Run data binding tests (Property 22)
3. Run performance baseline tests
4. Generate coverage reports
5. Document test results

### Continuous Integration

**CI Pipeline:**
```yaml
- Build solution
- Run unit tests (fail on any failure)
- Run property tests (fail on any failure)
- Run integration tests (fail on any failure)
- Collect code coverage (fail if <80%)
- Run performance tests (fail if thresholds exceeded)
- Generate reports
```

**PR Requirements:**
- All tests pass
- Code coverage ≥80%
- No performance regressions
- Migration checklist completed (if applicable)

