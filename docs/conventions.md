# Conventions

The `FreakyKit.Forge.Conventions` project provides optional naming conventions for organizing forge classes and methods. These are **advisory only** — they have no effect on the forge pipeline.

**Namespace:** `FreakyKit.Forge.Conventions`

Install via NuGet:

```xml
<PackageReference Include="FreakyKit.Forge.Conventions" Version="1.0.0" />
```

This package depends on `FreakyKit.Forge` (core attributes), which will be installed automatically.

```csharp
using FreakyKit.Forge.Conventions;
```

## Recommended Class Naming

Forge classes should be suffixed with `Forges`:

```csharp
PersonForges
OrderForges
InvoiceForges
```

Use `ForgeConventions.ForgeClassName("Person")` to get the recommended name:

```csharp
ForgeConventions.ForgeClassName("Person")   // "PersonForges"
ForgeConventions.ForgeClassName("Order")    // "OrderForges"
```

## Recommended Method Naming

Forge methods should be prefixed with `To`:

```csharp
ToDto
ToViewModel
ToEntity
```

Use `ForgeConventions.ForgeMethodName("PersonDto")` to get the recommended name:

```csharp
ForgeConventions.ForgeMethodName("PersonDto")    // "ToPersonDto"
ForgeConventions.ForgeMethodName("ViewModel")    // "ToViewModel"
```

## Hook Naming

Before and after hooks follow a convention-based naming pattern:

- **Before hook:** `OnBefore{MethodName}` — e.g., `OnBeforeToDto`
- **After hook:** `OnAfter{MethodName}` — e.g., `OnAfterToDto`

```csharp
[ForgeClass]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);

    // Implement these partial methods to run custom logic
    static partial void OnBeforeToDto(Person source);
    static partial void OnAfterToDto(Person source, PersonDto result);
}
```

## API

```csharp
public static class ForgeConventions
{
    public const string RecommendedClassSuffix = "Forges";
    public const string RecommendedMethodPrefix = "To";

    public static string ForgeClassName(string typeName);
    public static string ForgeMethodName(string destinationTypeName);
}
```
