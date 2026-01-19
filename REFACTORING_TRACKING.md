# Plan de Refactorisation - Suivi d'Implémentation

**Date de Création**: 19 janvier 2026  
**Statut Global**: 📋 Plan Finalisé - En Attente d'Approbation

---

## 📊 Vue d'Ensemble des 26 Étapes

### PHASE 1: Infrastructure & Services (Étapes 1-5)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 1 | Créer IFileDialogService | Interface abstraite pour file browse | ⬜ Pas Commencé | 2h | Dépendance: aucune |
| 2 | Implémenter FileDialogService | Implementation enveloppes FileDialog | ⬜ Pas Commencé | 2h | Après étape 1 |
| 3 | Créer ITextParsingService | Service parsage texte/noms | ⬜ Pas Commencé | 2h | Dépendance: aucune |
| 4 | Créer IDialogService | Service gestion dialogs | ⬜ Pas Commencé | 3h | Dépendance: aucune |
| 5 | Enregistrer services DI | Configuration DI (App.xaml.cs) | ⬜ Pas Commencé | 1h | Après 1-4 |

**Estimation Phase 1**: 10 heures

---

### PHASE 4: Analyse & Bonnes Pratiques (Étapes 19-26)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 19 | Appliquer SRP | Analyser/documenter violations | ⬜ Pas Commencé | 2h | Parallèle avec phases 2-3 |
| 20 | Appliquer DRY | Identifier/fusionner duplications | ⬜ Pas Commencé | 2h | Code review |
| 21 | Appliquer YAGNI | Lister/supprimer code mort | ⬜ Pas Commencé | 3h | CustomTemplate dialogs |
| 22 | Appliquer KISS | Simplifier logique complexe | ⬜ Pas Commencé | 2h | Refactoring méthodes |
| 23 | Appliquer OCP | Extensibilité sans modification | ⬜ Pas Commencé | 2h | Design review |
| 24 | Appliquer DIP | Dépend abstractions, pas concrétions | ⬜ Pas Commencé | 2h | Vérification DI |
| 25 | Appliquer LSP | Substitution polymorphe sûre | ⬜ Pas Commencé | 1h | Repository variants |
| 26 | Appliquer ISP | Interfaces ciblées, pas génériques | ⬜ Pas Commencé | 1h | IRepository split |

**Estimation Phase 4**: 15 heures

---

### PHASE 2: ViewModel Refactoring - MainWindow (Étapes 6-10)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 6 | Analyser MainWindow.xaml.cs | Documenter responsabilités | ⬜ Pas Commencé | 1h | Après Phase 1 |
| 7 | Créer MainWindowInitializationViewModel | Initialisation app/settings | ⬜ Pas Commencé | 3h | Après étape 6 |
| 8 | Créer RepositoryValidationViewModel | Validation repositories | ⬜ Pas Commencé | 2h | Après étape 6 |
| 9 | Refactoriser MainWindow.xaml.cs | Réduire de 556 à ~150 lignes | ⬜ Pas Commencé | 3h | Après 7-8 |
| 10 | Créer MainWindowCompositionRoot | Centraliser DI composition | ⬜ Pas Commencé | 1h | Après 7-8-9 |

**Estimation Phase 2**: 10 heures

---

### PHASE 3: ViewModel Refactoring - UserControls (Étapes 11-18)

| # | Étape | Description | Statut | Effort | Notes |
|---|-------|-------------|--------|--------|-------|
| 11 | Refactoriser CRUDGeneratorUC | 795 → 200 lignes (75% réduction) | ⬜ Pas Commencé | 5j | **CRITIQUE** |
| 12 | Refactoriser DtoGeneratorUC | 650 → 180 lignes (72% réduction) | ⬜ Pas Commencé | 4j | **CRITIQUE** |
| 13 | Refactoriser OptionGeneratorUC | 500 → 150 lignes (70% réduction) | ⬜ Pas Commencé | 3j | **IMPORTANTE** |
| 14 | Refactoriser ModifyProjectUC | Ajouter IFileDialogService | ⬜ Pas Commencé | 2j | Moyenne priorité |
| 15 | Refactoriser RepositoryFormUC | 60 → 20 lignes (67% réduction) | ⬜ Pas Commencé | 0.5j | Simple |
| 16 | Refactoriser VersionAndOptionUserControl | DRY cleanup | ⬜ Pas Commencé | 1j | Simple |
| 17 | Refactoriser LabeledField | Documentation (peu de changements) | ⬜ Pas Commencé | 0.25j | OK déjà |
| 18 | Refactoriser Dialog Controls | LogDetail, CustomTemplate* | ⬜ Pas Commencé | 1j | YAGNI included |

**Estimation Phase 3**: 16.75 jours (équivalent: ~3 semaines)

---

## 📈 Graphique de Progression

```
PHASE 1: Infrastructure        ██████ (10h)        [Semaine 0]
PHASE 2: MainWindow            ██████ (10h)        [Semaine 1]
PHASE 3: UserControls          ██████████████ (17h) [Semaines 2-3]
PHASE 4: Analyse/QA            ██████ (15h)        [Concurrent]
TESTS & DOCUMENTATION          ███ (5h)            [Semaine 4]

─────────────────────────────────────────────────
TOTAL:                         ~57 heures           ~4 semaines
```

---

## 📋 Matrices de Dépendances

### Dépendances strictes (Ordonnées)

```
Étape 1 (IFileDialogService)
    ├→ Étape 2 (FileDialogService)
    └→ Étape 5 (DI registration)
        ├→ Étape 14 (ModifyProjectUC uses it)
        └→ Étape 15 (RepositoryFormUC uses it)

Étape 3 (ITextParsingService)
    ├→ Étape 5 (DI registration)
    └→ Étape 11 (CRUDGeneratorUC uses it)

Étape 4 (IDialogService)
    ├→ Étape 5 (DI registration)
    └→ Étape 18 (Dialog controls use it)

Étape 6 (Analyse MainWindow)
    ├→ Étape 7 (MainWindowInitializationViewModel)
    ├→ Étape 8 (RepositoryValidationViewModel)
    └→ Étape 9 (Refactor MainWindow)
        └→ Étape 10 (CompositionRoot)
```

### Dépendances optionnelles (Parallèles)

```
Phase 1 (Services) peut être parallèle avec Phase 4 (Analyse)
Phase 2 (MainWindow) peut être parallèle avec Phase 3 (UserControls)
Étapes 11-18 (UserControls) peuvent être parallèles entre elles
```

---

## 🎯 Jalons Importants

### Jalons par Semaine

**Semaine 0 (Préparation)**
- [ ] Approbation du plan
- [x] Documentation finalisée
- [ ] Team training sur patterns
- [ ] Configuration outils (SonarQube, etc.)
- **Résultat**: Infrastructure services prête

**Semaine 1 (Phase 2)**
- [ ] MainWindowInitializationViewModel implémentée
- [ ] RepositoryValidationViewModel implémentée
- [ ] MainWindow.xaml.cs refactorisée
- [ ] Tests MainWindow passants
- **Résultat**: MainWindow >80% testable

**Semaine 2-3 (Phase 3 - Batch 1)**
- [ ] CRUDGeneratorUC refactorisée (75% réduction)
- [ ] DtoGeneratorUC refactorisée (72% réduction)
- [ ] OptionGeneratorUC refactorisée (70% réduction)
- [ ] Tests UserControls passants
- **Résultat**: 3 composants majeurs refactorisés

**Semaine 3 (Phase 3 - Batch 2)**
- [ ] ModifyProjectUC, RepositoryFormUC refactorisées
- [ ] Dialog controls nettoyés (YAGNI)
- [ ] Tests passants
- **Résultat**: Tous UserControls modernes

**Semaine 4 (Finalisation)**
- [ ] Tests complets (coverage >80%)
- [ ] Code review SOLID completed
- [ ] Performance validation
- [ ] Documentation mise à jour
- **Résultat**: Production-ready

---

## 🔄 Cycles de Révision

### Code Review Cycle

```
Pour chaque étape:
  1. Implémentation (dev)
  2. Unit tests (dev) → >80% coverage
  3. Code review SOLID (tech lead)
  4. Integration tests (QA)
  5. Merge à main

Estimé: 1 jour par ViewModel + UserControl
```

### Architecture Review

```
Checkpoints:
  - Après Phase 1 (Services OK?)
  - Après Phase 2 (MainWindow architecture OK?)
  - Après Phase 3 (UserControls patterns OK?)
  - Avant merge (overall structure?)
```

---

## 📊 Métriques à Tracker

### Quantitatives

| Métrique | Avant | Cible | Unité |
|----------|-------|-------|-------|
| Code-Behind Total | 3,431 | 880 | lignes |
| Complexité Cyclomatique Moyenne | 22 | 4 | n/a |
| Test Coverage | 10% | >80% | % |
| SonarQube Grade | D | A | grade |

### Qualitatives

| Aspect | Avant | Après |
|--------|-------|-------|
| **Maintenabilité** | 😞 Difficile | 😊 Facile |
| **Testabilité** | 😞 Impossible | 😊 Triviale |
| **Lisibilité** | 😞 Confuse | 😊 Claire |
| **Extensibilité** | 😞 Fermée | 😊 Ouverte |

---

## 🚨 Risques et Mitigations

| Risque | Probabilité | Impact | Mitigation |
|--------|-------------|--------|-----------|
| Régression fonctionnelle | Moyenne | Critique | Tests complets avant merge |
| Performance dégradée | Basse | Majeure | Profiling avant/après |
| Merge conflicts | Moyenne | Mineure | Petites PR, CI/CD |
| Team skills gap | Moyenne | Majeure | Training sur patterns |
| Timeline slippage | Moyenne | Majeure | Buffer 20% (12j au lieu de 10j) |

---

## 📋 Checklist Pré-Lancement

### Préparation Technique
- [ ] Repository créé (git)
- [ ] Branches configurées (main, develop, feature/*)
- [ ] CI/CD pipeline en place
- [ ] SonarQube/CodeCov connectés
- [ ] Tests framework configurés (xUnit)

### Préparation Humaine
- [ ] Team training SOLID (2h)
- [ ] Team training MVVM patterns (2h)
- [ ] Team training CommunityToolkit.Mvvm (1h)
- [ ] Pair programming sessions planifiées
- [ ] Code review guidelines partagées

### Préparation Documentation
- [ ] Architecture diagram créé
- [ ] ViewModel naming conventions documentées
- [ ] Service interfaces documented
- [ ] Message classes documented
- [ ] DI composition documented

### Baseline
- [ ] Snapshot métrique SonarQube (Day 0)
- [ ] Snapshot tests (Day 0)
- [ ] Snapshot performance (Day 0)

---

## 📅 Estimation Détaillée par Rôle

### Développeur Principal (Full Time)

```
Semaine 0: Préparation (10h)
  - Services implémentation
  - Team training
  
Semaine 1: MainWindow (10h)
  - ViewModels refactoring
  - Tests

Semaine 2-3: UserControls (17h)
  - Gros refactorings (CRUD, DTO, Option)
  - Tests

Semaine 4: Finalisation (5h)
  - Documentation
  - Performance review

TOTAL: ~42 heures (5-6 jours travail)
```

### Tech Lead (Part Time 30%)

```
Review code: 12h (3h/semaine)
Architecture: 6h (1.5h/semaine)
Training: 4h (2h semaine 0)

TOTAL: ~22 heures
```

### QA (Part Time 50%)

```
Test planning: 4h
Integration tests: 12h
Regression testing: 8h
Performance: 4h

TOTAL: ~28 heures
```

---

## 🎬 Getting Started

### Jour 1 (Lundi)

1. [ ] Review ce document (équipe)
2. [ ] Training SOLID (2h)
3. [ ] Training MVVM (2h)
4. [ ] Setup infrastructure (git, CI/CD)

### Jour 2-3 (Mardi-Mercredi)

1. [ ] Implémenter Phase 1 (Services)
2. [ ] Unit tests Phase 1
3. [ ] Code review Phase 1

### Jour 4-5 (Jeudi-Vendredi)

1. [ ] Implémenter Phase 2 (MainWindow)
2. [ ] Unit tests Phase 2
3. [ ] Integration tests Phase 2

---

## ✅ Sign-Off

### Approbation Requise

- [ ] Product Owner
- [ ] Tech Lead
- [ ] Architect
- [ ] QA Lead

### Notes d'Approbation

```
PO: ________________ Date: _________
Tech Lead: _________ Date: _________
Architect: ________ Date: _________
QA Lead: _________ Date: _________
```

---

## 📞 Contact & Questions

| Question | Réponse |
|----------|---------|
| Quand commençons-nous? | Après approbation du plan |
| Combien ça coûte? | ~57 heures (~7-8 jours travail) |
| Quels risques? | Voir section "Risques et Mitigations" |
| Comment gérez les bugs? | Hotfixes sur main, cherry-pick |
| Quand livrons-nous? | Fin semaine 4 (production-ready) |

---

*Document de suivi généré le 19 janvier 2026*
*Version: 1.0 - État: En attente d'approbation*
