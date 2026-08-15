# Migrating from Facet to Forge

A pattern-by-pattern migration guide for teams moving from Facet to Forge. Both are Roslyn source generators with zero runtime dependencies — the migration is about API surface and feature differences.

> **Why switch?** Both generate compile-time mapping code. Forge offers more granular control: implicit/explicit mode, side-specific member exclusion, conditional mapping, polymorphic dispatch, dictionary mapping, EF Core expression generation, and reference semantics control — all at compile time.

---

## Table of Contents

1. [Setup & Mapper Declaration](#1-setup--mapper-declaration)
2. [Basic Property Mapping](#2-basic-property-mapping)
3. [Custom Member Mapping](#3-custom-member-mapping)
4. [Ignoring Members](#4-ignoring-members)
5. [Nested Object Mapping](#5-nested-object-mapping)
6. [Collection Mapping](#6-collection-mapping)
7. [Constructor Mapping](#7-constructor-mapping)
8. [Enum Mapping](#8-enum-mapping)
9. [Flattening](#9-flattening)
10. [Update Existing Objects](#10-update-existing-objects)
11. [Before/After Hooks](#11-beforeafter-hooks)
12. [Nullable Handling](#12-nullable-handling)
13. [Features Forge Adds](#13-features-forge-adds)
14. [Migration Checklist](#migration-checklist)

---

## 1. Setup & Mapper Declaration

### Facet

Facet uses a `[FacetMapper]` attribute:

```csharp
using Facet;

[FacetMapper]
public partial class PersonMapper
{
    public partial PersonDto Map(Person source);
}
```

### Forge

```csharp
using FreakyKit.Forge;

[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}
```

**Key differences:**
- Forge requires `static` class (FKF003 error if not)
- Forge generates extension methods by default (`person.ToDto()`)
- No DI or instance creation — everything is static

---

## 2. Basic Property Mapping

### Facet

```csharp
[FacetMapper]
public partial class PersonMapper
{
    public partial PersonDto Map(Person source);
}
```

### Forge

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}
```

Both match properties by name. Generated code is equivalent.

---

## 3. Custom Member Mapping

### Facet

```csharp
[FacetMapper]
public partial class PersonMapper
{
    [MapProperty("FullName", "DisplayName")]
    public partial PersonDto Map(Person source);
}
```

### Forge

Place `[ForgeMap]` on the destination property:

```csharp
public class PersonDto
{
    [ForgeMap("FullName")]
    public string DisplayName { get; set; } = "";
}

[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}
```

**Key difference:** Forge puts mapping configuration on the type, not the mapper method — it travels with the type regardless of which forge class maps it.

---

## 4. Ignoring Members

### Facet

```csharp
[FacetMapper]
public partial class PersonMapper
{
    [IgnoreProperty("InternalId")]
    public partial PersonDto Map(Person source);
}
```

### Forge

```csharp
public class PersonDto
{
    [ForgeIgnore]
    public string InternalId { get; set; } = "";
}
```

**Forge adds:** Side-specific exclusion with `[ForgeIgnore(Side = ForgeIgnoreSide.Source)]` — exclude from one side without affecting the other.

---

## 5. Nested Object Mapping

### Facet

```csharp
[FacetMapper]
public partial class PersonMapper
{
    public partial PersonDto Map(Person source);
    public partial AddressDto Map(Address source);
    // Nested mapping discovered automatically
}
```

### Forge

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
}
```

**Key difference:** Forge requires explicit `AllowNestedForging = true` opt-in to prevent accidental deep traversal.

---

## 6. Collection Mapping

### Facet

```csharp
[FacetMapper]
public partial class OrderMapper
{
    public partial List<OrderDto> Map(List<Order> source);
}
```

### Forge

```csharp
[Forge]
public static partial class OrderForges
{
    public static partial OrderDto ToDto(Order source);
    public static partial List<OrderDto>? ToDtos(List<Order>? source);
}
```

Both handle standard collection types. Forge additionally supports immutable collections (`ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`), `ReadOnlyCollection<T>`, and `Collection<T>`.

---

## 7. Constructor Mapping

### Facet

```csharp
// Facet handles constructors automatically
[FacetMapper]
public partial class RecordMapper
{
    public partial PersonRecord Map(Person source);
}
```

### Forge

```csharp
[Forge]
public static partial class RecordForges
{
    public static partial PersonRecord ToRecord(Person source);
    // Automatic constructor selection — no attribute needed
}
```

For name mismatches, use `[ForgeMap]` on constructor parameters:

```csharp
public class Dest
{
    public Dest([ForgeMap("FullName")] string name) { Name = name; }
    public string Name { get; }
}
```

---

## 8. Enum Mapping

### Facet

Facet maps enums by value.

### Forge

```csharp
// Cast (default — by value)
public static partial Dest ToDto(Source source);
// Generates: __result.Status = (DestStatus)source.Status;

// By name (safer)
[ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
public static partial Dest ToDtoSafe(Source source);

// Enum ↔ string: automatic
// Enum → string: source.Status.ToString()
// String → enum: Enum.Parse<T>(source.Status)
```

---

## 9. Flattening

### Facet

Facet supports flattening by naming convention.

### Forge

```csharp
[Forge]
public static partial class MyForges
{
    [ForgeMethod(AllowFlattening = true)]
    public static partial FlatDto ToDto(Source source);
}
// Generates: __result.AddressCity = source.Address?.City;
```

Supports arbitrary depth (up to 10 levels), automatic null-conditional operators, and ambiguity detection (FKF530).

---

## 10. Update Existing Objects

### Facet

Facet supports update mapping with void returns.

### Forge

```csharp
[Forge]
public static partial class UserForges
{
    public static partial void Update(UpdateRequest source, User existing);
}
```

Same shape — void return with two parameters.

---

## 11. Before/After Hooks

### Facet

Facet supports before/after hooks.

### Forge

Convention-based partial methods:

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);

    static partial void OnBeforeToDto(Person source) { /* before assignments */ }
    static partial void OnAfterToDto(Person source, PersonDto result) { /* after assignments */ }
}
```

---

## 12. Nullable Handling

### Facet

Facet handles nullable conversions.

### Forge

Automatic handling plus `DefaultValue` fallbacks:

```csharp
public class Source
{
    [ForgeMap("Age", DefaultValue = 0)]
    public int? Age { get; set; }
}
// Generates: __result.Age = source.Age ?? 0;
```

---

## 13. Features Forge Adds

These features are available in Forge but not in Facet:

### Implicit/Explicit Mode

```csharp
// Implicit (default) — all shaped methods are forged, zero ceremony
[Forge]
public static partial class MyForges { ... }

// Explicit — only [ForgeMethod]-decorated methods
[Forge(Mode = ForgeMode.Explicit)]
public static partial class MyForges { ... }
```

### Strict Mapping (Drift Detection)

```csharp
[ForgeMethod(StrictMapping = true)]
public static partial PersonDto ToDto(Person source);
// Build errors when types drift apart
```

### Conditional Mapping

```csharp
[ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
public static partial void Update(Source source, Dest existing);

// Per-member custom conditions
[ForgeMap("Salary", Condition = nameof(CanUpdate))]
public decimal? Salary { get; set; }
```

### EF Core Expression Projections

```csharp
[ForgeMethod(GenerateExpression = true)]
public static partial PersonDto ToDto(Person source);
// Generates Expression<Func<Person, PersonDto>> for IQueryable.Select()
```

### Reference Semantics Control

```csharp
[ForgeMethod(ShareReference = ForgePolicy.True)]
public static partial PersonDto ToDto(Person source);
// Per-member override with [ForgeMap(ShareReference = ForgePolicy.False)]
```

### Polymorphic Dispatch

```csharp
[ForgePolymorphic(typeof(Dog), nameof(MapDog))]
[ForgePolymorphic(typeof(Cat), nameof(MapCat))]
public static partial AnimalDto MapAny(Animal source);
```

### Dictionary Mapping

```csharp
[ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase)]
public static partial AppSettings FromDict(Dictionary<string, object> dict);
```

### Cross-Class Method Sharing

```csharp
[ForgeUses(typeof(AddressForges))]
public static partial class PersonForges { ... }
```

### Type Converter Validation

```csharp
[ForgeConverter]
public static string Convert(DateTime value) => value.ToString("yyyy-MM-dd");
// Invalid signatures caught at build time (FKF221)
```

### 85 Build-Time Diagnostics

Full diagnostic coverage across 7 categories — see [diagnostics.md](diagnostics.md).

---

## Migration Checklist

- [ ] **Install packages**: Add `FreakyKit.Forge.Generator` and `FreakyKit.Forge.Analyzers`
- [ ] **Remove Facet**: Uninstall the Facet NuGet package
- [ ] **Convert mapper classes**: Change `[FacetMapper] partial class` to `[Forge] static partial class`
- [ ] **Make methods static**: All forge methods must be `static partial`
- [ ] **Convert `[MapProperty]`**: Move to `[ForgeMap("name")]` on destination properties
- [ ] **Convert `[IgnoreProperty]`**: Use `[ForgeIgnore]` on destination properties
- [ ] **Enable nested forging**: Add `AllowNestedForging = true` where types differ
- [ ] **Enable flattening**: Add `AllowFlattening = true` where name-based flattening is used
- [ ] **Build and fix diagnostics**: The compiler guides you
- [ ] **Explore new features**: Strict mapping, conditional mapping, polymorphic dispatch, EF Core expressions

### Quick Reference: Facet → Forge

| Facet | Forge |
|-------|-------|
| `[FacetMapper]` partial class | `[Forge]` static partial class |
| `[MapProperty("src", "dest")]` | `[ForgeMap("sourceName")]` on destination property |
| `[IgnoreProperty("prop")]` | `[ForgeIgnore]` on destination property |
| Constructor mapping | Automatic (or `[ForgeMap]` on ctor param) |
| Before/after hooks | `OnBefore{Method}`/`OnAfter{Method}` partial methods |
| N/A | `[ForgeMethod(StrictMapping = true)]` — drift detection |
| N/A | `[ForgeMethod(GenerateExpression = true)]` — EF Core |
| N/A | `[ForgePolymorphic]` — inheritance dispatch |
| N/A | `[ForgeDictionary]` — dictionary mapping |
| N/A | `[ForgeUses]` — cross-class sharing |
| N/A | `[ForgeConverter]` — validated type converters |
| N/A | `ForgeIgnoreSide` — side-specific exclusion |
| N/A | `ForgeMode.Explicit` — opt-in method selection |
