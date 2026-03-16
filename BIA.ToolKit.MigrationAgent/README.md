# BIA.ToolKit.MigrationAgent

Agent de migration pour les projets BIA Framework via GitHub Copilot (agent mode) intégré à VS Code.

> **Prérequis** : GitHub Copilot avec licence active dans VS Code (pas besoin de CLI).

---

## Usage développeur (Guide rapide)

### 1. Installer l'agent dans ton projet

Depuis le dossier BIAToolKit, lance le script d'installation en pointant vers ton projet :

```powershell
# Migration V5 → V6
.\BIA.ToolKit.MigrationAgent\scripts\Install-MigrationAgent.ps1 -TargetProjectPath "C:\MonProjet"

# Migration V6 → V7
.\BIA.ToolKit.MigrationAgent\scripts\Install-MigrationAgent.ps1 -TargetProjectPath "C:\MonProjet" -MigrationVersion "v6-to-v7"
```

Le script :
- Détecte automatiquement la version BIA de ton projet
- Copie le prompt et les données de migration dans `.github/copilot/`
- Configure les settings VS Code (auto-approve des commandes git/dotnet/npm)

### 2. Lancer la migration

1. **Ouvrir ton projet** dans VS Code
2. **Ouvrir Copilot Chat** : `Ctrl+Shift+I`
3. **Passer en mode Agent** (sélecteur en haut du chat)
4. **Attacher le prompt** : taper `#file:` puis sélectionner :
   - `.github/copilot/prompts/migration-v5-to-v6.prompt.md` (V5→V6)
   - `.github/copilot/prompts/migration-v6-to-v7.prompt.md` (V6→V7)
5. **Écrire** : `Migre mon projet` ou `Run this migration`

### 3. Suivre la migration

L'agent travaille phase par phase et demande confirmation entre chaque. Tu peux :
- **Vérifier** ce qu'il a détecté (version, CompanyName, ProjectName)
- **Valider** les changements proposés avant qu'il continue
- **Corriger** si l'agent fait une erreur : il respecte tes indications

### 4. Après la migration

1. **Vérifier le diff git** : `git diff migration/v6-to-v7~1..migration/v6-to-v7`
2. **Tester l'application** : lancer le back (.NET) et le front (Angular)
3. **Nettoyer** les fichiers de migration (optionnel) :
   ```powershell
   .\BIA.ToolKit.MigrationAgent\scripts\Uninstall-MigrationAgent.ps1 -TargetProjectPath "C:\MonProjet"
   ```
4. Si ça ne va pas : `git checkout main && git branch -D migration/v6-to-v7`

### Ce qui est installé dans ton projet

| Fichier | Emplacement | Description |
|---|---|---|
| Prompt agent | `.github/copilot/prompts/migration-*.prompt.md` | Instructions pour Copilot |
| Données migration | `.github/copilot/migration-data/{version}/` | Templates, breaking changes, config |
| Settings VS Code | `.vscode/settings.json` | Auto-approve commandes, prompt files |
| Manifest | `.github/copilot/migration-manifest.json` | Ce qui a été installé (pour désinstaller) |

> **Rien n'est ajouté au code source** de ton projet. Tout est dans `.github/` et `.vscode/`.

### Migrations disponibles

| Migration | Prompt | Statut |
|---|---|---|
| V5 → V6 | `migration-v5-to-v6.prompt.md` | Disponible (données à compléter) |
| V6 → V7 | `migration-v6-to-v7.prompt.md` | Disponible |

---

## Comment ça marche

### Phases de migration (V5→V6)

| Phase | Description |
|---|---|
| 1. Détection | Trouve `Constants.cs`, extrait `FrameworkVersion`, `CompanyName`, `ProjectName`, fronts Angular |
| 2. Branche git | Crée `migration/v5-to-v6`, vérifie le working tree propre |
| 3. Patch | Adapte le patch pré-calculé (substitution de noms) et applique avec `git apply --reject` |
| 4. BIA folders | Remplace les dossiers `bia-*` Angular par la version cible |
| 5. Résolution .rej | Le LLM comprend l'**intention** de chaque changement rejeté et l'applique en préservant le code custom |
| 6. Dépendances | Met à jour NuGet (.csproj) et npm (package.json) |
| 7. Fix usings | `dotnet format` pour nettoyer les `using` C# |
| 8. Cleanup | Supprime les fichiers temporaires, met à jour `FrameworkVersion`, commit |
| 9. Vérification | Build .NET + Angular, rapport de statut |

**L'avantage sur le BIAToolKit GUI** : la Phase 5 utilise l'intelligence du LLM au lieu d'un `git merge-file` mécanique. L'agent comprend l'*intention* des changements et les applique contextuellement.

### Phases supplémentaires (V6→V7)

La migration V6→V7 ajoute une **Phase 4 : IocContainer Split**, la transformation structurelle la plus complexe.

**Le problème** : en V6, `IocContainer.cs` est un fichier monolithique mélangeant code BIA et code custom du dev. En V7, il est séparé en 2 fichiers `partial class` :

| Fichier | Propriétaire | Contenu |
|---|---|---|
| `Crosscutting.Ioc/IocContainer.cs` | **Développeur** | Orchestrateur léger + registrations custom |
| `Crosscutting.Ioc/Bia/IocContainer.cs` | **BIA Framework** | Tout le boilerplate framework |

**Ce que fait l'agent** :
1. Lit le `IocContainer.cs` du dev et le compare au template V6 → identifie le code custom
2. Génère `Bia/IocContainer.cs` depuis le template V7 (code framework uniquement)
3. Réécrit `IocContainer.cs` en orchestrateur V7 en **injectant tout le code custom** du dev
4. Adapte les références de paramètres (`collection.AddXxx()` → `param.Collection.AddXxx()`)
5. Vérifie qu'aucune registration custom n'a été perdue

> **Règle d'or** : la migration **évolue** le code existant, elle ne l'écrase JAMAIS.

---

## Structure du repo

```
BIA.ToolKit.MigrationAgent/
  scripts/
    Install-MigrationAgent.ps1       # Installe prompt + données dans le projet cible
    Uninstall-MigrationAgent.ps1     # Nettoie les fichiers de migration
  prompts/
    migration-v5-to-v6.prompt.md     # Prompt agent Copilot pour V5 → V6
    migration-v6-to-v7.prompt.md     # Prompt agent Copilot pour V6 → V7
  sources/
    v5-to-v6/
      migration-config.json          # Métadonnées de migration
      migration.patch                # Diff pré-calculé (template V5 vs V6)
      breaking-changes.md            # Breaking changes connus
      package-versions.json          # Versions NuGet + npm
      files-to-delete.json           # Fichiers supprimés entre versions
      dotnet-dependencies.json       # Mises à jour .NET
      angular-dependencies.json      # Mises à jour Angular
    v6-to-v7/
      migration-config.json          # Métadonnées de migration
      breaking-changes.md            # Breaking changes (IocContainer split, etc.)
      IocContainer_V6.cs             # Template V6 de référence (pour analyse diff)
      IocContainer_V7.cs             # Template V7 niveau projet
      IocContainer_v7_Bia.cs         # Template V7 niveau Bia (framework)
      Exemple_IocContainer.cs        # Exemple réel V6 avec customisations dev
```

---

> **Équipe BIA / contributeurs BIAToolKit** : pour ajouter le support d'une nouvelle migration (V7→V8, etc.), voir [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Améliorer un prompt existant : boucle de rétroaction

La qualité du prompt s'améliore avec les retours de vraies migrations. Voici comment ça fonctionne.

### Le principe

```
Agent : Phases 1-9   → commits granulaires après chaque étape logique
                        └─ "chore(migration): apply V6→V7 patch"
                        └─ "chore(migration): IocContainer split V6→V7 (partial class)"
                        └─ "chore(migration): resolve .rej files (X resolved, Y remaining)"
                        └─ "chore(migration): update dependencies and cleanup V6→V7"
Agent : Phase 10     → build, tentatives de fix, itère jusqu'au max
                        → si échec non résolu : ajoute // TODO: BIA Migration dans le code
                        → commit FINAL : "chore: migrate BIA Framework from V6 to V7 [...]"
                        → remet la main au dev avec la liste de ce qui reste
Dev   : corrections manuelles sur les // TODO: BIA Migration
Dev   : commit ses corrections
Dev   : ré-invoque Phase 11 dans Copilot
Agent : git diff {dernier-commit-agent}..HEAD
              → voit exactement ce qui a changé APRÈS son commit final
              → analyse chaque hunk pour comprendre pourquoi il a échoué
              → génère le feedback avec les diffs réels et suggestions
```

**Le commit final de l'agent est la frontière nette.** L'historique granulaire permet en plus de voir quelle phase a introduit un problème si le build échoue.

Si l'agent ne peut pas résoudre une erreur, il laisse le code original intact et ajoute un marqueur `// TODO: BIA Migration V6→V7 - could not resolve automatically` à l'endroit exact. Le dev peut faire `Ctrl+Shift+F` sur ce marqueur pour trouver tous les points d'intervention.

### Ce que génère la Phase 11 (automatiquement depuis git)

La Phase 11 est invoquée **après** que le dev a commité ses corrections manuelles. L'agent :

1. Retrouve le commit de migration (`chore: migrate BIA Framework from V6 to V7`)
2. Exécute `git diff {migration-commit}..HEAD` pour voir ce qui a changé après son travail
3. Analyse chaque fichier/hunk modifié pour déterminer la cause racine (`.rej` non résolu, erreur de build inconnue, pattern non reconnu…)
4. Génère `.github/copilot/migration-feedback-vX-to-vY.md` avec pour chaque correction : le diff exact, la cause, et une suggestion concrète pour améliorer le prompt

Le dev n'a pas à documenter quoi que ce soit manuellement — le rapport se construit depuis la réalité du code.

### Comment contribuer un retour

1. Après migration, récupérer le fichier de feedback :
   ```
   .github/copilot/migration-feedback-v6-to-v7.md
   ```
2. Ouvrir une **issue ou PR sur BIAToolKit** en joignant ce fichier
3. L'équipe BIA intègre les corrections manuelles dans :
   - `sources/v6-to-v7/breaking-changes.md` — nouveaux cas documentés avec exemples de code
   - `prompts/migration-v6-to-v7.prompt.md` — nouvelles instructions pour l'agent

### Ce qui alimente `breaking-changes.md`

Le fichier `breaking-changes.md` est la **mémoire longue** de l'agent : quand le build échoue après migration, l'agent le lit pour savoir comment corriger. Chaque correction manuelle contribuée devient un exemple concret dans ce fichier :

```markdown
### Cas réel : {titre du problème}

**Symptôme** : {erreur de build ou comportement incorrect}
**Cause** : {pourquoi ça arrive}
**Correction** :
\```diff
- ancien code
+ nouveau code
\```
```

Plus il y a de cas réels documentés, plus l'agent est autonome sur les migrations suivantes.
