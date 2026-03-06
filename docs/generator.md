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
3. **Collection mapping** — collection-to-collection conversions
4. **Type converter** — `[ForgeConverter]` methods
5. **Nested forging** — other forge methods (requires `AllowNestedForging = true`)
6. **Error** — `FKF200` if nothing resolves the mismatch

### Nullable Handling

- `Nullable<T>` → `T`: generates `source.Prop.Value` (with `FKF201` warning)
- `T` → `Nullable<T>`: direct assignment
- Reference type nullability differences: direct assignment

### Enum Mapping

When both types are enums:

- **Cast** (default): `(DestEnum)source.Value`
- **ByName**: switch expression mapping each member by name

### Collection Mapping

Supported collection types include `List<T>`, `T[]`, `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, and any type implementing `IEnumerable<T>`.

Materialization rule:
- If the **destination** is an array (`T[]`): `.ToArray()`
- Otherwise: `.ToList()`

For different element types with a forge method available:
- `.Select(x => ForgeMethod(x)).ToList()` or `.Select(x => ForgeMethod(x)).ToArray()` (requires `AllowNestedForging = true`)

### Type Converters

Methods marked with `[ForgeConverter]` are scanned by parameter type → return type. When a converter matches the type mismatch, it is called:

```csharp
__result.Birthday = ConvertDateTime(source.Birthday);
```

## Flattening

When `AllowFlattening = true` and a destination member has no direct match, the generator tries prefix matching:

1. For each source member `S`, check if the destination key starts with `S`'s key
2. If so, look for a **property** on `S`'s type whose name matches the remainder
3. Generate: `__result.AddressCity = source.Address.City`

Only one level of nesting is supported. Flattening only traverses **properties** on intermediate types — fields are not considered, even when `ShouldIncludeFields = true`.

## Nested Forging

When a source and destination member share a name but have different types, the generator checks whether another forge method in the same class can convert between them.

With `AllowNestedForging = true`:

```csharp
__result.Home = ToAddressDto(source.Home);
```

The nested forge method must:
- Be in the same forge class
- Be `static partial`
- Take the source member's type as its parameter
- Return the destination member's type

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

## Generated File

For each forge class, the generator produces a single `.g.cs` file containing:

- An `// <auto-generated/>` header
- `#nullable enable`
- `using System;`
- `using System.Linq;`
- The partial class in the same namespace as the original
- All forge method implementations

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
