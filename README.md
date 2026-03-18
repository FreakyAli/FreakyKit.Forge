<p align="center">
  <img src="forge.png" alt="FreakyKit.Forge" width="600" />
</p>

<div>
   <a href="https://github.com/FreakyAli/FreakyKit.Forge/actions/workflows/ci.yml"><img src="https://github.com/FreakyAli/FreakyKit.Forge/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
   <a href="https://github.com/FreakyAli/FreakyKit.Forge/actions/workflows/test.yml"><img src="https://github.com/FreakyAli/FreakyKit.Forge/actions/workflows/test.yml/badge.svg" alt="Test"></a>
   <a href="https://www.nuget.org/packages/FreakyKit.Forge"><img src="https://img.shields.io/nuget/v/FreakyKit.Forge?color=blue&logo=nuget" alt="NuGet"></a>
   <a href="https://www.nuget.org/packages/FreakyKit.Forge"><img src="https://img.shields.io/nuget/dt/FreakyKit.Forge.svg" alt="Downloads"></a>
   <a href="./LICENSE"><img src="https://img.shields.io/github/license/FreakyAli/FreakyKit.Forge" alt="License"></a>
</div>

# FreakyKit.Forge

A compile-time object mapping library for C# powered by Roslyn source generators. Define your mappings as partial method declarations and Forge generates the implementations at build time — zero reflection, zero runtime overhead.

## Quick Start

```csharp
using FreakyKit.Forge;

public class Person    { public string Name { get; set; } public int Age { get; set; } }
public class PersonDto { public string Name { get; set; } public int Age { get; set; } }

[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}
```

At compile time, Forge generates the implementation:

```csharp
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source)
    {
        var __result = new PersonDto();
        __result.Name = source.Name;
        __result.Age = source.Age;
        return __result;
    }
}
```

Then just call it:

```csharp
var dto = PersonForges.ToDto(person);
```

### Support the Project

If you find this project helpful, consider supporting its development:

[![](https://miro.medium.com/max/600/0*wrBJU05A3BULKcWA.gif)](https://www.buymeacoffee.com/FreakyAli)

## Installation

```xml
<ItemGroup>
    <PackageReference Include="FreakyKit.Forge.Generator" Version="1.0.0" />
    <PackageReference Include="FreakyKit.Forge.Analyzers" Version="1.0.0" />
</ItemGroup>
```

For other installation options (lightweight, conventions, local development), see the [full installation guide](docs/installation.md).

## Features

- **Zero reflection** — all mapping code is generated at compile time
- **Zero runtime dependencies** — the generated code is plain C#
- **Parameterized constructor support** — automatically selects the best constructor
- **Init-only & record support** — init-only properties and records use object initializer syntax
- **Nested forging** — compose mappings for complex object graphs with null-safe access
- **Collection mapping** — automatic `List<T>`, `T[]`, `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`, `ReadOnlyCollection<T>`, `HashSet<T>` conversion with LINQ
- **Null-safe nested access** — null guards on nested forge calls, flattened properties, and collection mappings
- **Flattening** — map nested properties like `Address.City` to flat members like `AddressCity`
- **Custom member mapping** — rename members with `[ForgeMap]` on properties, fields, or constructor parameters
- **Ignore members** — exclude members with `[ForgeIgnore]`; use `Side` to restrict exclusion to source or destination only
- **Type converters** — bridge incompatible types with `[ForgeConverter]`; invalid converter signatures are caught by FKF221
- **Nullable handling** — automatic `Nullable<T>` ↔ `T` conversion with optional default values
- **Enum mapping** — cast or name-based enum-to-enum conversion
- **Update mapping** — modify existing objects in place (void return, 2 parameters)
- **Before/after hooks** — run custom logic before or after mapping via partial methods
- **Implicit and explicit modes** — control which methods get generated
- **Strict mapping (drift detection)** — opt-in error-level diagnostics when source/destination types drift apart
- **Rich diagnostics** — 34 diagnostics across 7 categories guide you at build time
- **Top-level collection projection** — declare a `List<Dest> ToList(List<Source> source)` method and the generator produces the LINQ projection automatically
- **Field support** — opt-in to include fields in member discovery
- **Private method support** — opt-in to include private forge methods
- **Conditional mapping** — skip assignments when source is null with `IgnoreIfNull`
- **Debugging friendly** — generated code includes `[GeneratedCode]`, `[DebuggerStepThrough]`, `#line` directives, `#pragma warning disable`, and XML doc comments

## Comparison

> **Note:** This comparison is based on publicly available documentation at the time of writing. If you spot an inaccuracy, please [open an issue](https://github.com/FreakyAli/FreakyKit.Forge/issues) and we'll correct it.

| Feature | Forge | AutoMapper | Mapperly | Mapster | Facet |
|---------|:-----:|:----------:|:--------:|:-------:|:-----:|
| Source generator (compile-time) | ✅ | ❌ | ✅ | ✅ | ✅ |
| Zero runtime dependencies | ✅ | ❌ | ✅ | ❌ | ✅ |
| Constructor mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Nested object mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Collection mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Flattening | ✅ | ✅ | ✅ | ✅ | ✅ |
| Custom member renaming | ✅ | ✅ | ✅ | ✅ | ✅ |
| Ignore members | ✅ | ✅ | ✅ | ✅ | ✅ |
| Type converters | ✅ | ✅ | ✅ | ✅ | ~ |
| Nullable handling | ✅ | ✅ | ✅ | ✅ | ✅ |
| Enum mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Update existing objects | ✅ | ✅ | ✅ | ✅ | ✅ |
| Before/after hooks | ✅ | ✅ | ✅ | ✅ | ✅ |
| Rich diagnostics | ✅ | ❌ | ✅ | ~ | ✅ |
| Field support | ✅ | ✅ | ✅ | ✅ | ❌ |
| Init-only / record support | ✅ | ✅ | ✅ | ✅ | ✅ |
| Null-safe nested access | ✅ | ✅ | ✅ | ~ | ~ |
| Immutable collection types | ✅ | ✅ | ✅ | ✅ | ~ |
| Strict mapping (drift detection) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Conditional mapping (ignore if null) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Debugging friendly output | ✅ | N/A | ✅ | ~ | ✅ |
| Implicit and explicit mapping modes | ✅ | ❌ | ❌ | ❌ | ❌ |
| Custom constructor parameter mapping | ✅ | ✅ | ✅ | ~ | ❌ |
| Dedicated collection projection methods | ✅ | ✅ | ✅ | ✅ | ~ |
| Side-specific member exclusion | ✅ | ~ | ❌ | ❌ | ❌ |
| Type converter validation | ✅ | N/A | ✅ | N/A | N/A |

## Performance Benchmarks

> Benchmarked on .NET 8 using BenchmarkDotNet v0.15.8. Numbers shown are for Forge alongside hand-written code. The same benchmarks were also run against AutoMapper 16.1.1, Mapperly 4.3.1, Mapster 7.4.0, and Facet 5.8.2 — full per-library breakdown in [docs/benchmarks.md](docs/benchmarks.md).

| Scenario | Forge | Hand-written | Ratio |
|----------|------:|-------------:|------:|
| Simple mapping (4 props) | 6.46 ns | 6.37 ns | 1.01x |
| Medium mapping (10 props) | 12.43 ns | 14.49 ns | **0.86x** |
| Nested object | 21.92 ns | 23.57 ns | **0.93x** |
| Property flattening | 10.97 ns | 11.72 ns | **0.94x** |
| Deep object graph | 208.5 ns | 204.5 ns | 1.02x |
| Collection (1,000 items) | 5,270 ns | 5,261 ns | 1.00x |
| Throughput (10,000 objects) | 152.0 μs | 155.2 μs | **0.98x** |
| Real-world e-commerce order | 161.5 ns | 161.9 ns | **1.00x** |
| Nullable DB entity (populated) | 11.45 ns | 11.71 ns | **0.98x** |

Forge consistently matches or beats hand-written code with zero allocation overhead. See [docs/benchmarks.md](docs/benchmarks.md) for full details.

## The Forge Ecosystem

| Package | Downloads | Description |
|---------|:---------:|-------------|
| [**FreakyKit.Forge**](https://www.nuget.org/packages/FreakyKit.Forge) | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.svg) | Core attributes and enums (`[Forge]`, `[ForgeMethod]`, `[ForgeMap]`, etc.) |
| [**FreakyKit.Forge.Generator**](https://www.nuget.org/packages/FreakyKit.Forge.Generator) | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Generator.svg) | Roslyn source generator — writes mapping method bodies at compile time |
| [**FreakyKit.Forge.Analyzers**](https://www.nuget.org/packages/FreakyKit.Forge.Analyzers) | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Analyzers.svg) | Roslyn analyzer — 34 diagnostics to validate your declarations at build time |
| [**FreakyKit.Forge.Diagnostics**](https://www.nuget.org/packages/FreakyKit.Forge.Diagnostics) | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Diagnostics.svg) | Shared diagnostic descriptors for custom Roslyn tooling |
| [**FreakyKit.Forge.Conventions**](https://www.nuget.org/packages/FreakyKit.Forge.Conventions) | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Conventions.svg) | Optional naming convention helpers |

## How It Works

1. Mark a `static partial class` with `[Forge]`
2. Declare `static partial` methods that take a source type and return a destination type
3. The source generator matches members by name (case-insensitive) and generates the mapping body
4. The analyzer validates your declarations and reports warnings/errors at build time

## Forge Method Shape

A valid forge method is:
- `static`
- `partial` (declaration only, no body)
- Returns a non-void type (the destination)
- Takes exactly one parameter (the source)
- Has no type parameters

```csharp
public static partial DestType MethodName(SourceType source);
```

### Update Method Shape

A void-returning method with two parameters is an **update** method:

```csharp
public static partial void Update(SourceType source, DestType existing);
// Generates: existing.Name = source.Name; (no construction, no return)
```

## Constructor Selection

Forge picks the destination constructor using these rules (in order):

1. **Parameterless constructor** — preferred if available
2. **Parameterized constructor** — selected if exactly one public constructor can be fully satisfied from source members (matched by name and type, case-insensitive)
3. **Ambiguity error** (`FKF500`) — if multiple constructors are equally viable
4. **Missing parameter error** (`FKF501`) — if a single constructor has unsatisfiable parameters
5. **No viable constructor error** (`FKF502`) — if no public constructor can be used

```csharp
public class Dest
{
    public string Name { get; }
    public int Age { get; }
    public Dest(string name, int age) { Name = name; Age = age; }
}

// Generates: var __result = new Dest(source.Name, source.Age);
```

## Nested Forging

When source and destination have members with the same name but different types, you can compose mappings:

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
}

// Generates: __result.Home = ToAddressDto(source.Home);
```

Without `AllowNestedForging = true`, a type mismatch where a forge method exists emits `FKF300` (warning). Without any forge method for the conversion, it emits `FKF200` (error) and blocks generation.

## Collection Mapping

Collections are automatically mapped when source and destination members are collection types. Supported types include:

- **Standard:** `List<T>`, `T[]`, `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `HashSet<T>`
- **Immutable:** `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`
- **Read-only:** `ReadOnlyCollection<T>`
- Any type implementing `IEnumerable<T>`

```csharp
public class Source { public List<int> Values { get; set; } = new(); }
public class Dest   { public int[] Values { get; set; } = Array.Empty<int>(); }

// Generates: __result.Values = source.Values != null ? source.Values.ToArray() : null;
```

Immutable collection example:

```csharp
public class Source { public List<int> Values { get; set; } = new(); }
public class Dest   { public ImmutableArray<int> Values { get; set; } }

// Generates: __result.Values = source.Values != null ? source.Values.ToImmutableArray() : default;
```

When element types differ and a forge method exists, use `AllowNestedForging = true`:

```csharp
[ForgeMethod(AllowNestedForging = true)]
public static partial PersonDto ToDto(Person source);
public static partial AddressDto ToAddressDto(Address source);

// Generates: __result.Addresses = source.Addresses != null ? source.Addresses.Select(x => ToAddressDto(x)).ToList() : null;
```

## Flattening

Opt-in to flatten nested source properties into flat destination members:

```csharp
public class Source { public Address Address { get; set; } }
public class Address { public string City { get; set; } }
public class Dest { public string AddressCity { get; set; } }

[ForgeMethod(AllowFlattening = true)]
public static partial Dest ToDest(Source source);

// Generates: __result.AddressCity = source.Address.City;
```

One level of nesting is supported. The destination member name is matched by concatenating the source member name with its nested property name (case-insensitive).

## Custom Member Mapping

Use `[ForgeMap]` to map members with different names. Can be placed on properties, fields, or constructor parameters:

```csharp
// Source-side: "FirstName" maps to destination member "Name"
public class Source { [ForgeMap("Name")] public string FirstName { get; set; } }
public class Dest   { public string Name { get; set; } }

// Destination-side: "Name" reads from source member "FirstName"
public class Source { public string FirstName { get; set; } }
public class Dest   { [ForgeMap("FirstName")] public string Name { get; set; } }

// Constructor parameter: redirect matching when the parameter name differs from the source member
public class Dest
{
    public string Name { get; }
    public Dest([ForgeMap("FullName")] string name) { Name = name; }
}
// Generates: var __result = new Dest(source.FullName);
```

## Ignore Members

Use `[ForgeIgnore]` to exclude a member from mapping. By default both sides are excluded. Use `Side` to restrict to one side:

```csharp
public class Source
{
    public string Name { get; set; }
    [ForgeIgnore] public string InternalId { get; set; }  // skipped on both sides, no warnings

    [ForgeIgnore(Side = ForgeIgnoreSide.Source)]
    public string AuditField { get; set; }  // not mapped from source (suppresses FKF101)
                                             // but dest can still map to it via [ForgeMap]
}

public class Dest
{
    public string Name { get; set; }
    [ForgeIgnore(Side = ForgeIgnoreSide.Destination)]
    public int ComputedScore { get; set; }  // not populated by forge (suppresses FKF100)
}
```

## Type Converters

Use `[ForgeConverter]` on a static method to bridge incompatible types. The method must be non-void, non-generic, and take exactly one parameter — the analyzer emits FKF221 if the signature is invalid:

```csharp
[Forge]
public static partial class MyForges
{
    public static partial Dest ToDest(Source source);

    [ForgeConverter]
    public static string ConvertDateTime(DateTime value) => value.ToString("yyyy-MM-dd");
    // Generates: __result.Birthday = ConvertDateTime(source.Birthday);

    // Bad signature — FKF221 warning, converter will be ignored:
    // [ForgeConverter] public static string Convert(DateTime v, string fmt) => v.ToString(fmt);
}
```

## Nullable Handling

Forge automatically handles nullable type differences:

- `Nullable<T>` → `T`: generates `source.Prop.Value` (with `FKF201` warning)
- `T` → `Nullable<T>`: direct assignment
- Reference type nullability differences: direct assignment

### Default Values for Nullable Mappings

Use `DefaultValue` on `[ForgeMap]` to provide a fallback instead of `.Value`:

```csharp
public class Source { [ForgeMap("Age", DefaultValue = 0)] public int? Age { get; set; } }
public class Dest   { public int Age { get; set; } }

// Generates: __result.Age = source.Age ?? 0;
// No FKF201 warning — the fallback prevents InvalidOperationException
```

`DefaultValue` can be placed on either the source or destination member.

## Conditional Mapping (Ignore If Null)

Skip assignments when the source value is null. Useful for update methods where you want to preserve existing values.

**Method-level** — applies to all assignments:

```csharp
[Forge]
public static partial class MyForges
{
    [ForgeMethod(IgnoreIfNull = true)]
    public static partial void Update(Source source, Dest existing);
}

// Generates:
// if (source.Name != null) existing.Name = source.Name;
// if (source.Age != null) existing.Age = source.Age;
```

**Per-member** — applies to a specific member via `[ForgeMap]`:

```csharp
public class Source
{
    [ForgeMap("Name", IgnoreIfNull = true)]
    public string? Name { get; set; }
    public string? Email { get; set; }
}

// Generates:
// if (source.Name != null) __result.Name = source.Name;
// __result.Email = source.Email;  (no null check)
```

`IgnoreIfNull` can be placed on `[ForgeMap]` on either the source or destination member, or on `[ForgeMethod]` for method-wide behavior.

## Init-Only & Record Support

Properties with `init` setters and record types are automatically handled using C# object initializer syntax:

```csharp
public class Source { public int Id { get; set; } public string Name { get; set; } = ""; }
public record Dest(int Id, string Name);

// Generates:
// var __result = new Dest(default, default)
// {
//     Id = source.Id,
//     Name = source.Name
// };
```

Init-only properties are placed in the object initializer block, while regular settable properties use standard assignment. In **update methods**, init-only properties are skipped since they cannot be reassigned after construction.

## Null-Safe Nested Access

Forge automatically generates null guards when accessing nested members through reference types:

**Nested forge calls:**

```csharp
// Generates: __result.Address = source.Address != null ? ToAddressDto(source.Address) : null;
```

**Flattened properties:**

```csharp
// Generates: __result.AddressCity = source.Address?.City;
```

**Collection members:**

```csharp
// Generates: __result.Values = source.Values != null ? source.Values.ToArray() : null;
```

This prevents `NullReferenceException` at runtime when source members are null.

## Strict Mapping (Drift Detection)

Enable strict mapping to catch type drift at compile time. When source or destination types change (members added, removed, or renamed), strict mode escalates warnings to errors:

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
- **FKF111** (Error): Every source member must have a matching destination member or be excluded via `[ForgeIgnore]`

Without strict mapping, these are reported as FKF100/FKF101 warnings. Strict mode is useful for critical mappings where silent drift could cause data loss.

## Enum Mapping

Forge automatically handles enum-to-enum conversions:

```csharp
// Default: cast mapping
[ForgeMethod(MappingStrategy = ForgeMapping.Cast)]
public static partial Dest ToDest(Source source);
// Generates: __result.Status = (DestStatus)source.Status;

// Name-based mapping (safer when underlying values differ)
[ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
public static partial Dest ToDest(Source source);
// Generates: __result.Status = source.Status switch { ... };
```

## Before/After Hooks

Add custom logic before or after mapping using convention-based partial methods:

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);

    // Called before mapping assignments
    static partial void OnBeforeToDto(Person source);

    // Called after mapping assignments, before return
    static partial void OnAfterToDto(Person source, PersonDto result);
}
```

### Hook Signatures for Update Methods

For update methods (void return, 2 parameters), the hook signatures use the destination parameter directly:

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial void Update(Person source, PersonDto existing);

    // Before hook: same as create — takes only the source
    static partial void OnBeforeUpdate(Person source);

    // After hook: takes source + dest parameter (not __result)
    static partial void OnAfterUpdate(Person source, PersonDto existing);
}
```

## Implicit vs Explicit Mode

**Implicit mode** (default) — all properly-shaped partial methods in the class are treated as forge methods:

```csharp
[Forge] // Mode = ForgeMode.Implicit is the default
public static partial class MyForges
{
    public static partial Dest ToDest(Source source);     // forged
    public static partial Other ToOther(Source source);   // also forged
}
```

**Explicit mode** — only methods decorated with `[ForgeMethod]` are treated as forge methods:

```csharp
[Forge(Mode = ForgeMode.Explicit)]
public static partial class MyForges
{
    [ForgeMethod]
    public static partial Dest ToDest(Source source);     // forged

    public static partial Other ToOther(Source source);   // ignored (FKF002 warning)
}
```

## Attribute Reference

### `[Forge]`

Applied to a `static partial class`. Marks it as a forge class.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Mode` | `ForgeMode` | `Implicit` | Controls which methods are treated as forge methods |
| `ShouldIncludePrivate` | `bool` | `false` | When true, private forge methods are included |

### `[ForgeMethod]`

Applied to a `static partial` method. Required in explicit mode, optional in implicit mode.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ShouldIncludeFields` | `bool` | `false` | Include fields in member discovery |
| `AllowNestedForging` | `bool` | `false` | Allow calling other forge methods for nested type conversions |
| `MappingStrategy` | `ForgeMapping` | `Cast` | How enum-to-enum mappings are generated |
| `AllowFlattening` | `bool` | `false` | Flatten nested source properties into flat destination members |
| `IgnoreIfNull` | `bool` | `false` | Wrap all assignments in null checks — skip when source is null |
| `StrictMapping` | `bool` | `false` | Escalate unmapped/unused member warnings to errors (drift detection) |

### `[ForgeIgnore]`

Applied to a property or field. Excludes the member from mapping — no FKF100/FKF101 warnings.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Side` | `ForgeIgnoreSide` | `Both` | Which side to exclude: `Both`, `Source` (suppresses FKF101 only), or `Destination` (suppresses FKF100 only) |

### `[ForgeMap("name")]`

Applied to a property, field, or constructor parameter. Maps the member to a differently-named counterpart.

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | `string` | The name of the counterpart member (or a shared key when used on both sides) |
| `DefaultValue` | `object?` | Fallback value for `Nullable<T>` → `T` mappings. Generates `??` instead of `.Value` |
| `IgnoreIfNull` | `bool` | When true, wraps the assignment in `if (source.X != null)` — skips when source is null |

### `[ForgeConverter]`

Applied to a `static` method. Marks it as a type converter. The method must be non-void, non-generic, and take exactly one parameter. Invalid signatures emit FKF221.

### `ForgeMode`

| Value | Description |
|-------|-------------|
| `Implicit` | All properly-shaped partial methods are forge methods |
| `Explicit` | Only `[ForgeMethod]`-decorated methods are forge methods |

### `ForgeIgnoreSide`

| Value | Description |
|-------|-------------|
| `Both` | Member excluded on both source and destination sides (default) |
| `Source` | Excluded only on source side — suppresses FKF101 |
| `Destination` | Excluded only on destination side — suppresses FKF100 |

### `ForgeMapping`

| Value | Description |
|-------|-------------|
| `Cast` | Direct cast: `(DestEnum)source.Value` |
| `ByName` | Switch expression mapping by member name |

## Diagnostics

See [docs/diagnostics.md](docs/diagnostics.md) for the full diagnostics reference.

| ID | Severity | Summary |
|----|----------|---------|
| FKF001 | Info | Explicit mode activated |
| FKF002 | Warning | Method ignored in explicit mode |
| FKF010 | Warning | Private forge method ignored |
| FKF011 | Info | Private visibility enabled |
| FKF020 | Error | Forge method declares a body |
| FKF030 | Error | Forge method name overloaded |
| FKF040 | Info | Update mode activated |
| FKF041 | Error | Update destination has no settable members |
| FKF050 | Info | Before hook detected |
| FKF051 | Info | After hook detected |
| FKF100 | Warning | Destination member has no source match |
| FKF101 | Warning | Source member unused |
| FKF102 | Info | Member ignored via [ForgeIgnore] |
| FKF103 | Info | Custom member mapping via [ForgeMap] |
| FKF104 | Error | ForgeMap target not found |
| FKF105 | Warning | Duplicate ForgeMap target |
| FKF106 | Info | Flattened mapping applied |
| FKF110 | Error | Strict: destination member missing source |
| FKF111 | Error | Strict: source member unused |
| FKF200 | Error | Incompatible member types |
| FKF201 | Warning | Nullable value type to non-nullable mapping |
| FKF202 | Info | Nullable mapping applied |
| FKF210 | Info | Enum cast mapping |
| FKF211 | Info | Enum name-based mapping |
| FKF212 | Warning | Enum member missing in destination |
| FKF220 | Info | Type converter used |
| FKF300 | Warning | Nested forging disabled |
| FKF310 | Info | Collection mapping applied |
| FKF400 | Warning | Field ignored |
| FKF401 | Info | Fields enabled |
| FKF500 | Error | Constructor ambiguity |
| FKF501 | Error | Missing constructor parameter |
| FKF502 | Error | No viable constructor |

## Project Structure

```text
src/
  FreakyKit.Forge/              # Core attributes and enums (NuGet: FreakyKit.Forge)
  FreakyKit.Forge.Generator/    # Roslyn source generator (NuGet: FreakyKit.Forge.Generator)
  FreakyKit.Forge.Analyzers/    # Roslyn analyzer (NuGet: FreakyKit.Forge.Analyzers)
  FreakyKit.Forge.Diagnostics/  # Shared diagnostic descriptors (NuGet: FreakyKit.Forge.Diagnostics)
  FreakyKit.Forge.Conventions/  # Optional naming conventions (NuGet: FreakyKit.Forge.Conventions)
tests/
  FreakyKit.Forge.Analyzers.Tests/
  FreakyKit.Forge.Generator.Tests/
  FreakyKit.Forge.Integration.Tests/
```

## License

Apache-2.0 — see [LICENSE](LICENSE) for details.

## Activity

Sparkline:

[![Sparkline](https://stars.medv.io/FreakyAli/FreakyKit.Forge.svg)](https://stars.medv.io/FreakyAli/FreakyKit.Forge)

RepoBeats:

![Alt](https://repobeats.axiom.co/api/embed/4e1dad54a0d67502121ef9d06efa2b3fba64c7a3.svg "Repobeats analytics image")
