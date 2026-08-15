# Migrating from Mapperly to Forge

A pattern-by-pattern migration guide for teams moving from Mapperly to Forge. Both are Roslyn source generators — the migration is about API differences, not a paradigm shift.

> **Why switch?** Both generate compile-time mapping code with zero runtime overhead. The difference is in control: Forge offers implicit/explicit mode selection, side-specific member exclusion, and dictionary mapping out of the box. If you want more granular control over what gets generated and how, read on.

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
11. [Type Converters](#11-type-converters)
12. [Before/After Hooks](#12-beforeafter-hooks)
13. [Strict Mapping](#13-strict-mapping)
14. [EF Core / IQueryable Projections](#14-ef-core--iqueryable-projections)
15. [Nullable Handling](#15-nullable-handling)
16. [Implicit vs Explicit Mode](#16-implicit-vs-explicit-mode)
17. [Dictionary Mapping](#17-dictionary-mapping)
18. [Polymorphic Mapping](#18-polymorphic-mapping)
19. [Conditional Mapping](#19-conditional-mapping)
20. [Cross-Class Method Sharing](#20-cross-class-method-sharing)
21. [Reference Semantics](#21-reference-semantics)
22. [Migration Checklist](#migration-checklist)

---

## 1. Setup & Mapper Declaration

### Mapperly

Mapperly uses a `[Mapper]` attribute on a partial class. The class is not required to be static:

```csharp
using Riok.Mapperly.Abstractions;

[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);
}
```

### Forge

Forge requires a `static partial class` with `[Forge]`:

```csharp
using FreakyKit.Forge;

[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}
```

**Key differences:**
- Forge class must be `static` — enforced at build time (FKF003)
- Forge class must be `partial` — enforced at build time (FKF004)
- Extension methods are generated automatically by default (`person.ToDto()`)
- No DI or instance creation needed — everything is static

---

## 2. Basic Property Mapping

### Mapperly

```csharp
[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);
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

Both match properties by name (case-insensitive). The generated code is nearly identical.

---

## 3. Custom Member Mapping

### Mapperly

```csharp
[Mapper]
public partial class PersonMapper
{
    [MapProperty(nameof(Person.FullName), nameof(PersonDto.DisplayName))]
    public partial PersonDto ToDto(Person source);
}
```

### Forge

Place `[ForgeMap]` on the **destination property** (or source property, or constructor parameter):

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

**Key difference:** Mapperly puts the mapping configuration on the mapper method. Forge puts it on the type's property — the mapping intent lives with the type, not scattered across mapper classes.

---

## 4. Ignoring Members

### Mapperly

```csharp
[Mapper]
public partial class PersonMapper
{
    [MapperIgnoreTarget(nameof(PersonDto.InternalId))]
    [MapperIgnoreSource(nameof(Person.AuditField))]
    public partial PersonDto ToDto(Person source);
}
```

### Forge

```csharp
public class PersonDto
{
    [ForgeIgnore]
    public string InternalId { get; set; } = "";  // excluded from mapping
}

public class Person
{
    [ForgeIgnore(Side = ForgeIgnoreSide.Source)]
    public string AuditField { get; set; } = "";  // not mapped from source
}
```

**Forge's advantage:** `ForgeIgnoreSide` lets you exclude a member from one side without hiding it on the other — something Mapperly's `MapperIgnoreTarget`/`MapperIgnoreSource` also supports, but Forge additionally allows `ForgeIgnoreSide.Both` as a default.

---

## 5. Nested Object Mapping

### Mapperly

```csharp
[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);
    public partial AddressDto ToAddressDto(Address source);
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

**Key difference:** Forge requires explicit opt-in with `AllowNestedForging = true`. This prevents accidental deep traversal and gives you a build-time warning (FKF300) when a nested method exists but isn't enabled.

---

## 6. Collection Mapping

### Mapperly

```csharp
[Mapper]
public partial class OrderMapper
{
    public partial List<OrderDto> ToDtos(List<Order> source);
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

Both handle the same collection types. Collection properties on objects are mapped automatically when `AllowNestedForging = true`.

---

## 7. Constructor Mapping

### Mapperly

```csharp
[Mapper]
public partial class PersonMapper
{
    [MapperConstructor]
    public partial PersonRecord ToRecord(Person source);
}
```

### Forge

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonRecord ToRecord(Person source);
    // Constructor selected automatically — no attribute needed
}
```

Forge selects the best constructor automatically. Use `[ForgeMap]` on constructor parameters when names don't match:

```csharp
public class Dest
{
    public Dest([ForgeMap("FullName")] string name) { Name = name; }
    public string Name { get; }
}
```

---

## 8. Enum Mapping

### Mapperly

```csharp
[Mapper]
public partial class MyMapper
{
    [MapEnum(EnumMappingStrategy.ByName)]
    public partial DestEnum MapEnum(SourceEnum source);
}
```

### Forge

```csharp
[Forge]
public static partial class MyForges
{
    // Default: cast (by value)
    public static partial Dest ToDto(Source source);
    // Generates: __result.Status = (DestStatus)source.Status;

    // By name
    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
    public static partial Dest ToDtoSafe(Source source);
    // Generates: switch expression mapping each member by name
}
```

Forge also handles **enum-to-string** and **string-to-enum** automatically, with optional `DefaultValue` fallback.

---

## 9. Flattening

### Mapperly

```csharp
[Mapper]
public partial class MyMapper
{
    [MapProperty("Address.City", "AddressCity")]
    public partial FlatDto ToDto(Source source);
}
```

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

**Forge's advantage:** Automatic name-based discovery — you don't need to specify each flattened path. Supports arbitrary depth (up to 10 levels) with automatic null-conditional operators.

---

## 10. Update Existing Objects

### Mapperly

```csharp
[Mapper]
public partial class UserMapper
{
    public partial void Update(UpdateRequest source, User target);
}
```

### Forge

```csharp
[Forge]
public static partial class UserForges
{
    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
    public static partial void Update(UpdateRequest source, User existing);
}
```

Same shape — void return with two parameters.

---

## 11. Type Converters

### Mapperly

```csharp
[Mapper]
public partial class MyMapper
{
    public partial Dest ToDto(Source source);

    // User-implemented method — Mapperly calls it for DateTime → string conversion
    private string DateToString(DateTime value) => value.ToString("yyyy-MM-dd");
}
```

### Forge

```csharp
[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);

    [ForgeConverter]
    public static string DateToString(DateTime value) => value.ToString("yyyy-MM-dd");
}
```

**Key difference:** Forge requires the explicit `[ForgeConverter]` attribute. Invalid converter signatures are caught at build time (FKF221).

---

## 12. Before/After Hooks

### Mapperly

```csharp
[Mapper]
public partial class PersonMapper
{
    [BeforeMap]
    private void Validate(Person source) { /* ... */ }

    [AfterMap]
    private void Enrich(PersonDto dto) { /* ... */ }

    public partial PersonDto ToDto(Person source);
}
```

### Forge

Convention-based — name the methods `OnBefore{MethodName}` / `OnAfter{MethodName}`:

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);

    static partial void OnBeforeToDto(Person source) { /* ... */ }
    static partial void OnAfterToDto(Person source, PersonDto result) { /* ... */ }
}
```

**Key difference:** Mapperly uses attributes; Forge uses naming convention. Both are compile-time checked.

---

## 13. Strict Mapping

### Mapperly

```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
public partial class StrictMapper
{
    public partial PersonDto ToDto(Person source);
}
```

### Forge

```csharp
[Forge]
public static partial class StrictForges
{
    [ForgeMethod(StrictMapping = true)]
    public static partial PersonDto ToDto(Person source);
}
```

Both escalate unmapped members to errors. Forge controls this per-method rather than per-class.

---

## 14. EF Core / IQueryable Projections

### Mapperly

```csharp
[Mapper]
public partial class PersonMapper
{
    [MapperIgnoreSource(nameof(Person.InternalField))]
    public partial IQueryable<PersonDto> ProjectToDto(IQueryable<Person> query);
}
```

### Forge

```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonDto ToDto(Person source);
}

// Usage:
var dtos = await dbContext.People
    .Select(PersonForges.ToDtoExpression)
    .ToListAsync();
```

**Key difference:** Mapperly generates an `IQueryable` extension. Forge generates a static `Expression<Func<S, D>>` property — the same method works both as imperative code and as an EF Core projection.

---

## 15. Nullable Handling

### Mapperly

```csharp
// Mapperly handles nullable conversions automatically
[Mapper]
public partial class MyMapper
{
    public partial Dest ToDto(Source source);
}
```

### Forge

Same automatic handling, plus `DefaultValue` for fallbacks:

```csharp
public class Source
{
    [ForgeMap("Age", DefaultValue = 0)]
    public int? Age { get; set; }
}
// Generates: __result.Age = source.Age ?? 0;
```

---

## 16. Implicit vs Explicit Mode

### Mapperly

Mapperly always requires the method to be declared in the mapper class. There's no distinction between implicit and explicit.

### Forge

```csharp
// Implicit (default) — all shaped methods are forged
[Forge]
public static partial class MyForges
{
    public static partial PersonDto ToDto(Person source);      // forged
    public static partial AddressDto ToAddr(Address source);   // also forged
}

// Explicit — only [ForgeMethod]-decorated methods
[Forge(Mode = ForgeMode.Explicit)]
public static partial class MyForges
{
    [ForgeMethod]
    public static partial PersonDto ToDto(Person source);      // forged

    public static partial AddressDto ToAddr(Address source);   // ignored (FKF002 warning)
}
```

**Forge's advantage:** Implicit mode means zero ceremony for simple cases. Explicit mode locks down critical paths.

---

## 17. Dictionary Mapping

### Mapperly

Mapperly doesn't have built-in dictionary-to-object or object-to-dictionary mapping.

### Forge

```csharp
[Forge]
public static partial class ConfigForges
{
    [ForgeMethod]
    [ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase)]
    public static partial AppSettings FromDict(Dictionary<string, object> dict);

    [ForgeMethod]
    [ForgeDictionary(NullValue = NullValuePolicy.Skip)]
    public static partial Dictionary<string, object> ToDict(AppSettings settings);
}
```

---

## 18. Polymorphic Mapping

### Mapperly

Mapperly doesn't have built-in polymorphic dispatch. You'd need to manually implement switch logic.

### Forge

```csharp
[Forge]
public static partial class AnimalForges
{
    public static partial DogDto MapDog(Dog source);
    public static partial CatDto MapCat(Cat source);

    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
    public static partial AnimalDto MapAny(Animal source);
}
```

---

## 19. Conditional Mapping

### Mapperly

Mapperly doesn't have built-in conditional mapping (IgnoreIfNull, IgnoreIfDefault, Condition).

### Forge

```csharp
// Method-level
[ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
public static partial void Update(Source source, Dest existing);

// Per-member custom condition
public class Dest
{
    [ForgeMap("Salary", Condition = nameof(PersonForges.CanUpdateSalary))]
    public decimal? Salary { get; set; }
}
```

---

## 20. Cross-Class Method Sharing

### Mapperly

Mapperly mappers are independent classes. Cross-class method sharing requires manual wiring.

### Forge

```csharp
[Forge]
public static partial class AddressForges
{
    public static partial AddressDto ToAddressDto(Address source);
}

[Forge]
[ForgeUses(typeof(AddressForges))]
public static partial class PersonForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
}
```

---

## 21. Reference Semantics

### Mapperly

Mapperly deep-copies collections by default. No per-member opt-out.

### Forge

```csharp
// Default: deep-copy (same as Mapperly)
// Opt into reference-sharing for hot paths:
[ForgeMethod(ShareReference = ForgePolicy.True)]
public static partial PersonDto ToDto(Person source);

// Per-member override:
public class Dest
{
    [ForgeMap("History", ShareReference = ForgePolicy.False)]
    public List<string> History { get; set; } = new();
}
```

---

## Migration Checklist

- [ ] **Install packages**: Add `FreakyKit.Forge.Generator` and `FreakyKit.Forge.Analyzers`
- [ ] **Remove Mapperly**: Uninstall `Riok.Mapperly`
- [ ] **Convert mapper classes**: Change `[Mapper] partial class` to `[Forge] static partial class`
- [ ] **Make methods static**: All forge methods must be `static partial`
- [ ] **Convert `[MapProperty]`**: Move to `[ForgeMap("name")]` on destination properties
- [ ] **Convert `[MapperIgnoreTarget]`/`[MapperIgnoreSource]`**: Use `[ForgeIgnore]` with optional `Side`
- [ ] **Convert `[MapEnum]`**: Use `[ForgeMethod(MappingStrategy = ForgeMapping.ByName)]`
- [ ] **Convert user-implemented converters**: Add `[ForgeConverter]` attribute
- [ ] **Convert `[BeforeMap]`/`[AfterMap]`**: Rename to `OnBefore{Method}`/`OnAfter{Method}` convention
- [ ] **Enable nested forging**: Add `AllowNestedForging = true` where needed
- [ ] **Enable flattening**: Add `AllowFlattening = true` where needed
- [ ] **Convert IQueryable projections**: Replace `ProjectToDto(IQueryable)` with `GenerateExpression = true` and `.Select(Expression)`
- [ ] **Link cross-class methods**: Use `[ForgeUses(typeof(...))]`
- [ ] **Build and fix diagnostics**: The compiler guides you

### Quick Reference: Mapperly → Forge

| Mapperly | Forge |
|----------|-------|
| `[Mapper]` partial class | `[Forge]` static partial class |
| `[MapProperty("src", "dest")]` | `[ForgeMap("sourceName")]` on destination property |
| `[MapperIgnoreTarget("prop")]` | `[ForgeIgnore]` on destination property |
| `[MapperIgnoreSource("prop")]` | `[ForgeIgnore(Side = ForgeIgnoreSide.Source)]` on source property |
| `[MapEnum(ByName)]` | `[ForgeMethod(MappingStrategy = ForgeMapping.ByName)]` |
| `[MapperConstructor]` | Automatic (or `[ForgeMap]` on ctor param) |
| `[BeforeMap]`/`[AfterMap]` | `OnBefore{Method}`/`OnAfter{Method}` partial methods |
| `RequiredMappingStrategy.Both` | `[ForgeMethod(StrictMapping = true)]` |
| `ProjectToDto(IQueryable)` | `GenerateExpression = true` + `.Select(Expression)` |
| User-implemented method | `[ForgeConverter]` static method |
