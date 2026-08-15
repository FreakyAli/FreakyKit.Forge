# Migrating from AutoMapper to Forge

A pattern-by-pattern migration guide for teams moving from AutoMapper to compile-time mapping with Forge. Each section shows the AutoMapper pattern you're used to, then the Forge equivalent — copy-paste and adapt.

> **Why migrate?** AutoMapper uses runtime reflection. Forge uses Roslyn source generators — your mapping code is generated at compile time, runs as plain C# assignments, and gives you 85 build-time diagnostics instead of runtime surprises. See [benchmarks](benchmarks.md) for the performance difference.

---

## Table of Contents

1. [Setup & Registration](#1-setup--registration)
2. [Basic Property Mapping](#2-basic-property-mapping)
3. [Custom Member Mapping (ForMember → ForgeMap)](#3-custom-member-mapping)
4. [Ignoring Members](#4-ignoring-members)
5. [Reverse Mapping](#5-reverse-mapping)
6. [Calling the Mapper](#6-calling-the-mapper)
7. [Nested Object Mapping](#7-nested-object-mapping)
8. [Collection Mapping](#8-collection-mapping)
9. [Constructor Mapping](#9-constructor-mapping)
10. [Null Substitution / Default Values](#10-null-substitution--default-values)
11. [Conditional Mapping](#11-conditional-mapping)
12. [Custom Value Resolvers / Type Converters](#12-custom-value-resolvers--type-converters)
13. [Profiles & Cross-Class Reuse](#13-profiles--cross-class-reuse)
14. [BeforeMap / AfterMap Hooks](#14-beforemap--aftermap-hooks)
15. [Flattening](#15-flattening)
16. [Enum Mapping](#16-enum-mapping)
17. [Update Existing Objects](#17-update-existing-objects)
18. [Strict Mapping / Configuration Validation](#18-strict-mapping--configuration-validation)
19. [EF Core / IQueryable Projections](#19-ef-core--iqueryable-projections)
20. [Record & Init-Only Types](#20-record--init-only-types)
21. [Polymorphic / Inheritance Mapping](#21-polymorphic--inheritance-mapping)
22. [Dictionary Mapping](#22-dictionary-mapping)
23. [Migration Checklist](#migration-checklist)

---

## 1. Setup & Registration

### AutoMapper

AutoMapper requires runtime configuration — either a `MapperConfiguration` or `IServiceCollection` registration:

```csharp
// Program.cs or Startup.cs
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Or manually:
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Person, PersonDto>();
    cfg.CreateMap<Address, AddressDto>();
});
var mapper = config.CreateMapper();
```

### Forge

No registration, no DI, no startup cost. Declare your mappings; the source generator handles everything at compile time:

```xml
<!-- Add to your .csproj -->
<PackageReference Include="FreakyKit.Forge.Generator" Version="1.5.0" />
<PackageReference Include="FreakyKit.Forge.Analyzers" Version="1.5.0" />
```

```csharp
using FreakyKit.Forge;

[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    public static partial AddressDto ToAddressDto(Address source);
}
```

**Key difference:** AutoMapper maps are registered at runtime and validated at startup. Forge mappings are validated at compile time — if something is wrong, you see it before the code ever runs.

---

## 2. Basic Property Mapping

### AutoMapper

```csharp
cfg.CreateMap<Person, PersonDto>();
```

### Forge

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}
```

Properties are matched by name (case-insensitive). No configuration needed for matching names.

---

## 3. Custom Member Mapping

### AutoMapper

```csharp
cfg.CreateMap<Person, PersonDto>()
    .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.FullName));
```

### Forge

Place `[ForgeMap]` on the **destination** property:

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
// Generates: __result.DisplayName = source.FullName;
```

**Three placement options:**

```csharp
// 1. On destination property — "read from this source member"
public class Dest { [ForgeMap("FullName")] public string Name { get; set; } = ""; }

// 2. On source property — "write to this destination member"
public class Source { [ForgeMap("Name")] public string FullName { get; set; } = ""; }

// 3. On constructor parameter — "match this constructor param to a source member"
public class Dest
{
    public string Name { get; }
    public Dest([ForgeMap("FullName")] string name) { Name = name; }
}
```

---

## 4. Ignoring Members

### AutoMapper

```csharp
cfg.CreateMap<Person, PersonDto>()
    .ForMember(dest => dest.InternalId, opt => opt.Ignore());
```

### Forge

```csharp
public class PersonDto
{
    [ForgeIgnore]
    public string InternalId { get; set; } = "";  // excluded from mapping
}
```

**Side-specific exclusion** (something AutoMapper doesn't offer cleanly):

```csharp
public class Source
{
    [ForgeIgnore(Side = ForgeIgnoreSide.Source)]
    public string AuditField { get; set; } = "";  // not mapped FROM source, but dest can map TO it
}

public class Dest
{
    [ForgeIgnore(Side = ForgeIgnoreSide.Destination)]
    public int ComputedScore { get; set; }  // not populated by forge, but source participates in matching
}
```

---

## 5. Reverse Mapping

### AutoMapper

```csharp
cfg.CreateMap<Person, PersonDto>()
    .ReverseMap();
```

### Forge

Declare both directions explicitly — it's clearer and safer:

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    public static partial Person FromDto(PersonDto source);
}
```

**Why explicit?** AutoMapper's `.ReverseMap()` can silently produce wrong mappings when the types aren't symmetric. Two explicit methods make both directions visible, testable, and independently configurable.

---

## 6. Calling the Mapper

### AutoMapper

```csharp
// Requires IMapper from DI
var dto = mapper.Map<PersonDto>(person);
```

### Forge

```csharp
// Static call — no DI needed
var dto = PersonForges.ToDto(person);

// Or extension method (generated by default)
var dto = person.ToDto();
```

Extension methods are generated automatically. Disable with `[Forge(GenerateExtensionMethods = false)]`.

---

## 7. Nested Object Mapping

### AutoMapper

```csharp
cfg.CreateMap<Person, PersonDto>();
cfg.CreateMap<Address, AddressDto>();
// AutoMapper discovers nested mappings automatically
```

### Forge

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
    // Generates: __result.Home = source.Home != null ? ToAddressDto(source.Home) : null;
}
```

**`AllowNestedForging = true`** is opt-in — Forge tells you at build time (FKF300) when a nested forge method exists but you haven't enabled it. This prevents accidental deep object graph traversal.

For methods in separate classes, use `[ForgeUses]`:

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
    // Discovers ToAddressDto from AddressForges
}
```

---

## 8. Collection Mapping

### AutoMapper

```csharp
// AutoMapper handles collections automatically once element mapping is configured
cfg.CreateMap<Order, OrderDto>();
var dtos = mapper.Map<List<OrderDto>>(orders);
```

### Forge

```csharp
[Forge]
public static partial class OrderForges
{
    public static partial OrderDto ToDto(Order source);

    // Top-level collection projection — optional, for bulk mapping
    public static partial List<OrderDto>? ToDtos(List<Order>? source);
}

// Usage:
var dtos = OrderForges.ToDtos(orders);
```

Collection properties on objects are handled automatically when `AllowNestedForging = true`:

```csharp
// Person has: public List<Address> Addresses { get; set; }
// PersonDto has: public List<AddressDto> Addresses { get; set; }

[ForgeMethod(AllowNestedForging = true)]
public static partial PersonDto ToDto(Person source);
// Generates: __result.Addresses = source.Addresses?.Select(x => ToAddressDto(x)).ToList();
```

**Supported collection types:** `List<T>`, `T[]`, `HashSet<T>`, `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`, `ReadOnlyCollection<T>`, `Collection<T>`.

---

## 9. Constructor Mapping

### AutoMapper

```csharp
cfg.CreateMap<Source, Dest>()
    .ForCtorParam("name", opt => opt.MapFrom(src => src.FullName));
```

### Forge

Forge automatically selects the best constructor. Use `[ForgeMap]` on constructor parameters when names differ:

```csharp
public class Dest
{
    public string Name { get; }
    public int Age { get; }
    public Dest([ForgeMap("FullName")] string name, int age)
    {
        Name = name;
        Age = age;
    }
}

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}
// Generates: var __result = new Dest(source.FullName, source.Age);
```

**Constructor selection rules:**
1. Parameterless constructor preferred
2. Single parameterized constructor that can be fully satisfied
3. Build error if ambiguous (FKF500) or unsatisfiable (FKF501/FKF502)

---

## 10. Null Substitution / Default Values

### AutoMapper

```csharp
cfg.CreateMap<Source, Dest>()
    .ForMember(dest => dest.Name, opt => opt.NullSubstitute("N/A"));
```

### Forge

Use `DefaultValue` on `[ForgeMap]` for nullable-to-non-nullable mappings:

```csharp
public class Source
{
    [ForgeMap("Age", DefaultValue = 0)]
    public int? Age { get; set; }
}

public class Dest { public int Age { get; set; } }
// Generates: __result.Age = source.Age ?? 0;
```

For enum fallbacks:

```csharp
public class Dest
{
    [ForgeMap("Status", DefaultValue = OrderStatus.Unknown)]
    public OrderStatus Status { get; set; }
}
// Generates: Enum.TryParse<OrderStatus>(source.Status, out var __parsed)
//     ? __parsed : OrderStatus.Unknown;
```

---

## 11. Conditional Mapping

### AutoMapper

```csharp
cfg.CreateMap<Source, Dest>()
    .ForMember(dest => dest.Name, opt => opt.Condition(src => src.Name != null))
    .ForMember(dest => dest.Salary, opt => opt.Condition(src => src.IsManager));
```

### Forge

**Null-check (most common):**

```csharp
// Method-level — all assignments get null checks
[ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
public static partial void Update(Source source, Dest existing);
// Generates: if (source.Name != null) existing.Name = source.Name;

// Per-member
public class Dest
{
    [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)]
    public string? Name { get; set; }
}
```

**Default-check (PATCH APIs):**

```csharp
public class Dest
{
    [ForgeMap("Age", IgnoreIfDefault = true)]
    public int Age { get; set; }
}
// Generates: if (!EqualityComparer<int>.Default.Equals(source.Age, default)) __result.Age = source.Age;
```

**Custom condition:**

```csharp
public class Dest
{
    [ForgeMap("Salary", Condition = nameof(PersonForges.CanUpdateSalary))]
    public decimal? Salary { get; set; }
}

[Forge]
public static partial class PersonForges
{
    public static partial Dest ToDto(Person source);

    internal static bool CanUpdateSalary(Person source) => source.IsManager;
}
// Generates: if (CanUpdateSalary(source)) __result.Salary = source.Salary;
```

---

## 12. Custom Value Resolvers / Type Converters

### AutoMapper

```csharp
// Value resolver
public class DateResolver : IValueResolver<Source, Dest, string>
{
    public string Resolve(Source source, Dest destination, string destMember, ResolutionContext context)
        => source.Birthday.ToString("yyyy-MM-dd");
}

cfg.CreateMap<Source, Dest>()
    .ForMember(dest => dest.Birthday, opt => opt.MapFrom<DateResolver>());
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
// Generates: __result.Birthday = DateToString(source.Birthday);
```

**Key differences:**
- No class boilerplate — just a static method
- Must be `static`, non-void, non-generic, exactly one parameter
- Converter is discovered automatically when source and destination types match
- Invalid signatures are caught at build time (FKF221)

---

## 13. Profiles & Cross-Class Reuse

### AutoMapper

```csharp
public class AddressProfile : Profile
{
    public AddressProfile()
    {
        CreateMap<Address, AddressDto>();
    }
}

public class PersonProfile : Profile
{
    public PersonProfile()
    {
        CreateMap<Person, PersonDto>();
        // AutoMapper discovers AddressProfile automatically via assembly scanning
    }
}
```

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
    // Discovers ToAddressDto from AddressForges
}
```

**Key differences:**
- `[ForgeUses]` replaces implicit assembly scanning with explicit inclusion
- Order matters — first class with a matching method wins
- Shadowed methods emit a warning (FKF523) so you know about priority

---

## 14. BeforeMap / AfterMap Hooks

### AutoMapper

```csharp
cfg.CreateMap<Person, PersonDto>()
    .BeforeMap((src, dest) => { /* validate */ })
    .AfterMap((src, dest) => { dest.MappedAt = DateTime.UtcNow; });
```

### Forge

Convention-based partial methods — no lambdas, no runtime delegates:

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);

    static partial void OnBeforeToDto(Person source)
    {
        // Runs before any assignments
    }

    static partial void OnAfterToDto(Person source, PersonDto result)
    {
        result.MappedAt = DateTime.UtcNow;
    }
}
```

**Naming convention:** `OnBefore{MethodName}` and `OnAfter{MethodName}`. The compiler resolves these — if you spell the name wrong, you get a build error, not a silent miss.

---

## 15. Flattening

### AutoMapper

```csharp
// AutoMapper flattens by convention when destination property names match
// the concatenated source path: Address.City → AddressCity
cfg.CreateMap<Person, PersonFlatDto>();
```

### Forge

Flattening is opt-in per method:

```csharp
public class Source { public Address Address { get; set; } = new(); }
public class Address { public string City { get; set; } = ""; }
public class FlatDto { public string AddressCity { get; set; } = ""; }

[Forge]
public static partial class MyForges
{
    [ForgeMethod(AllowFlattening = true)]
    public static partial FlatDto ToDto(Source source);
}
// Generates: __result.AddressCity = source.Address?.City;
```

Supports arbitrary depth (up to 10 levels), with automatic null-conditional operators for reference-type intermediates:

```csharp
// Three levels deep:
// source.Address?.Coords?.Point?.Code → dest.AddressCoordsPointCode
```

---

## 16. Enum Mapping

### AutoMapper

```csharp
// AutoMapper maps enums by value (cast) by default
cfg.CreateMap<Source, Dest>();
```

### Forge

```csharp
// Default: cast (same as AutoMapper)
[ForgeMethod]
public static partial Dest ToDto(Source source);
// Generates: __result.Status = (DestStatus)source.Status;

// Name-based (safer when underlying values differ)
[ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
public static partial Dest ToDto(Source source);
// Generates a switch expression mapping each member by name
```

Forge also handles **enum-to-string** and **string-to-enum** automatically:

```csharp
// Enum → string: source.Status.ToString()
// String → enum: Enum.Parse<OrderStatus>(source.Status)
// String → enum with fallback: [ForgeMap("Status", DefaultValue = OrderStatus.Unknown)]
```

---

## 17. Update Existing Objects

### AutoMapper

```csharp
mapper.Map(source, existingDest);
```

### Forge

Declare a void method with two parameters:

```csharp
[Forge]
public static partial class UserForges
{
    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
    public static partial void Update(UpdateRequest source, User existing);
}
// Generates:
//   if (source.Name != null) existing.Name = source.Name;
//   if (source.Email != null) existing.Email = source.Email;

// Usage:
UserForges.Update(request, existingUser);
```

---

## 18. Strict Mapping / Configuration Validation

### AutoMapper

```csharp
// Runtime validation — throws at startup if misconfigured
cfg.AssertConfigurationIsValid();
```

### Forge

Compile-time drift detection — no startup cost, no runtime surprises:

```csharp
[Forge]
public static partial class MyForges
{
    [ForgeMethod(StrictMapping = true)]
    public static partial PersonDto ToDto(Person source);
}
```

With `StrictMapping = true`:
- **FKF110** (Error): Every destination member must have a matching source member
- **FKF111** (Error): Every source member must be used or explicitly excluded with `[ForgeIgnore]`

This catches the same class of bugs as `AssertConfigurationIsValid()` — but at **compile time**, not test time.

---

## 19. EF Core / IQueryable Projections

### AutoMapper

```csharp
// AutoMapper.Extensions.Microsoft.DependencyInjection
var dtos = await dbContext.People
    .ProjectTo<PersonDto>(mapper.ConfigurationProvider)
    .ToListAsync();
```

### Forge

```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonDto ToDto(Person source);
}

// The generator emits a static Expression property:
var dtos = await dbContext.People
    .Where(p => p.IsActive)
    .Select(PersonForges.ToDtoExpression)
    .ToListAsync();
```

**Key differences:**
- No extra NuGet package needed
- Expression is generated at compile time — no reflection, no runtime expression building
- Same method works both as imperative code and as an EF Core projection
- Requires EF Core 8+

---

## 20. Record & Init-Only Types

### AutoMapper

```csharp
// AutoMapper handles records/init-only via constructor mapping
cfg.CreateMap<Source, PersonRecord>();
```

### Forge

```csharp
public record PersonRecord(string Name, int Age);

[Forge]
public static partial class RecordForges
{
    public static partial PersonRecord ToRecord(Source source);
}
// Generates: var __result = new PersonRecord(source.Name, source.Age);
```

Init-only properties use object initializer syntax automatically. No configuration needed.

---

## 21. Polymorphic / Inheritance Mapping

### AutoMapper

```csharp
cfg.CreateMap<Animal, AnimalDto>()
    .Include<Dog, DogDto>()
    .Include<Cat, CatDto>();

cfg.CreateMap<Dog, DogDto>();
cfg.CreateMap<Cat, CatDto>();
```

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
// Generates:
// return source switch
// {
//     Dog __p0 => MapDog(__p0),
//     Cat __p1 => MapCat(__p1),
//     _ => throw new InvalidOperationException(...)
// };
```

**Key differences:**
- Dispatch order is explicit — you control which derived types are checked first
- Default arm throws (no silent fallback to base mapping)
- Add a base fallback explicitly: `[ForgePolymorphic(typeof(Animal), nameof(MapBase))]`
- Each arm's return type must be assignable to the dispatch method's return type

---

## 22. Dictionary Mapping

### AutoMapper

AutoMapper doesn't have built-in dictionary-to-object mapping.

### Forge

```csharp
[Forge]
public static partial class ConfigForges
{
    // Dictionary → object (e.g., from JSON/config)
    [ForgeMethod]
    [ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase, MissingKey = MissingKeyPolicy.UseDefault)]
    public static partial AppSettings FromDict(Dictionary<string, object> dict);

    // Object → dictionary
    [ForgeMethod]
    [ForgeDictionary(NullValue = NullValuePolicy.Skip)]
    public static partial Dictionary<string, object> ToDict(AppSettings settings);
}
```

Supports `Dictionary<string, object>` (with casting) and `Dictionary<string, string>` (with parsing). Configure key casing, missing key behavior, and null value handling.

---

## Migration Checklist

Use this checklist when migrating a project from AutoMapper to Forge:

- [ ] **Install packages**: Add `FreakyKit.Forge.Generator` and `FreakyKit.Forge.Analyzers` to your project
- [ ] **Remove AutoMapper**: Uninstall `AutoMapper` and `AutoMapper.Extensions.Microsoft.DependencyInjection`
- [ ] **Remove DI registration**: Delete `builder.Services.AddAutoMapper(...)` calls
- [ ] **Remove IMapper injection**: Replace constructor-injected `IMapper` with static forge calls
- [ ] **Convert profiles to forge classes**: Each AutoMapper `Profile` becomes a `[Forge]` static partial class
- [ ] **Convert CreateMap to method signatures**: Each `CreateMap<S, D>()` becomes a `public static partial D ToDto(S source);`
- [ ] **Convert ForMember to [ForgeMap]**: Move member customization from fluent config to destination property attributes
- [ ] **Convert Ignore to [ForgeIgnore]**: Replace `.Ignore()` calls with `[ForgeIgnore]` attributes
- [ ] **Convert value resolvers to [ForgeConverter]**: Replace `IValueResolver<>` classes with static converter methods
- [ ] **Convert BeforeMap/AfterMap**: Replace lambda hooks with `OnBefore{Method}` / `OnAfter{Method}` partial methods
- [ ] **Convert ProjectTo**: Replace `.ProjectTo<T>(config)` with `.Select(Forges.ToDtoExpression)` and add `GenerateExpression = true`
- [ ] **Enable nested forging**: Add `AllowNestedForging = true` where you have type-mismatched nested objects
- [ ] **Link cross-class methods**: Use `[ForgeUses(typeof(...))]` where AutoMapper discovered mappings across profiles
- [ ] **Build and fix diagnostics**: The compiler will tell you exactly what needs attention
- [ ] **Remove AssertConfigurationIsValid**: Replace with `StrictMapping = true` on critical mappings (compile-time, not runtime)
- [ ] **Run tests**: Your existing tests should pass — the mapping behavior is the same, just generated instead of reflected

### Quick Reference: AutoMapper → Forge

| AutoMapper | Forge |
|------------|-------|
| `CreateMap<S, D>()` | `public static partial D ToDto(S source);` in a `[Forge]` class |
| `.ForMember(d => d.X, o => o.MapFrom(s => s.Y))` | `[ForgeMap("Y")]` on destination property `X` |
| `.Ignore()` | `[ForgeIgnore]` on destination property |
| `.ReverseMap()` | Two separate forge methods |
| `mapper.Map<D>(source)` | `ForgeClass.ToDto(source)` or `source.ToDto()` |
| `mapper.Map(source, dest)` | `ForgeClass.Update(source, dest)` (void, 2 params) |
| `IValueResolver<S, D, M>` | `[ForgeConverter] static M Convert(S value)` |
| `.BeforeMap(...)` / `.AfterMap(...)` | `OnBefore{Method}` / `OnAfter{Method}` partial methods |
| `Profile` | `[Forge]` static partial class |
| Assembly scanning | `[ForgeUses(typeof(...))]` |
| `.ProjectTo<D>(config)` | `.Select(Forges.ToDtoExpression)` with `GenerateExpression = true` |
| `AssertConfigurationIsValid()` | `[ForgeMethod(StrictMapping = true)]` |
| `.NullSubstitute("default")` | `[ForgeMap("Prop", DefaultValue = "default")]` |
| `.Condition(src => ...)` | `[ForgeMap("Prop", Condition = nameof(Method))]` |
| `.Include<Dog, DogDto>()` | `[ForgePolymorphic(typeof(Dog), nameof(MapDog))]` |
