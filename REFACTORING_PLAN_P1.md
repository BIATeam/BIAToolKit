# Plan de Refactorisation P1 - Axes d'Amélioration MVVM

**Date:** 20 Janvier 2026  
**Status:** À EXÉCUTER  
**Priorité:** P1 (Important, après P0 qui est COMPLÉTÉ)

---

## 📋 Contexte

Après la refactorisation **P0** complétée (ModifyProjectUC + VersionAndOptionUserControl), le codebase a atteint **85% de conformité MVVM**. Ce document détaille les **3 axes prioritaires** pour atteindre **95%+ de conformité**.

**P0 Complété:**
- ✅ ModifyProjectUC.xaml.cs: 81 → 44 lignes (-45.7%)
- ✅ VersionAndOptionUserControl.xaml.cs: 36 → 29 lignes (-19.4%)
- ✅ Supprimé: 2 wirings lambda, 2 event handlers UI, 6 accès directs ViewModel
- ✅ Build: CLEAN ✓

---

## 🎯 Axes P1 à Exécuter

### **AXE 1: DtoGeneratorUC - Remplacer Callback par Behavior [1h]**

**Fichier:** `BIA.ToolKit/UserControls/DtoGeneratorUC.xaml.cs`

**Problème:**
```csharp
vm.RequestResetMappingColumnsWidths = ResetMappingColumnsWidths;  // ← Couplage fort
```

**Solution:**
1. Créer `ResetColumnsWidthBehavior<T>` pour GridView
2. Remplacer le wiring lambda par un binding Behavior
3. Supprimer la propriété `RequestResetMappingColumnsWidths` du ViewModel
4. Garder la logique dans le Behavior, pas dans le code-behind

**Résultat Attendu:**
- Code-behind: 70 → ~20 lignes
- Zéro callback/delegate
- Pur MVVM

**Steps:**
1. Lire `DtoGeneratorUC.xaml.cs` complet (actuellement 70 lignes)
2. Créer `Behaviors/ResetColumnsWidthBehavior.cs`
3. Refactor `DtoGeneratorUC.xaml.cs` pour utiliser le Behavior
4. Test compilation + build

---

### **AXE 2: LogDetailUC - DialogService Wrapper [45m]**

**Fichier:** `BIA.ToolKit/Dialogs/LogDetailUC.xaml.cs`

**Problème:**
```csharp
public bool? ShowDialog(List<ConsoleWriter.Message> messages)  // ← Custom ShowDialog
{
    Messages = messages;
    foreach (ConsoleWriter.Message msg in messages)
        ConsoleWriter.AddMsgLine(...);  // ← Logique de formatage dans code-behind
    return ShowDialog();
}
```

**Solution:**
1. Créer `LogDetailViewModel` pour gérer les messages et le formatage
2. Créer `LogDetailDialogService` (ou ajouter à DialogService existant)
3. Exposer via DialogService: `ShowLogDetailsAsync(messages)`
4. Code-behind ne contient que DI + DataContext

**Résultat Attendu:**
- Code-behind: 22 → ~8 lignes
- Logique formatage: code-behind → ViewModel
- Dialog service centralisé

**Steps:**
1. Lire `LogDetailUC.xaml.cs` complet
2. Créer `LogDetailViewModel` dans Application.ViewModel
3. Créer extension `IDialogService.ShowLogDetailsAsync()`
4. Refactor `LogDetailUC.xaml.cs`
5. Mettre à jour les appels depuis `MainWindow`
6. Test compilation + build

---

### **AXE 3: RepositoryFormUC - DI Complète [1.5h]**

**Fichier:** `BIA.ToolKit/Dialogs/RepositoryFormUC.xaml.cs`

**Problème:**
```csharp
// RepositoryFormViewModel créé manuellement
DataContext = new RepositoryFormViewModel(repository, gitService, messenger, consoleWriter);

// fileDialogService avec fallback manuel
this.fileDialogService = fileDialogService ?? new Infrastructure.Services.FileDialogService();
```

**Solution:**
1. Faire `RepositoryFormUC` une vraie `UserControl` (hériter de UserControl, pas Window)
2. DI complet: injecter `RepositoryFormViewModel` au lieu de le créer
3. DI le `IFileDialogService` sans fallback (résoudre via DI container)
4. Mettre à jour MainWindow pour utiliser DialogService

**Résultat Attendu:**
- Code-behind: 68 → ~15 lignes
- Zéro création d'objets
- DI complète
- Testable

**Steps:**
1. Lire `RepositoryFormUC.xaml.cs` complet
2. Changer héritage: `Window` → `UserControl`
3. Refactor constructor pour DI du ViewModel
4. Déplacer logique browse buttons vers RepositoryFormViewModel
5. Créer `IWindowDialogService` ou adapter `DialogService`
6. Mettre à jour AppSettings DI
7. Tester MainWindow appels
8. Test compilation + build

---

## 🔄 Autres Améliorations Optionnelles (P2)

| Fichier | Priorité | Effort | Impact | Type |
|---------|----------|--------|--------|------|
| **RepositoryResumeUC.xaml.cs** | 🟢 | 5m | Très faible | Vérifier que c'est vide, bon état |
| **CustomsRepoTemplateUC.xaml.cs** | 🟡 | 2h | Moyen | TODO impl + DI DialogService |
| **CustomRepoTemplateUC.xaml.cs** | 🟡 | 45m | Moyen | Mettre à jour après CustomsRepoTemplateUC |
| **DtoGeneratorViewModel** | 🟡 | 1h | Moyen | Supprimer `RequestResetMappingColumnsWidths` (après AXE 1) |

---

## 📊 Métriques de Succès

| Métrique | Actuel (P0) | Cible (P1) | Status |
|----------|-----------|-----------|--------|
| **Conformité MVVM** | 85% | 95% | À atteindre |
| **Code-behind moyen (lignes)** | 40 | <25 | À atteindre |
| **Zéro delegate/callback** | ❌ 1 (Dto) | ✅ 0 | À atteindre |
| **100% DI résolue** | 75% | 95% | À atteindre |
| **Build Clean** | ✅ | ✅ | Maintenir |

---

## 🛠️ Checklist d'Exécution

### Préparation
- [ ] Créer branche feature: `refactor/P1-mvvm-axes`
- [ ] Cet fichier: REFACTORING_PLAN_P1.md ✅

### AXE 1: DtoGeneratorUC
- [ ] Créer `Behaviors/ResetColumnsWidthBehavior.cs`
- [ ] Refactor `DtoGeneratorUC.xaml.cs`
- [ ] Supprimer wiring lambda
- [ ] Build clean
- [ ] Commit: `refactor(DtoGeneratorUC): remove callback, use Behavior`

### AXE 2: LogDetailUC
- [ ] Créer `LogDetailViewModel` (Application layer)
- [ ] Créer extension `IDialogService.ShowLogDetailsAsync()`
- [ ] Refactor `LogDetailUC.xaml.cs`
- [ ] Mettre à jour appelants (MainWindow, etc.)
- [ ] Build clean
- [ ] Commit: `refactor(LogDetailUC): move logic to ViewModel`

### AXE 3: RepositoryFormUC
- [ ] Changer `Window` → `UserControl`
- [ ] Refactor constructor (DI du ViewModel)
- [ ] Créer DialogService pour repository forms
- [ ] Mettre à jour MainWindow
- [ ] Déplacer logique browse vers ViewModel
- [ ] Build clean
- [ ] Commit: `refactor(RepositoryFormUC): complete DI`

### Finalisation
- [ ] Tous les builds: CLEAN ✓
- [ ] Métriques P1 atteintes
- [ ] Merger dans develop
- [ ] Documentation mise à jour

---

## 📝 Notes pour le Développement

### Points à Respecter
1. **Zéro breaking change** - interfaces restent compatibles
2. **Build après chaque axe** - valider incrémentalement
3. **Commits atomiques** - un commit par axe
4. **Tests fonctionnels** - vérifier navigation/dialogs

### Anti-Patterns à Éviter
- ❌ Créer des objets dans code-behind (utiliser DI)
- ❌ Appeler directement ViewModel depuis code-behind (déléguer via events)
- ❌ Logique UI dans ViewModel (garder Commands/properties uniquement)
- ❌ Couplage code-behind ↔ ViewModel (passer par DataContext)

### Code Pattern à Respecter
```csharp
// ✅ BON: Code-behind minimal
public partial class MyUC : UserControl
{
    private readonly MyViewModel vm;
    
    public MyUC(MyViewModel viewModel)
    {
        InitializeComponent();
        vm = viewModel;
        DataContext = vm;
    }
    
    // Événement simple → délégation ViewModel
    private void SomeEvent_Handler(object s, EventArgs e)
    {
        vm.HandleSomeEvent();
    }
}

// ❌ MAUVAIS: Couplage fort
public partial class MyUC : UserControl
{
    public MyUC()
    {
        InitializeComponent();
        DataContext = new MyViewModel();  // ← Création manuelle
    }
    
    private void SomeEvent_Handler(object s, EventArgs e)
    {
        var vm = (MyViewModel)DataContext;
        vm.SomeProperty = "value";  // ← Logique métier
    }
}
```

---

## 📞 Dépendances et Interactions

- **AXE 1** → indépendant
- **AXE 2** → dépend de IDialogService existant
- **AXE 3** → dépend de DialogService (peut être fait en // avec AXE 2)

**Ordre Recommandé:**
1. AXE 2 (créer IDialogService extension)
2. AXE 1 (indépendant, rapide)
3. AXE 3 (dépend de DialogService)

---

## 🎓 Apprentissages à Retenir

### De P0:
- Déplacer façade methods vers ViewModel public methods
- Event handlers peuvent rester si simples (délégation)
- Wirings lambda → gestion dans ViewModel
- TextChanged/SelectionChanged doivent être délégués

### Pour P1:
- Callbacks/delegates → Behaviors ou Properties
- Dialog custom ShowDialog() → DialogService wrapper
- Créations manuelles → toujours DI

---

**Dernière Mise à Jour:** 20 Janvier 2026  
**Prochaine Révision:** Après P1 complété
