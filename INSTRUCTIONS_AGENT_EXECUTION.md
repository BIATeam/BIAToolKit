# 🤖 INSTRUCTIONS POUR AGENT DÉDIÉ D'EXÉCUTION

**Cible**: Exécution complète du plan de restauration BIAToolKit  
**Baseline**: 6a721cfe1d3af3ecb5f18f295072ef1c465c7e79  
**Durée estimée**: 11-14.5 heures  
**Complexité**: Moyenne-Élevée

---

## 🎯 MISSION PRINCIPALE

Restaurer l'application BIAToolKit de 43% à 100% d'opérationnalité en implémentant:
- 8 commandes manquantes
- 2 message handlers manquants
- 3 TODOs dans CustomTemplates
- Correction des NotImplementedExceptions
- Tests de validation

---

## 📋 PHASAGE DÉTAILLÉ

### ✅ PHASE 0: PRÉPARATION INITIALE (30-45 min)

#### Étape 0.1: Validation environnement
```powershell
cd c:\sources\Github\BIAToolKit
# Vérifier Git
git status
git log --oneline -1

# Vérifier .NET
dotnet --version

# Test compilation
dotnet clean BIAToolKit.sln
dotnet build BIAToolKit.sln --configuration Debug
```

**Critères d'acceptation**:
- ✅ Compilation sans erreur
- ✅ Version .NET 6.0 ou supérieur
- ✅ Repository sur branche correcte

#### Étape 0.2: Créer branche de travail
```powershell
git checkout -b feature/restore-missing-implementations
git push -u origin feature/restore-missing-implementations
```

**Critères d'acceptation**:
- ✅ Branche créée et pushée
- ✅ Confirmée sur GitHub

#### Étape 0.3: Audit CRITIQUE Phase 1A
⚠️ **IMPORTANT**: Faire cet audit AVANT de procéder Phase 1A

```powershell
# 1. Lister toutes les commandes existantes
grep -n "Command" BIA.ToolKit/ViewModels/MainViewModel.cs | findstr /I "public\|private"

# 2. Identifier les AsyncRelayCommand vs RelayCommand
grep -n "AsyncRelayCommand\|RelayCommand" BIA.ToolKit/ViewModels/MainViewModel.cs

# 3. Identifier les methods async manquantes
grep -n "throw new NotImplementedException\|TODO" BIA.ToolKit/ViewModels/MainViewModel.cs
```

**Résultat attendu**:
- Liste exacte des commandes présentes
- Liste exacte des commandes manquantes
- Comparaison avec le PLAN_EXECUTION.md
- **Reporter les écarts** dans EXECUTION_NOTES.md

#### Étape 0.4: Audit CRITIQUE Phase 2A
```powershell
# Localiser CustomTemplatesRepositoriesSettingsUC
Get-ChildItem -Recurse -Path "BIA.ToolKit" -Filter "*CustomTemplate*" -Include "*.xaml*"
Get-ChildItem -Recurse -Path "BIA.ToolKit" -Filter "*Settings*" -Include "*.xaml.cs" | Select-Object FullName

# Lister tous les UserControls
Get-ChildItem "BIA.ToolKit/UserControls" -Filter "*.xaml.cs" | Select-Object Name
```

**Résultat attendu**:
- Localisation exacte du fichier (ou confirmation qu'il n'existe pas)
- Si manquant: Adapter Phase 2A
- **Reporter dans EXECUTION_NOTES.md**

#### Étape 0.5: Audit des NotImplementedExceptions
```powershell
# Trouver tous les throw NotImplementedException
grep -r "throw new NotImplementedException" BIA.ToolKit.Application/Services/ | `
  Select-Object { $_ -split ':' | Select-Object -First 1 } -Unique
```

**Résultat attendu**:
- Fichiers exacts à modifier
- Nombre de NotImplementedExceptions par fichier

---

### 🔴 PHASE 1: DÉBLOCAGE CRITIQUE (5-5.5 heures)

#### PHASE 1A: Restaurer les 8 Commandes (2-2.5h)

**PRÉ-REQUIS**: Avoir complété l'audit 0.3

**Fichier à modifier**: [BIA.ToolKit/ViewModels/MainViewModel.cs](BIA.ToolKit/ViewModels/MainViewModel.cs)

**Stratégie**:
1. Basé sur l'audit, identifier les commandes manquantes RÉELLES
2. Adapter le code suggéré du PLAN_EXECUTION.md à la structure actuelle
3. Implémenter 1 commande à la fois
4. Compiler après chaque implémentation
5. Commit Git après chaque 2 commandes

**Structure d'implémentation pour chaque commande**:
```csharp
// 1. Dans le constructeur, ajouter:
CommandName = new AsyncRelayCommand(CommandNameAsync);

// 2. En bas de la classe, ajouter la méthode:
private async Task CommandNameAsync()
{
    try
    {
        // Implémentation spécifique
    }
    catch (Exception ex)
    {
        consoleWriter.AddMessageLine($"Erreur: {ex.Message}", "red");
    }
}
```

**Tâches détaillées**:

##### 1A.1 - RefreshCommand (20 min)
- [ ] Ajouter initialisation dans constructeur
- [ ] Implémenter RefreshAsync()
- [ ] Compiler et valider
- [ ] Commit: `feat: add RefreshCommand`

```csharp
RefreshCommand = new AsyncRelayCommand(RefreshAsync);

private async Task RefreshAsync()
{
    try
    {
        // Basé sur la structure existante du projet
        // Implémenter le refresh des repositories
    }
    catch (Exception ex)
    {
        consoleWriter.AddMessageLine($"Erreur lors du refresh: {ex.Message}", "red");
    }
}
```

##### 1A.2 - AddRepositoryCommand (20 min)
- [ ] Ajouter avec paramètre Repository
- [ ] Implémenter AddRepositoryAsync(Repository)
- [ ] Compiler et valider
- [ ] Commit: `feat: add AddRepositoryCommand`

##### 1A.3 - RemoveRepositoryCommand (20 min)
- [ ] Implémenter avec confirmation dialog
- [ ] Nettoyer les collections
- [ ] Compiler et valider
- [ ] Commit: `feat: add RemoveRepositoryCommand`

##### 1A.4 - UpdateRepositoryCommand (20 min)
- [ ] Basé sur EventBroker_OnRepositoryChanged (déjà existant)
- [ ] Adapter la logique
- [ ] Compiler et valider
- [ ] Commit: `feat: add UpdateRepositoryCommand`

##### 1A.5 - GenerateProjectCommand (30 min)
- [ ] Implémenter la génération complète
- [ ] Ajouter flag IsGenerating
- [ ] Observer pattern de GenerateCrudService
- [ ] Compiler et valider
- [ ] Commit: `feat: add GenerateProjectCommand`

##### 1A.6 - AnalyzeProjectCommand (30 min)
- [ ] Créer dialog de sélection de projet
- [ ] Implémenter l'analyse
- [ ] Afficher les résultats
- [ ] Compiler et valider
- [ ] Commit: `feat: add AnalyzeProjectCommand`

##### 1A.7 - ExportSettingsCommand (20 min)
- [ ] Implémenter export JSON
- [ ] Utiliser SettingsService
- [ ] Compiler et valider
- [ ] Commit: `feat: add ExportSettingsCommand`

##### 1A.8 - ImportSettingsCommand (20 min)
- [ ] Implémenter import JSON
- [ ] Valider le format
- [ ] Rafraîchir l'UI
- [ ] Compiler et valider
- [ ] Commit: `feat: add ImportSettingsCommand`

**Checkpoint Phase 1A**:
```powershell
# Validation
dotnet clean BIAToolKit.sln
dotnet build BIAToolKit.sln --configuration Debug

# Devrait compiler sans erreur
# 0 erreurs, 0 warnings critiques
```

---

#### PHASE 1B: Restaurer les 2 Message Handlers (1h)

**Fichier à modifier**: [BIA.ToolKit/ViewModels/MainViewModel.cs](BIA.ToolKit/ViewModels/MainViewModel.cs)

**Tâches**:

##### 1B.1 - RefreshRepositoriesMessageHandler (20 min)
- [ ] Enregistrer dans constructeur:
```csharp
messenger.Register<RefreshRepositoriesMessage>(this, (r, m) => OnRefreshRepositoriesMessage(m));
```
- [ ] Implémenter OnRefreshRepositoriesMessage
- [ ] Compiler et valider
- [ ] Commit: `feat: add RefreshRepositoriesMessage handler`

##### 1B.2 - GenerationCompletedMessageHandler (20 min)
- [ ] Enregistrer le handler
- [ ] Implémenter la logique
- [ ] Compiler et valider
- [ ] Commit: `feat: add GenerationCompletedMessage handler`

##### 1B.3 - Cleanup (20 min)
- [ ] Implémenter Dispose() ou Cleanup()
- [ ] Unregister tous les handlers
- [ ] Compiler et valider
- [ ] Commit: `feat: add message handler cleanup`

**Checkpoint Phase 1B**:
```powershell
dotnet build BIAToolKit.sln --configuration Debug
# Compiler sans erreur
```

---

#### PHASE 1C: Tests et Validation (1.5-2h)

##### Test 1: Compilation complète
```powershell
dotnet clean BIAToolKit.sln
dotnet build BIAToolKit.sln --configuration Debug
# Résultat attendu: SUCCESS, 0 erreurs
```

##### Test 2: Tests unitaires
```powershell
dotnet test BIAToolKit.sln --configuration Debug --logger "console;verbosity=detailed"
# Résultat attendu: ALL TESTS PASSED
```

##### Test 3: Tests manuels UI
- [ ] Lancer: `dotnet run --project BIA.ToolKit/BIA.ToolKit.csproj`
- [ ] Tester chaque commande restaurée
- [ ] Vérifier qu'aucune erreur UI
- [ ] Vérifier que les collections se mettent à jour

##### Test 4: Code review
- [ ] Vérifier cohérence avec codebase existant
- [ ] Vérifier gestion des erreurs (try-catch)
- [ ] Vérifier conventions de nommage
- [ ] Vérifier logging approprié

**Checkpoint Phase 1 FINAL**:
```powershell
git status
# Commit final
git commit -m "test: Phase 1 complete - 8 commands + 2 handlers + validation"
git log --oneline -5
```

---

### 🟠 PHASE 2: COMPLÉTION FONCTIONNALITÉS (3-4h)

#### PHASE 2A: TODOs CustomTemplates (1.5-2h)

**PRÉ-REQUIS**: Avoir localisé le fichier exact (audit 0.4)

**Fichier à modifier**: Résultat de l'audit (possible renommage)

**Tâches**:

##### 2A.1 - LoadCustomTemplates (40 min)
- [ ] Localiser le TODO dans le code
- [ ] Implémenter LoadCustomTemplatesAsync()
- [ ] Compiler et valider
- [ ] Test manuel: Vérifier le chargement
- [ ] Commit: `feat: implement LoadCustomTemplates`

##### 2A.2 - SaveCustomTemplate (40 min)
- [ ] Implémenter SaveCustomTemplateAsync()
- [ ] Ajouter validations
- [ ] Compiler et valider
- [ ] Test manuel: Ajouter un template
- [ ] Commit: `feat: implement SaveCustomTemplate`

##### 2A.3 - DeleteCustomTemplate (40 min)
- [ ] Implémenter DeleteCustomTemplateAsync()
- [ ] Ajouter confirmation dialog
- [ ] Compiler et valider
- [ ] Test manuel: Supprimer un template
- [ ] Commit: `feat: implement DeleteCustomTemplate`

**Checkpoint Phase 2A**: 
```powershell
dotnet build BIAToolKit.sln --configuration Debug
```

---

#### PHASE 2B: NotImplementedExceptions (1.5-2h)

**PRÉ-REQUIS**: Avoir audit des fichiers (audit 0.5)

**Tâches par fichier**:

##### 2B.1 - CRUDGenerationService.cs (60 min)

Fichier: [BIA.ToolKit.Application/Services/CRUD/CRUDGenerationService.cs](BIA.ToolKit.Application/Services/CRUD/CRUDGenerationService.cs)

- [ ] Localiser `throw new NotImplementedException()`
- [ ] Implémenter GenerateControllerAsync()
- [ ] Implémenter GenerateServiceAsync()
- [ ] Implémenter GenerateRepositoryAsync()
- [ ] Compiler et valider
- [ ] Commit: `fix: implement CRUDGenerationService methods`

**Pattern à suivre**:
```csharp
public async Task GenerateControllerAsync(CRUDConfig config)
{
    // Basé sur la structure existante du service
    // Utiliser les ressources disponibles
}
```

##### 2B.2 - OptionGenerationService.cs (60 min)

Fichier: [BIA.ToolKit.Application/Services/Option/OptionGenerationService.cs](BIA.ToolKit.Application/Services/Option/OptionGenerationService.cs)

- [ ] Localiser `throw new NotImplementedException()`
- [ ] Implémenter GenerateOptionAsync()
- [ ] Implémenter GenerateOptionDtoAsync()
- [ ] Compiler et valider
- [ ] Commit: `fix: implement OptionGenerationService methods`

**Checkpoint Phase 2B**:
```powershell
dotnet build BIAToolKit.sln --configuration Debug
dotnet test BIAToolKit.sln --configuration Debug
```

---

### 🟡 PHASE 3: TESTS END-TO-END (2h)

#### Test 1: Gestion des Repositories (20 min)
- [ ] Démarrer application
- [ ] Ajouter repository de test
- [ ] Vérifier dans la liste
- [ ] Rafraîchir (RefreshCommand)
- [ ] Modifier le repository
- [ ] Supprimer le repository
- [ ] Vérifier suppression

#### Test 2: Génération de projet (20 min)
- [ ] Configurer paramètres de génération
- [ ] Lancer GenerateProjectCommand
- [ ] Observer la progression
- [ ] Vérifier les fichiers générés

#### Test 3: Analyse de projet (15 min)
- [ ] Sélectionner un projet
- [ ] Lancer AnalyzeProjectCommand
- [ ] Vérifier les résultats

#### Test 4: Import/Export (15 min)
- [ ] Configurer des paramètres
- [ ] Exporter (ExportSettingsCommand)
- [ ] Importer (ImportSettingsCommand)
- [ ] Vérifier les paramètres restaurés

#### Test 5: Custom Templates (20 min)
- [ ] Ouvrir CustomTemplates UI
- [ ] Charger templates (LoadCustomTemplates)
- [ ] Ajouter template (SaveCustomTemplate)
- [ ] Supprimer template (DeleteCustomTemplate)

#### Test 6: Régression (30 min)
- [ ] Vérifier fonctionnalités existantes
- [ ] Navigation application
- [ ] Affichage des données
- [ ] Interactions UI

**Checkpoint Phase 3**:
```
Tous les scénarios passent ✅
Aucune régression ✅
Application stable ✅
```

---

### ✅ PHASE 4: DOCUMENTATION ET RELEASE (1h)

#### Étape 4.1: Build Release
```powershell
dotnet clean BIAToolKit.sln --configuration Release
dotnet build BIAToolKit.sln --configuration Release
dotnet test BIAToolKit.sln --configuration Release
```

#### Étape 4.2: Package MSIX
```powershell
cd Bia.ToolKit.MSIX
msbuild Bia.ToolKit.MSIX.wapproj /p:Configuration=Release /p:Platform=x64
```

#### Étape 4.3: Mettre à jour documentation
- [ ] Créer/mettre à jour CHANGELOG.md
- [ ] Mettre à jour README.md avec nouvelles fonctionnalités
- [ ] Documenter breaking changes (s'il y en a)

#### Étape 4.4: Git tagging
```powershell
git checkout main
git merge feature/restore-missing-implementations
git tag -a v1.0.0 -m "Restoration complete - fully operational"
git push origin v1.0.0
```

**Checkpoint Phase 4**:
- ✅ Build Release OK
- ✅ Package MSIX créé
- ✅ Documentation à jour
- ✅ Git tag créé

---

## 📊 GESTION DE L'ÉTAT

### Fichiers de tracking

**EXECUTION_NOTES.md** - À créer et maintenir:
```markdown
# Notes d'exécution
## Phase 0
- Audit 1A: [Résultats]
- Audit 2A: [Résultats]
- Audit exceptions: [Résultats]
- Écarts trouvés: [List]

## Phase 1A
- Commandes implémentées: [Count]
- Compilation status: [OK/FAILED]
- Tests status: [OK/FAILED]

## Phase 2A
- Fichier exact localisé: [Path]
- Adaptations requises: [List]
- TODOs complétés: [Count]

...
```

### Checkpoints obligatoires

Après chaque phase majeure:
1. Compiler sans erreur
2. Tests unitaires au vert
3. Pas de warnings critiques
4. Commit Git avec message clair
5. Documenter dans EXECUTION_NOTES.md

---

## 🚨 GESTION DES ERREURS

### Si compilation échoue

```powershell
# 1. Analyser l'erreur
dotnet build BIAToolKit.sln 2>&1 | Tee-Object -FilePath error_log.txt

# 2. Rollback au dernier commit
git status
git diff

# 3. Rollback si nécessaire
git reset --hard HEAD
```

### Si test échoue

```powershell
# 1. Exécuter avec logs détaillés
dotnet test BIAToolKit.sln --logger "console;verbosity=detailed"

# 2. Lire les logs de test
# 3. Identifier le test échoué
# 4. Corriger le code testé
```

### Si UI non réactive

```powershell
# 1. Arrêter l'application (Ctrl+C)
# 2. Vérifier les erreurs d'injection de dépendances
# 3. Vérifier les bindings XAML
# 4. Vérifier l'ordre d'initialisation des services
```

---

## 📞 RESSOURCES DISPONIBLES

### Documentation:
- [PLAN_EXECUTION.md](PLAN_EXECUTION.md) - Plan détaillé complet
- [VALIDATION_PLAN_EXECUTION.md](VALIDATION_PLAN_EXECUTION.md) - Validations effectuées
- [QUICK_START_IMPLEMENTATION.md](QUICK_START_IMPLEMENTATION.md) - Code snippets
- [INDEX_COMPLET.md](INDEX_COMPLET.md) - Navigation complète

### En cas de blocage:
1. Consulter l'INDEX_COMPLET.md
2. Chercher dans QUICK_START_IMPLEMENTATION.md
3. Vérifier le baseline: `git show 6a721cf`
4. Consulter AUDIT_MIGRATION_COMPLET.md

---

## 🎯 RÉSUMÉ TIMELINE

| Phase | Durée | Commits |
|-------|-------|---------|
| Phase 0 + Audits | 45 min | 1 (branch) |
| Phase 1A | 2-2.5h | 4-5 |
| Phase 1B | 1h | 1-2 |
| Phase 1C | 1.5-2h | 1 |
| Phase 2A | 1.5-2h | 1-3 |
| Phase 2B | 1.5-2h | 1-2 |
| Phase 3 | 2h | 1 |
| Phase 4 | 1h | 1 (merge) |
| **TOTAL** | **11-14.5h** | ~20 commits |

---

## ✅ CRITÈRES DE SUCCÈS FINAL

✅ Application compile sans erreur  
✅ Tous les tests unitaires passent  
✅ Tous les scénarios E2E validés  
✅ Aucune régression détectée  
✅ Build Release réussie  
✅ Package MSIX créé  
✅ Documentation à jour  
✅ Git tag v1.0.0 créé  
✅ Application 100% opérationnelle  

---

**Prêt à démarrer l'exécution** ✅

