# Source Generator

The FreakyKit.Forge source generator (`FreakyKit.Forge.Generator`) is a Roslyn incremental source generator that produces partial method implementations for forge methods at compile time.

## Pipeline

1. **Discovery** — finds all `static partial class` types decorated with `[Forge]`
2. **Extraction** — for each forge class, collects valid forge methods and extracts their mapping models
3. **Validation** — emits diagnostics for errors and warnings
4. **Generation** — if no errors exist for a forge class, generates the partial method implementations

Error-severity diagnostics block generation entirely for the affected forge class. No partial output is emitted — it's all or nothing per class.

## Method Shapes

### Create Method (Standard)

The standard forge method takes one parameter (source) and returns a destination type:

```csharp
public static partial DestType MethodName(SourceType source);
```

The generator constructs a new destination object, assigns matching members, and returns it.

### Update Method

A void-returning method with two parameters maps source to an existing destination:

```csharp
public static partial void Update(SourceType source, DestType existing);
```

No construction or return statement is generated — members are assigned directly on the second parameter.

### Collection Projection Method

When both the parameter type and return type are collection types, the generator treats the method as a **collection projection** — it generates a single LINQ expression rather than member-by-member assignments.

```csharp
public static partial List<PersonDto>? ToDtos(List<Person>? source);
// Generates: return source != null ? source.Select(x => ToDto(x)).ToList() : null;
```

Requirements:
- A forge method that converts the source element type to the destination element type must exist in the same forge class
- If element types are different but no such forge method exists, `FKF200` is emitted
- If element types are the same, the collection is materialised directly without a `Select`

Null safety follows the same rules as regular collection mappings: a null-guard is generated when the source collection is a reference type.

### Expression Projection (additive)

When a method has `[ForgeMethod(GenerateExpression = true)]`, the generator emits an **additional**
static property of type `Expression<Func<TSource, TDest>>` named `{MethodName}Expression`, alongside
the regular partial method body. The expression property is suitable for use with EF Core / LINQ
providers in `IQueryable.Select(...)`:

```csharp
[ForgeMethod(GenerateExpression = true)]
public static partial PersonDto ToDto(Person source);

// Generated alongside the imperative body:
public static Expression<Func<Person, PersonDto>> ToDtoExpression { get; } =
    source => new PersonDto { Name = source.Name, Age = source.Age };
```

The expression property is built from the same member-resolution chain as the imperative method
but rewrites each translatable case into an expression-tree-compatible form (no `?.`, no `switch`
expressions, no `Expression.Invoke` for nested forge). Members whose conversion can't be expressed
translatably (custom converters, `IgnoreIfNull`, non-translatable collection materializers) are
silently omitted from the expression property and emit **FKF506** with a reason. See
[projections.md](projections.md) for the full coverage matrix and EF Core 8+ requirement.

Update methods (void return, two parameters) cannot have an expression — `GenerateExpression = true`
on an update method emits **FKF504** and blocks generation for the class.

## Member Matching

Members are matched between source and destination types by **name** (case-insensitive). Only public, non-static, instance members are considered:

- **Properties** are always included
- **Fields** are included only when `ShouldIncludeFields = true` on `[ForgeMethod]`
- **Indexers** are excluded
- **Private members** are excluded

When a destination member has a matching source member with the same type, a simple assignment is generated:

```csharp
__result.Name = source.Name;
```

#### Same-type mutable collections

When the same-type member is a **mutable collection** (`List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `T[]`, `IList<T>`, `ICollection<T>`, `IDictionary<K,V>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyDictionary<K,V>`, `ISet<T>`, `Collection<T>`, `ReadOnlyCollection<T>`), the generator emits a copy-constructor expression by default so the destination owns an independent instance:

```csharp
__result.Tags = source.Tags != null ? new List<string>(source.Tags) : null;
```

Set `[ForgeMethod(ShareReference = true)]` (or per-member `[ForgeMap(ShareReference = true)]`) to opt out and emit direct reference assignment instead. Immutable collections (`ImmutableArray<T>`, `ImmutableList<T>`, etc.) and same-type custom classes are always direct reference assignment regardless of the flag. See [attributes.md#reference-semantics-for-same-type-collections](attributes.md#reference-semantics-for-same-type-collections).

### Custom Mapping with `[ForgeMap]`

Members with `[ForgeMap]` are registered under the specified key instead of their actual name. This allows mapping between differently-named members:

```csharp
// Source-side: FirstName registered under key "Name"
public class Source { [ForgeMap("Name")] public string FirstName { get; set; } }
// Generates: __result.Name = source.FirstName;
```

### Ignored Members

Members with `[ForgeIgnore]` are completely excluded from matching on both source and destination sides. No warnings are emitted.

### Read-Only Properties

Read-only properties (no setter) on the destination type are skipped for property assignment. They can still be satisfied through constructor parameters.

### Init-Only Properties

Properties with `init` setters (including record positional parameters) are placed in an **object initializer** block rather than using standard assignment:

```csharp
var __result = new Dest()
{
    Id = source.Id,       // init-only → object initializer
    Name = source.Name    // init-only → object initializer
};
__result.Age = source.Age; // regular setter → standard assignment
```

In **update methods** (void return, 2 parameters), init-only properties are skipped since they cannot be reassigned after construction.

## Constructor Selection

The generator picks the destination type's constructor using the following priority:

### 1. Parameterless Constructor (Preferred)

If the destination type has a public parameterless constructor, it is always used:

```csharp
var __result = new Dest();
```

### 2. Parameterized Constructor

If no parameterless constructor exists, the generator looks for a public constructor where **every parameter** can be satisfied from a source member (matched by name and type, case-insensitive):

```csharp
var __result = new Dest(source.Name, source.Age);
```

Members used in constructor arguments are **not** reassigned in the property-assignment phase.

### 3. No Construction (Update Mode)

Update methods skip construction entirely. Assignments go directly to the existing object parameter.

### 4. Error Cases

- **Multiple viable constructors** — `FKF500` error
- **Single constructor with unsatisfiable parameters** — `FKF501` error per missing parameter
- **No viable constructor at all** — `FKF502` error

## Type Mismatch Resolution

When source and destination members share a name but have different types, the generator tries the following resolution chain (in order):

1. **Nullable handling** — `Nullable<T>` ↔ `T` conversions
2. **Enum mapping** — enum-to-enum via cast or name-based switch
3. **Enum ↔ string mapping** — automatic string serialization for enums
4. **Dictionary mapping** — `Dictionary<K, V1>` → `Dictionary<K, V2>` conversions
5. **Collection mapping** — collection-to-collection conversions
6. **Type converter** — `[ForgeConverter]` methods
7. **Implicit numeric conversions** — safe or lossy implicit conversions (with warnings)
8. **Nested forging** — other forge methods (requires `AllowNestedForging = true`)
9. **Error** — `FKF200` if nothing resolves the mismatch

### Nullable Handling

- `Nullable<T>` → `T`: generates `source.Prop.Value` (with `FKF201` warning)
- `T` → `Nullable<T>`: direct assignment
- Reference type nullability differences: direct assignment

### Enum Mapping

When both types are enums:

- **Cast** (default): `(DestEnum)source.Value`
- **ByName**: switch expression mapping each member by name

### Enum ↔ String Mapping

When one member is an enum and the other is a `string`, automatic conversion is applied:

- **Enum → string**: `source.Status.ToString()`
- **String → enum**: `Enum.Parse<StatusEnum>(source.Status)` (throws if invalid)
- **With fallback**: Use `[ForgeMap("Status", DefaultValue = Status.Unknown)]` on the destination property to provide a fallback value when parsing fails: `Enum.TryParse<StatusEnum>(source.Status, out var result) ? result : Status.Unknown`

```csharp
[Forge]
public static partial class OrderForges
{
    // String → enum (throws if "Cancelled" is not a valid Status value)
    public static partial Order ToOrder(OrderDto source);
    // Generates: __result.Status = Enum.Parse<Status>(source.Status);
    
    // With fallback value
    public static partial Order ToOrderSafe(OrderDto source);
    // If destination property has [ForgeMap(..., DefaultValue = Status.Unknown)],
    // generates: Enum.TryParse<Status>(source.Status, out var __parsed) ? __parsed : Status.Unknown;
}
```

Emits **FKF230** (Info) when enum-string mapping is applied.

### Dictionary Mapping

When both types are dictionaries with matching key types but different value types:

- **Same value type**: `new Dictionary<K, V>(source.Values)`
- **Different value type with forge method**: `source.Values.ToDictionary(__kvp => __kvp.Key, __kvp => ForgeMethod(__kvp.Value))` (requires `AllowNestedForging = true`)
- **Reference type source**: Wrapped with null guard: `source.Values != null ? ... : null`

Supports `Dictionary<K, V>` and `IDictionary<K, V>` on both sides. Keys must have the same type; if keys differ, no conversion is attempted.

```csharp
public class Source { public Dictionary<string, int> Scores { get; set; } }
public class Dest   { public Dictionary<string, double> Scores { get; set; } }
// Generates: __result.Scores = source.Scores != null ? source.Scores.ToDictionary(__kvp => __kvp.Key, __kvp => (double)__kvp.Value) : null
```

### Collection Mapping

Supported collection types:

- **Standard:** `List<T>`, `T[]`, `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `HashSet<T>`
- **Immutable:** `ImmutableArray<T>`, `ImmutableList<T>`, `IImmutableList<T>`, `ImmutableHashSet<T>`, `IImmutableSet<T>`
- **Read-only:** `ReadOnlyCollection<T>`, `Collection<T>`
- Any type implementing `IEnumerable<T>`

Materialization rule:
- Array (`T[]`): `.ToArray()`
- `HashSet<T>`: `.ToHashSet()`
- `ImmutableArray<T>`: `.ToImmutableArray()`
- `ImmutableList<T>` / `IImmutableList<T>`: `.ToImmutableList()`
- `ImmutableHashSet<T>` / `IImmutableSet<T>`: `.ToImmutableHashSet()`
- `ReadOnlyCollection<T>`: `.ToList().AsReadOnly()`
- Otherwise: `.ToList()`

**Null-safe access:** When the source collection is a reference type, a null guard is generated:
- Reference type destination: `source.Values != null ? source.Values.ToList() : null`
- Value type destination (e.g., `ImmutableArray<T>`): `source.Values != null ? source.Values.ToImmutableArray() : default`

For different element types with a forge method available:
- `.Select(x => ForgeMethod(x)).ToList()` or `.Select(x => ForgeMethod(x)).ToArray()` (requires `AllowNestedForging = true`)

### Type Converters

Methods marked with `[ForgeConverter]` are scanned by parameter type → return type. When a converter matches the type mismatch, it is called:

```csharp
__result.Birthday = ConvertDateTime(source.Birthday);
```

### Implicit Numeric Conversions

When types differ but an implicit conversion exists, the generator applies it directly. Some implicit conversions may lose precision or data and emit an **FKF203** warning:

**Safe (lossless) implicit conversions** — no warning:
- `byte` → `short`, `int`, `long`, `float`, `double`, `decimal`
- `short` → `int`, `long`, `float`, `double`, `decimal`
- `ushort` → `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`
- `int` → `long`, `double`, `decimal`
- `uint` → `long`, `ulong`, `double`, `decimal`

**Lossy implicit conversions** — emit **FKF203** (Warning):
- `float` → `double`: precision may be lost in some contexts
- `int` / `uint` → `float`: 24-bit mantissa limits precision for large integers
- `long` / `ulong` → `float` / `double`: significant precision loss

```csharp
public class Source { public int Count { get; set; } }
public class Dest   { public float FloatCount { get; set; } }
// Generates: __result.FloatCount = source.Count;  // FKF203: int → float may lose precision
```

Use a `[ForgeConverter]` method if you need explicit control over the conversion.

## Flattening

When `AllowFlattening = true` and a destination member has no direct match, the generator tries prefix matching:

1. For each source member `S`, check if the destination key starts with `S`'s key
2. If so, look for a **property** on `S`'s type whose name matches the remainder
3. Generate: `__result.AddressCity = source.Address?.City` (null-safe for reference type intermediates)

Only one level of nesting is supported. Flattening only traverses **properties** on intermediate types — fields are not considered, even when `ShouldIncludeFields = true`. When the intermediate type is a reference type, the null-conditional operator (`?.`) is used to prevent `NullReferenceException`.

## Nested Forging

When a source and destination member share a name but have different types, the generator checks whether another forge method in the same class can convert between them.

With `AllowNestedForging = true`:

```csharp
// Null-safe: guards against NullReferenceException when source member is null
__result.Home = source.Home != null ? ToAddressDto(source.Home) : null;
```

The nested forge method must:
- Be in the same forge class
- Be `static partial`
- Take the source member's type as its parameter
- Return the destination member's type

### Null Fallback Behavior

By default, when a source member is null, the nested forge returns null for the destination member. You can customize this with `[ForgeMap(NullFallback = ...)]` on the destination member:

```csharp
public class Dest
{
    // Default behavior: source.Home null → result.Home = null
    public AddressDto Home { get; set; }

    // Custom behavior: source.Home null → result.Home = new AddressDto()
    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
    public AddressDto OtherHome { get; set; }
}

// Generates:
// __result.Home = source.Home != null ? ToAddressDto(source.Home) : null;
// __result.OtherHome = source.Home != null ? ToAddressDto(source.Home) : new AddressDto();
```

`NullFallback` also works with collections, using `[]` (collection expression syntax) to create an empty collection:

```csharp
[ForgeMap("Addresses", NullFallback = NullFallback.DefaultConstruct)]
public List<AddressDto> Addresses { get; set; }

// Generates: __result.Addresses = source.Addresses != null ? ... : [];
```

For more details, see [`[ForgeMap]` NullFallback](attributes.md#nullfallback) in the attributes reference.

## Before/After Hooks

The generator scans the forge class for convention-based partial methods:

- **Before hook**: `static partial void OnBefore{MethodName}({SourceType} source)` — called before any assignments
- **After hook (create)**: `static partial void OnAfter{MethodName}({SourceType} source, {DestType} result)` — called after assignments, before return
- **After hook (update)**: `static partial void OnAfter{MethodName}({SourceType} source, {DestType} existing)` — called after assignments (uses the dest parameter name, not `__result`)

### Create Method Example

```csharp
// Generated:
public static partial PersonDto ToDto(Person source)
{
    OnBeforeToDto(source);
    var __result = new PersonDto();
    __result.Name = source.Name;
    OnAfterToDto(source, __result);
    return __result;
}
```

### Update Method Example

```csharp
// Generated:
public static partial void Update(Person source, PersonDto existing)
{
    OnBeforeUpdate(source);
    existing.Name = source.Name;
    OnAfterUpdate(source, existing);
}
```

## Extension Methods

When `GenerateExtensionMethods = true` (the default) on the `[Forge]` attribute, the generator emits an additional **extension method class** alongside the forge class. This enables idiomatic `this` syntax without requiring static method calls.

The extension class is named `{ForgeClassName}Extensions` and lives in the same namespace as the forge class. Each extension method is a thin wrapper that forwards to the corresponding static forge method:

```csharp
// User code
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    public static partial void Update(Person source, PersonDto existing);
}

// Generated extensions (in PersonForges.Forge.g.cs):
public static class PersonForgesExtensions
{
    public static PersonDto ToDto(this Person source) => PersonForges.ToDto(source);
    public static void Update(this Person source, PersonDto existing) => PersonForges.Update(source, existing);
}

// Usage: both forms work
var dto = person.ToDto();                                    // extension syntax
var dto2 = PersonForges.ToDto(person);                       // static syntax
```

Extension methods skip collection/dictionary projection methods — only create and update method shapes are wrapped. Set `GenerateExtensionMethods = false` on `[Forge]` to suppress extension method generation entirely.

Extension methods are **only** generated for top-level forge classes. Nested forge classes (classes declared inside other types) do not generate extensions.

## Generated File

For each forge class, the generator produces a single `.g.cs` file containing:

- An `// <auto-generated/>` header
- `#nullable enable`
- `using System;`
- `using System.Linq;`
- The partial class in the same namespace as the original
- All forge method implementations
- (If `GenerateExtensionMethods = true`) The extension method class

The file is named `{FullyQualifiedClassName}.Forge.g.cs` (with `.`, `<`, and `>` replaced by underscores).

### Nested Type Support

Forge classes can be nested inside other types. The generator will emit the correct containing type chain so the partial declaration matches the original nesting structure:

```csharp
public partial class Outer
{
    [Forge]
    public static partial class InnerForges
    {
        public static partial PersonDto ToDto(Person source);
    }
}

// Generated:
partial class Outer
{
    public static partial class InnerForges
    {
        public static partial PersonDto ToDto(Person source) { ... }
    }
}
```

Containing types must be declared `partial` in user code so the generated partial declaration can extend them.

## Error Handling

The generator follows a strict **no partial output** policy:

- If any forge method in a class produces an error-severity diagnostic, **no source is generated for the entire class**
- All method-level errors are collected before stopping, so you see every error at once
- Warnings do not block generation
