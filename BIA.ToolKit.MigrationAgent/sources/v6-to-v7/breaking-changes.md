# Breaking Changes: V6 to V7

## IocContainer Split (CRITICAL)

### What changed

In V6, `IocContainer.cs` is a single monolithic file in `{CompanyName}.{ProjectName}.Crosscutting.Ioc/`.

In V7, it is split into two files using `partial class`:

| File | Location | Owner | Purpose |
|---|---|---|---|
| `IocContainer.cs` | `Crosscutting.Ioc/` | **Developer** | Thin orchestrator + custom registrations |
| `IocContainer.cs` | `Crosscutting.Ioc/Bia/` | **BIA Framework** | All BIA boilerplate registrations |

### Method signature changes

The `ParamIocContainer` parameter object replaces individual parameters:

**V6:**
```csharp
public static void ConfigureContainer(
    IServiceCollection collection,
    IConfiguration configuration,
    bool isApi,
    bool isUnitTest = false)
```

**V7:**
```csharp
public static void ConfigureContainer(ParamIocContainer param)
```

**Parameter mapping:**
| V6 | V7 |
|---|---|
| `collection` | `param.Collection` |
| `configuration` | `param.Configuration` |
| `isApi` | `param.IsApi` |
| `isUnitTest` | `param.IsUnitTest` |
| `biaNetSection` | `param.BiaNetSection` |

### New usings required

```csharp
using BIA.Net.Core.Ioc.Param;
using {CompanyName}.{ProjectName}.Crosscutting.Ioc.Bia.Param;
```

### Class declaration

**V6:** `public static class IocContainer`
**V7:** `public static partial class IocContainer`

### New method pattern

Each `Configure*` method in the project-level file now delegates to `Bia*` methods:

```csharp
private static void ConfigureApplicationContainer(ParamIocContainer param)
{
    BiaConfigureApplicationContainer(param);
    BiaConfigureApplicationContainerAutoRegister(GetGlobalParamAutoRegister(param));

    // Developer custom registrations go here
}
```

### New helper method

```csharp
private static ParamAutoRegister GetGlobalParamAutoRegister(ParamIocContainer param)
{
    return new ParamAutoRegister()
    {
        Collection = param.Collection,
        ExcludedServiceNames = null,
        IncludedServiceNames = null,
    };
}
```

### New types introduced

- `ParamIocContainer` — encapsulates all IoC configuration parameters
- `ParamAutoRegister` — parameters for auto-registration of services from assemblies

### Callers of ConfigureContainer must be updated

Any code that calls `IocContainer.ConfigureContainer(collection, configuration, isApi, isUnitTest)` must be updated to create a `ParamIocContainer` and pass it instead. Typical locations:
- `Startup.cs` / `Program.cs`
- `*Api/Startup.cs`
- `*Worker/Startup.cs`
- Unit test IoC setup

## Other .NET Changes

<!-- TODO: Fill with actual changes when known -->

### Target Framework
- V6: net8.0
- V7: net9.0 (to confirm)

### Removed APIs
<!-- List any removed APIs here -->

### Renamed Namespaces
<!-- List any namespace renames here -->

## Angular Changes

<!-- TODO: Fill with actual Angular changes when known -->

### Angular Version
- V6: Angular 17/18
- V7: Angular 19 (to confirm)

## Configuration Changes

<!-- TODO: Fill with any appsettings.json changes -->
