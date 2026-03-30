# BIA.ToolKit.MigrationAgent

Agent de migration pour les projets BIA Framework via GitHub Copilot (agent mode) intégré à VS Code.

> **Prérequis** : GitHub Copilot avec licence active dans VS Code (pas besoin de CLI).

---

## Usage développeur (Guide rapide)

### 1. Installer l'agent dans ton projet

Depuis le dossier extrait du zip (ou le dossier BIAToolKit), lance le script d'installation :

```powershell
# Migration V5 → V6
.\Install.ps1 -TargetProjectPath "C:\MonProjet"

# Migration V6 → V7
.\Install.ps1 -TargetProjectPath "C:\MonProjet" -MigrationVersion "v6-to-v7"
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
   .\Uninstall.ps1 -TargetProjectPath "C:\MonProjet"
   ```
4. Si ça ne va pas : `git checkout main && git branch -D migration/v6-to-v7`

### Ce qui est installé dans ton projet

| Fichier | Emplacement | Description |
|---|---|---|
| Prompt agent | `.github/copilot/prompts/migration-*.prompt.md` | Instructions pour Copilot |
| Données migration | `.github/copilot/migration-data/{version}/` | Templates, migration-fixes, config |
| Settings VS Code | `.vscode/settings.json` | Auto-approve commandes, prompt files |
| Manifest | `.github/copilot/migration-manifest.json` | Ce qui a été installé (pour désinstaller) |

> **Rien n'est ajouté au code source** de ton projet. Tout est dans `.github/` et `.vscode/`.

### Migrations disponibles

| Migration | Prompt | Statut |
|---|---|---|
| V5 → V6 | `migration-v5-to-v6.prompt.md` | Disponible (données à compléter) |
| V6 → V7 | `migration-v6-to-v7.prompt.md` | **POC** — test IocContainer split uniquement |

---

## Comment ça marche

L'agent travaille **phase par phase** : détection du projet, branche git, application d'un patch, résolution intelligente des conflits (`.rej`), mise à jour des dépendances, vérification du build.

**L'avantage sur le BIAToolKit GUI** : la résolution des conflits utilise l'intelligence du LLM au lieu d'un `git merge-file` mécanique. L'agent comprend l'*intention* des changements et les applique en préservant le code custom du développeur.

> **Règle d'or** : la migration **évolue** le code existant, elle ne l'écrase JAMAIS.

Chaque prompt de migration contient le détail de ses phases. Consultez le `.prompt.md` correspondant pour les spécificités d'une version.

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
      migration-fixes/               # Cas connus : symptôme, diff, solution
      package-versions.json          # Versions NuGet + npm
      files-to-delete.json           # Fichiers supprimés entre versions
      dotnet-dependencies.json       # Mises à jour .NET
      angular-dependencies.json      # Mises à jour Angular
    v6-to-v7/
      migration-config.json          # Métadonnées de migration
      migration-fixes/               # Migration fixes (IocContainer split, etc.)
      IocContainer_V6.cs             # Template V6 de référence (pour analyse diff)
      IocContainer_V7.cs             # Template V7 niveau projet
      IocContainer_v7_Bia.cs         # Template V7 niveau Bia (framework)
      Exemple_IocContainer.cs        # Exemple réel V6 avec customisations dev
```


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
Dev   : corrige un problème dans son code
Dev   : ouvre Copilot Agent + attache report-migration-fix.prompt.md
Dev   : tape "Documente mon correctif"
Agent : git diff → analyse le correctif → demande confirmation au dev
              → génère un rapport structuré dans migration-fixes/
              → commit le rapport
Dev   : répète pour chaque correctif (un rapport par fix)
Dev   : envoie les rapports à l'équipe BIA (issue/PR)
```

**Le commit final de l'agent est la frontière nette.** L'historique granulaire permet en plus de voir quelle phase a introduit un problème si le build échoue.

Si l'agent ne peut pas résoudre une erreur, il laisse le code original intact et ajoute un marqueur `// TODO: BIA Migration V6→V7 - could not resolve automatically` à l'endroit exact. Le dev peut faire `Ctrl+Shift+F` sur ce marqueur pour trouver tous les points d'intervention.

### Documenter un correctif

Après chaque correction manuelle, tapez directement dans le **même chat** de migration :

```
Documente mon correctif : <détail optionnel>
```

Exemple : `Documente mon correctif : le merge du <p-table> a raté`

L'agent va :
- Analyser le `git diff` pour comprendre ce que vous avez changé
- Vous demander confirmation de son analyse
- Générer un rapport structuré dans `.github/copilot/migration-fixes/`

Vous pouvez taper cette commande autant de fois que nécessaire — un rapport par correctif.

> **Si vous avez fermé le chat de migration**, vous pouvez utiliser le prompt standalone `report-migration-fix.prompt.md` comme fallback (il re-détecte le contexte automatiquement).

### Contribuer les retours à BIAToolKit

1. Ouvrir une **issue ou PR sur BIAToolKit** en joignant les fichiers de `.github/copilot/migration-fixes/`
2. L'équipe BIA rapatrie chaque fichier dans `sources/{version}/migration-fixes/`
3. Les prochaines migrations bénéficieront directement de ces retours — l'agent les lit automatiquement

### Ce qui alimente `migration-fixes/`

Le dossier `migration-fixes/` est la **mémoire longue** de l'agent : quand le build échoue après migration, l'agent lit tous les fichiers de ce dossier pour trouver des patterns similaires et savoir comment corriger.

Deux sources alimentent ce dossier :
- **L'équipe BIA** : cas prédictifs documentés à l'avance (ex : IocContainer split)
- **Les devs** : cas réels documentés via "Documente mon correctif" après correction
