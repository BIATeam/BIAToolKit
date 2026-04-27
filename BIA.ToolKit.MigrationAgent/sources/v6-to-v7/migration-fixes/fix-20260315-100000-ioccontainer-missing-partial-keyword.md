# Migration Fix: IocContainer missing `partial` keyword

- **Migration** : V6 → V7
- **Date** : 2026-03-15
- **Catégorie** : `build-error`
- **Fichier** : `DotNet/{CompanyName}.{ProjectName}.Crosscutting.Ioc/IocContainer.cs`

## Symptôme

After the IocContainer split (Phase 5), the build fails with:

```
error CS0260: Missing partial modifier on declaration of type 'IocContainer';
another partial declaration of this type exists
```

## Diff du correctif

```diff
- public static class IocContainer
+ public static partial class IocContainer
```

## Pourquoi l'agent a échoué

The agent split the file into `IocContainer.cs` (project part) and `Bia/IocContainer.cs` (BIA part) but forgot to add the `partial` keyword on the project-side class declaration. Both files must declare `public static partial class IocContainer` for the partial class pattern to work.

## Commentaire du développeur

Exemple de fix — ce fichier sert de modèle pour le format attendu dans `migration-fixes/`.
