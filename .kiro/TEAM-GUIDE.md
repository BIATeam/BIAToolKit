# 🚀 Équipe de Développement Virtuelle

Bienvenue dans ton équipe de dev complète ! Cette configuration te donne accès à 6 experts spécialisés.

## 👥 Les Membres de l'Équipe (7 Experts)

### 1. 📋 Product Owner
**Activation** : `#product-owner`
**Rôle** : Clarifier les besoins métier, définir les user stories et critères d'acceptation
**Quand l'utiliser** : Requirements flous, priorisation, validation métier

### 2. 🏗️ Tech Lead / Architecte
**Activation** : `#tech-lead`
**Rôle** : Valider l'architecture, proposer des patterns, identifier les risques techniques
**Quand l'utiliser** : Choix d'architecture, validation technique, design complexe

### 3. 🎨 Dev Frontend Angular/TypeScript
**Activation** : `#frontend-dev-angular` ou automatique sur fichiers `.ts`, `.html`, `.scss`
**Rôle** : Implémenter les composants Angular avec les best practices
**Quand l'utiliser** : Questions Angular, state management, performance frontend

### 4. ⚙️ Dev Backend .NET 10
**Activation** : `#backend-dev-dotnet` ou automatique sur fichiers `.cs`
**Rôle** : Implémenter les APIs et la logique métier en .NET 10
**Quand l'utiliser** : Questions .NET, APIs, accès données, sécurité backend

### 5. 🧪 QA Engineer
**Activation** : `#qa-engineer`
**Rôle** : Définir la stratégie de test, identifier les propriétés de correctness
**Quand l'utiliser** : Stratégie de test, property-based testing, cas limites

### 6. 🔧 DevOps Engineer
**Activation** : `#devops-engineer`
**Rôle** : Gérer le déploiement, CI/CD, infrastructure
**Quand l'utiliser** : Configuration, déploiement, environnements, monitoring

### 7. 📝 Technical Writer
**Activation** : `#technical-writer`
**Rôle** : Rédiger et améliorer la documentation (utilisateur, développeur, API)
**Quand l'utiliser** : Création de docs, guides, tutoriels, documentation BIA Framework

### 🎯 Spec Reviewer (Orchestrateur)
**Activation** : Automatique sur les fichiers de spec
**Rôle** : Coordonner l'équipe et solliciter les bons experts
**Quand l'utiliser** : Review de spec, validation complète

## 🤖 Hooks Automatiques Configurés

### 1. Review Automatique de Spec
- **Déclencheur** : Modification d'un fichier de spec
- **Action** : Le Spec Reviewer analyse et sollicite les experts pertinents
- **Fichiers** : `requirements.md`, `design.md`, `tasks.md`, `bugfix.md`

### 2. Validation Pré-Implémentation
- **Déclencheur** : Avant de démarrer une tâche d'implémentation
- **Action** : Le Tech Lead valide l'architecture et identifie les risques
- **Objectif** : Éviter les problèmes techniques avant de coder

### 3. Expert Angular - Review Code
- **Déclencheur** : Modification de fichiers Angular
- **Action** : Review automatique par l'expert frontend
- **Fichiers** : `*.component.ts`, `*.service.ts`, `*.module.ts`

### 4. Expert .NET - Review Code
- **Déclencheur** : Modification de fichiers .NET
- **Action** : Review automatique par l'expert backend
- **Fichiers** : `*.cs`, `Program.cs`, `Startup.cs`

## 📖 Comment Utiliser l'Équipe

### Scénario 1 : Créer une Nouvelle Spec
```
1. Lance la création de spec normalement
2. Le Spec Reviewer s'active automatiquement
3. Il sollicite les experts selon les besoins détectés
4. Tu reçois des feedbacks ciblés de chaque expert
```

### Scénario 2 : Solliciter un Expert Spécifique
```
Dans ton message, utilise le contexte :
"#tech-lead peux-tu valider cette architecture ?"
"#qa-engineer quels tests property-based proposer ?"
"#backend-dev-dotnet comment optimiser cette requête EF Core ?"
```

### Scénario 3 : Review de Code
```
1. Modifie un fichier .cs ou .ts
2. L'expert correspondant review automatiquement
3. Tu reçois des suggestions d'amélioration
```

### Scénario 4 : Validation Avant Implémentation
```
1. Lance l'exécution d'une tâche de spec
2. Le hook pré-implémentation se déclenche
3. Le Tech Lead valide l'architecture
4. L'implémentation démarre si tout est OK
```

## 🎯 Exemples d'Utilisation

### Exemple 1 : Clarifier des Requirements
```
"#product-owner les requirements de cette feature sont-ils assez clairs ?
Manque-t-il des critères d'acceptation ?"
```

### Exemple 2 : Valider une Architecture
```
"#tech-lead je veux implémenter un CQRS avec MediatR pour cette feature.
Est-ce justifié ou c'est de la sur-ingénierie ?"
```

### Exemple 3 : Question Technique Frontend
```
"#frontend-dev-angular comment gérer le state management pour cette feature ?
NgRx ou un simple service suffit ?"
```

### Exemple 4 : Question Technique Backend
```
"#backend-dev-dotnet comment optimiser cette requête qui fait un N+1 ?
Dois-je utiliser Include() ou une requête Dapper ?"
```

### Exemple 5 : Stratégie de Test
```
"#qa-engineer quelles propriétés de correctness tester pour ce système de panier ?
Quels sont les invariants critiques ?"
```

### Exemple 6 : Déploiement
```
"#devops-engineer comment structurer les environnements pour cette app
Angular + .NET 10 avec une base PostgreSQL ?"
```

### Exemple 7 : Documentation
```
"#technical-writer peux-tu créer un guide utilisateur pour cette feature CRUD ?
Avec des captures d'écran et des exemples."
```

## 🔄 Workflow Recommandé

### Phase 1 : Requirements
1. Rédige tes requirements
2. Le Spec Reviewer s'active automatiquement
3. Il sollicite le Product Owner si besoin
4. Itère jusqu'à validation

### Phase 2 : Design
1. Rédige ton design technique
2. Le Spec Reviewer sollicite le Tech Lead
3. Les experts frontend/backend donnent leur avis
4. Le DevOps valide la faisabilité du déploiement
5. Itère jusqu'à validation

### Phase 3 : Tasks
1. Définis tes tâches d'implémentation
2. Le QA Engineer propose les tests
3. Validation finale avant implémentation

### Phase 4 : Implémentation
1. Lance l'exécution des tâches
2. Le hook pré-implémentation valide tout
3. Les experts review le code automatiquement
4. Itère jusqu'à complétion

## 💡 Conseils

- **Sollicite les experts tôt** : Mieux vaut avoir un feedback en phase de design qu'après l'implémentation
- **Utilise les hooks** : Ils automatisent beaucoup de validations
- **Combine les experts** : Tu peux solliciter plusieurs experts dans le même message
- **Sois spécifique** : Plus ta question est précise, meilleure sera la réponse

## 🛠️ Personnalisation

Tu peux modifier les steering files dans `.kiro/steering/` pour :
- Ajuster les responsabilités de chaque expert
- Ajouter des patterns spécifiques à ton projet
- Modifier les règles d'activation automatique

Tu peux modifier les hooks dans l'interface Kiro pour :
- Changer les déclencheurs
- Ajuster les actions automatiques
- Désactiver certains hooks si trop verbeux

## 🎉 Profite de ton Équipe !

Tu as maintenant une équipe complète qui t'accompagne à chaque étape du développement. N'hésite pas à les solliciter, c'est leur job ! 😊
