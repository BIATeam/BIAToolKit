# 🎬 LANCER L'AGENT - INSTRUCTIONS FINALES

**Date**: 2 Mars 2026  
**Status**: 🟢 PRÊT À DÉMARRER  
**Baseline**: 6a721cfe1d3af3ecb5f18f295072ef1c465c7e79

---

## ⏱️ AVANT DE CLIQUER "START"

### Temps de lecture: 3 min
### Checklist: 5 min
### Total avant lancement: 8 min

---

## 🚀 PRÉREQUIS RAPIDES

### ✅ Environnement
- [ ] .NET 6.0+ installé → `dotnet --version`
- [ ] Git disponible → `git --version`
- [ ] Espace disque: 2-3 GB libre
- [ ] Terminal PowerShell ouvert

### ✅ Repository
- [ ] Aucun changement non sauvegardé → `git status`
- [ ] Branche main à jour → `git fetch origin`
- [ ] Pas de merge en cours

### ✅ Documents
- [ ] DEMARRAGE_RAPIDE_AGENT.md lu (2 min)
- [ ] CHECKLIST_PRE_LANCEMENT.md lu (5 min)
- [ ] Compris la timeline 11-14.5h
- [ ] Compris les 4 phases majeures

---

## 📊 QUICK REMINDER

### Ce que l'agent va faire

**Phase 0 (45 min)**
```
✓ Valider environnement
✓ Créer branche feature/restore-missing-implementations
✓ Auditer commandes MainViewModel
✓ Localiser CustomTemplatesRepositoriesSettingsUC
✓ Auditer NotImplementedExceptions
```

**Phase 1 (4.5-5.5h)**
```
✓ Implémenter 8 commandes
✓ Implémenter 2 message handlers
✓ Tester et valider
```

**Phase 2 (3-4h)**
```
✓ Implémenter 3 TODOs
✓ Corriger 6 exceptions
✓ Tester et valider
```

**Phase 3 (2h)**
```
✓ Tests End-to-End (6 scénarios)
✓ Validation régression
```

---

## 🎯 RÉSULTAT ATTENDU

```
AVANT              APRÈS
43% opérationnel   100% opérationnel
3/11 commandes     11/11 commandes
3/5 handlers       5/5 handlers
0/8 méthodes       8/8 méthodes
🔴 BRISÉ           🟢 FONCTIONNEL
```

---

## 📁 RESSOURCES DISPONIBLES PENDANT EXÉCUTION

### Guides par phase
- 📖 INSTRUCTIONS_AGENT_EXECUTION.md - Phase 0-3 détaillées
- 📖 QUICK_START_IMPLEMENTATION.md - Code snippets prêts
- 📖 VALIDATION_PLAN_EXECUTION.md - Points d'attention

### Tracking
- 📝 EXECUTION_NOTES_TEMPLATE.md - Template de suivi
- Will be filled as: EXECUTION_NOTES.md

### Troubleshooting
- 📖 PLAN_EXECUTION.md - Plan original complet
- 📖 AUDIT_MIGRATION_COMPLET.md - Context historique

---

## 🔴 POINTS CRITIQUES À RETENIR

### Phase 0: Audits
⚠️ L'agent va:
1. Auditer les commandes existantes dans MainViewModel
2. Localiser (ou confirmer absence de) CustomTemplatesRepositoriesSettingsUC
3. Auditer tous les NotImplementedExceptions
4. Reporter les écarts vs PLAN_EXECUTION.md

🎯 Si écarts trouvés → Agent adaptera le plan automatiquement

### Phase 1-2: Implémentation
✅ Compilation obligatoire après chaque 2-3 tâches  
✅ Tests obligatoires après chaque phase majeure  
✅ Commits après chaque 2 tâches complétées

### Phase 3: Validation
✅ 6 scénarios E2E doivent tous passer 

---

## 📋 ORDRE DE LANCEMENT

### Étape 1: Terminal PowerShell (30 sec)
```powershell
cd c:\sources\Github\BIAToolKit
git status
```

### Étape 2: Valider quick (1 min)
```powershell
dotnet --version
dotnet build BIAToolKit.sln --configuration Debug
```

### Étape 3: LANCER L'AGENT (30 sec)
```
✅ Tous les prérequis cochés?
✅ Documents lus?
✅ Compris la timeline?
✅ Repository clean?

→ LANCER L'AGENT MAINTENANT
```

---

## 🎬 L'AGENT COMMENCERA PAR:

```
Phase 0: PRÉPARATION

├─ 0.1 Validation environnement
├─ 0.2 Branche de travail
├─ 0.3 AUDIT MainViewModel Commands
├─ 0.4 AUDIT CustomTemplatesRepositoriesSettingsUC
├─ 0.5 AUDIT NotImplementedExceptions
└─ Reporting des écarts
```

**Durée Phase 0: ~45 min**

L'agent créera `EXECUTION_NOTES.md` avec:
- Résultats des audits
- Écarts trouvés
- Adaptations apportées
- Prêt pour Phase 1

---

## 📞 MONITORING

### Pendant l'exécution:
```
✅ Vérifier EXECUTION_NOTES.md mis à jour
✅ Observer les commits Git
✅ Vérifier pas d'erreurs bloquantes
✅ Vérifier progress sur timeline
```

### Après chaque Phase:
```
✅ Vérifier checkpoint atteint
✅ Lire EXECUTION_NOTES.md pour phase
✅ Confirmer avant Phase suivante
```

---

## ✅ FINAL CHECKLIST AVANT DÉMARRAGE

- [ ] Git status = clean
- [ ] .NET version >= 6.0
- [ ] Espace disque > 2 GB
- [ ] DEMARRAGE_RAPIDE_AGENT.md lu
- [ ] CHECKLIST_PRE_LANCEMENT.md lu
- [ ] Compris timeline 11-14.5h
- [ ] Compris les 4 phases
- [ ] Compris les audits Phase 0
- [ ] Terminal PowerShell prêt
- [ ] Répertoire BIAToolKit accessible

---

## 🎯 C'EST PARTI!

```
If all checkboxes above are ✅

→ DÉMARRER L'AGENT MAINTENANT

L'agent continuera autonomement jusqu'à:
- Phase 0 ✅ (45 min)
- Phase 1 ✅ (4.5-5.5h)
- Phase 2 ✅ (3-4h)  
- Phase 3 ✅ (2h)

Total: 11-14.5 heures d'exécution autonome
```

---

## 🎉 APRÈS AGENT

Une fois l'agent terminé:
```
1. Vérifier EXECUTION_NOTES.md complet
2. Tester l'app: dotnet run --project BIA.ToolKit/BIA.ToolKit.csproj
3. Vérifier branch merge sur main
4. Vérifier tag v1.0.0 créé
5. Célébrer! 🎊
```

---

**Status**: 🟢 PRÊT À DÉMARRER

