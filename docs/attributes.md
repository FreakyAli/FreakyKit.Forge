# Attributes Reference

## Quick Start

Forge uses 6 attributes across 3 layers:

| Layer | Attribute | Purpose |
|-------|-----------|---------|
| **Class** | `[Forge]` | "This class contains mapping methods" |
| **Class** | `[ForgeUses]` | "Borrow methods from other forge classes" |
| **Method** | `[ForgeMethod]` | "Generate code for this mapping" |
| **Method** | `[ForgeConverter]` | "Use this to convert custom types" |
| **Property** | `[ForgeMap]` | "Customize how this property maps" |
| **Property** | `[ForgeIgnore]` | "Skip this property" |

**Minimal example:**
```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod]
    public static partial PersonDto ToDto(Person source);
}
```

**Common gotchas:**
- `[ForgeMethod]` must be in a `[Forge]` class (emits FKF525 error if not)
- `[ForgeMap]` / `[ForgeIgnore]` go on destination properties, not source
- `[ForgeUses]` also requires `[Forge]` on the class (emits FKF524 error if not)

---

## `[Forge]`

**Namespace:** `FreakyKit.Forge`
**Target:** Class (`static partial class` only)

Marks a static partial class as a forge class. The source generator discovers all valid forge methods within this class and generates their implementations.

### Properties

#### `Mode` (`ForgeMode`, default: `ForgeMode.Implicit`)

Controls which methods in the class are treated as forge methods.

- **`ForgeMode.Implicit`** — all properly-shaped static partial methods are automatically treated as forge methods. No additional attributes needed on methods.
- **`ForgeMode.Explicit`** — only methods explicitly decorated with `[ForgeMethod]` are treated as forge methods. Unmarked candidate methods emit `FKF002`.

```csharp
// Implicit (default) — both methods are forged
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    public static partial PersonSummary ToSummary(Person source);
}

// Explicit — only ToDto is forged, ToSummary emits FKF002
[Forge(Mode = ForgeMode.Explicit)]
public static partial class PersonForges
{
    [ForgeMethod]
    public static partial PersonDto ToDto(Person source);
    public static partial PersonSummary ToSummary(Person source);
}
```

#### `ShouldIncludePrivate` (`bool`, default: `false`)

When true, private forge methods are included in generation. When false, private forge methods emit `FKF010` and are ignored.

```csharp
[Forge(ShouldIncludePrivate = true)]
public static partial class PersonForges
{
    private static partial PersonDto ToDto(Person source);  // included
}
```

#### `GenerateExtensionMethods` (`bool`, default: `true`)

When true, the generator creates an extension method class alongside the forge class, allowing idiomatic chaining syntax like `person.ToDto()` instead of `PersonForges.ToDto(person)`. When false, only static forge methods are generated. Only applies to top-level forge classes — nested forge classes never generate extensions.

The extension methods are thin wrappers that forward to the corresponding static forge methods:

```csharp
// User code — both styles work when GenerateExtensionMethods = true (default)
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}

// Generated extension class (PersonForgesExtensions):
public static class PersonForgesExtensions
{
    public static PersonDto ToDto(this Person source) => PersonForges.ToDto(source);
}

// Usage: idiomatic chaining syntax
var dto = person.ToDto();

// Both are equivalent; static form still works
var dto2 = PersonForges.ToDto(person);
```

Set to `false` to suppress extension method generation:

```csharp
[Forge(GenerateExtensionMethods = false)]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    // Only PersonForges.ToDto(person) works; person.ToDto() is not available
}
```

---

## `[ForgeUses]`

**Namespace:** `FreakyKit.Forge`  
**Target:** Class (on `[Forge]` classes)

Declares that a forge class uses (includes) methods from other forge classes during nested forge method discovery. When a nested mapping requires a method that doesn't exist in the current class, the generator searches included classes in order.

### Properties

#### `ForgeClasses` (`params Type[]`)

The forge classes to include for method discovery. Order matters: the first class that has a matching method wins. All included classes must be decorated with `[Forge]`.

```csharp
[Forge]
public static partial class AddressForges
{
    [ForgeMethod]
    public static partial AddressDto ToAddressDto(Address source);
}

[Forge]
public static partial class CompanyForges
{
    [ForgeMethod]
    public static partial CompanyDto ToCompanyDto(Company source);
}

[Forge]
[ForgeUses(typeof(AddressForges), typeof(CompanyForges))]
public static partial class PersonForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToPersonDto(Person source);
    // Discovers ToAddressDto from AddressForges
    // Discovers ToCompanyDto from CompanyForges
}
```

**Order matters:**

The first class in the list has priority. If multiple included classes have methods for the same type pair, the first match is used and others are shadowed. A warning (FKF523) is emitted for each shadowed method so you're aware of the behavior.

```csharp
[Forge]
[ForgeUses(typeof(AddressForges1), typeof(AddressForges2))]
public static partial class PersonForges
{
    // If both classes have Address → AddressDto methods,
    // AddressForges1's method is used
    // FKF523 warning emitted for the shadowed method in AddressForges2
}
```

**Validation:**

- The class must be decorated with `[Forge]` — using `[ForgeUses]` without `[Forge]` emits **FKF524** (Error)
- All included classes must be decorated with `[Forge]` (FKF521)
- Self-includes are detected (FKF522)
- Shadowed methods emit warnings (FKF523) to inform you of the precedence
- Invalid includes emit error diagnostics

---

## `[ForgeMethod]`

**Namespace:** `FreakyKit.Forge`
**Target:** Method (`static partial` method only)

Marks a method as a forge method and configures its mapping behavior. In `ForgeMode.Explicit`, this attribute is required. In `ForgeMode.Implicit`, it is optional and provides per-method configuration.

### Context Requirements

`[ForgeMethod]` can only be used on methods in classes decorated with `[Forge]`. If a `[ForgeMethod]` attribute is found on a method in a class without `[Forge]`, the generator emits **FKF525** (Error) and does not process the method.

```csharp
// ✗ INVALID: No [Forge] on class
public static class MyNonForgeClass
{
    [ForgeMethod]
    public static partial PersonDto ToDto(Person source);  // FKF525 Error
}

// ✓ VALID: Class has [Forge]
[Forge]
public static partial class MyForges
{
    [ForgeMethod]
    public static partial PersonDto ToDto(Person source);
}
```

### Properties

#### `ShouldIncludeFields` (`bool`, default: `false`)

When true, public fields on the source and destination types are included in member discovery alongside properties. When false, fields are excluded and emit `FKF400`.

```csharp
public class Source
{
    public string Name;     // field
    public int Age { get; set; }  // property
}

[Forge]
public static partial class MyForges
{
    // Without ShouldIncludeFields: only Age is mapped, Name emits FKF400
    public static partial Dest ToDest(Source source);

    // With ShouldIncludeFields: both Name and Age are mapped
    [ForgeMethod(ShouldIncludeFields = true)]
    public static partial Dest ToDestWithFields(Source source);
}
```

#### `AllowNestedForging` (`bool`, default: `false`)

When true, the generator calls an existing forge method to convert nested types whose names match but whose types differ. When false, a type mismatch where a forge method exists emits `FKF300`.

Also enables collection mapping with different element types via `.Select()`.

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
    // Generates: __result.Home = ToAddressDto(source.Home);
    // Generates: __result.Addresses = source.Addresses.Select(x => ToAddressDto(x)).ToList();
}
```

#### `MappingStrategy` (`ForgeMapping`, default: `ForgeMapping.Cast`)

Controls how enum-to-enum mappings are generated when source and destination members share the same name but have different enum types.

- **`ForgeMapping.Cast`** — generates a direct cast: `(DestEnum)source.Value`
- **`ForgeMapping.ByName`** — generates a switch expression that maps each member by name

```csharp
[ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
public static partial PersonDto ToDto(Person source);
// Generates a switch expression for enum members
```

#### `AllowFlattening` (`bool`, default: `false`)

When true, the generator attempts to flatten nested source properties into flat destination members at arbitrary depth. When a destination member has no direct match, the generator tries prefix matching: `AddressCity` → `source.Address?.City`, `AddressCoordsLatitude` → `source.Address?.Coords?.Latitude`, and deeper nesting of any depth up to 10 levels.

Flattening supports **arbitrary nesting depth** (limited to 10 levels for performance): traverse as many nested levels as needed. The generator uses **properties only** on intermediate types — fields are not considered for traversal, even when `ShouldIncludeFields = true`.

Null-safety is automatic: the generator inserts `?.` null-conditional operators after each reference-type intermediate, and uses `.` after value-type intermediates (e.g., structs). Value types do not use null-conditional chaining because they cannot be null.

**Examples of flattening at various depths:**

Single-level flattening:
```csharp
public class Address { public string City { get; set; } = ""; }
public class Source  { public Address Address { get; set; } = new(); }
public class Dest    { public string AddressCity { get; set; } = ""; }

[ForgeMethod(AllowFlattening = true)]
public static partial Dest ToDto(Source source);
// Generates: dest.AddressCity = source.Address?.City
```

Two-level flattening:
```csharp
public class Coordinates { public double Latitude { get; set; } }
public class Address     { public Coordinates Coords { get; set; } = new(); }
public class Source      { public Address Address { get; set; } = new(); }
public class Dest        { public double AddressCoordsLatitude { get; set; } }

[ForgeMethod(AllowFlattening = true)]
public static partial Dest ToDto(Source source);
// Generates: dest.AddressCoordsLatitude = source.Address?.Coords?.Latitude
```

Three-level flattening and beyond:
```csharp
public class GeoPoint   { public string Code { get; set; } = ""; }
public class Coordinates { public GeoPoint Point { get; set; } = new(); }
public class Address     { public Coordinates Coords { get; set; } = new(); }
public class Source      { public Address Address { get; set; } = new(); }
public class Dest        { public string AddressCoordsPointCode { get; set; } = ""; }

[ForgeMethod(AllowFlattening = true)]
public static partial Dest ToDto(Source source);
// Generates: dest.AddressCoordsPointCode = source.Address?.Coords?.Point?.Code
// FKF531 (Info) diagnostic emitted: "Deep flattening detected (3+ levels)"
```

**Diagnostic Messages:**

- **FKF531 (Info)** — Deep flattening detected on a destination member (3+ levels of nesting). This is informational only — no action required, but you may want to consider if a simpler structure would be clearer.
- **FKF532 (Error)** — Flattening nesting depth limit exceeded (>10 levels). Flattening is limited to 10 levels. Restructure the source type hierarchy or use nested forging instead.
- **FKF530 (Error)** — Ambiguous flattening detected. When multiple source property paths could match a destination member name prefix, the generator requires explicit resolution. See below for three fix strategies.

**Handling Ambiguous Flattening (FKF530):**

Ambiguous flattening occurs when a destination member name matches multiple source paths. For example:

**BEFORE (generates FKF530 Error):**
```csharp
public class Address { public string City { get; set; } = ""; }
public class Source
{
    public Address Address { get; set; } = new();
    public Address AddressCity { get; set; } = new();  // Conflict: AddressCity matches both
}

public class Dest
{
    public string AddressCity { get; set; } = "";  // Ambiguous: could be Address.City OR AddressCity (direct)
}

[Forge]
public static partial class MyForges
{
    [ForgeMethod(AllowFlattening = true)]
    public static partial Dest ToDest(Source source);  // FKF530 Error — ambiguous
}
```

**Solution 1: Rename the destination member to be unambiguous**
```csharp
public class Dest
{
    [ForgeMap("Address.City")]
    public string NestedAddressCity { get; set; } = "";  // Now unambiguous
}
```

**Solution 2: Use [ForgeIgnore] to exclude one of the ambiguous sources**
```csharp
public class Source
{
    public Address Address { get; set; } = new();
    [ForgeIgnore] public Address AddressCity { get; set; } = new();  // Excluded, removes ambiguity
}
```

**Solution 3: Restructure the source types to avoid overlapping names**
```csharp
public class Source
{
    public Address MainAddress { get; set; } = new();  // Renamed: no longer "Address"
    public Address OptionalAddress { get; set; } = new();
}

public class Dest
{
    public string AddressCity { get; set; } = "";  // Now unambiguous: matches MainAddress.City
}
```

**Design Decision: Flattening vs Nested Forging**

Use **AllowFlattening = true** when:
- You want to flatten a deeply nested source structure into a single DTO property
- The source has read-only reference types at intermediate levels (no constructor parameters needed)
- You want automatic null-safety chaining with minimal code

Use **AllowNestedForging = true** when:
- The source and destination types differ at intermediate levels (type mismatch)
- You need custom logic for nested conversions
- You want to preserve strongly-typed structure

#### `IgnoreIfNull` (`ForgePolicy`, default: `ForgePolicy.Inherit`)

Controls whether all property assignments are wrapped in a null check. When set to `ForgePolicy.True`, the destination member is only assigned when the source value is not null. Particularly useful for update methods where you want to preserve existing values when the source field is null.

- **`ForgePolicy.Inherit`** (default) — inherit from global default (false / assign even if null)
- **`ForgePolicy.True`** — all assignments wrapped in null check
- **`ForgePolicy.False`** — assignments happen even if source is null

Can be overridden per-member using `ForgeMapAttribute.IgnoreIfNull`. Per-member settings always take precedence.

```csharp
[Forge]
public static partial class MyForges
{
    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
    public static partial void Update(Source source, Dest existing);
    // Generates: if (source.Name != null) existing.Name = source.Name;
}
```

#### `StrictMapping` (`bool`, default: `false`)

When true, unmapped destination members and unused source members are reported as **errors** instead of warnings, enabling compile-time drift detection. This ensures mappings stay in sync when source or destination types change.

- Unmapped destination members emit **FKF110** (Error) instead of FKF100 (Warning)
- Unused source members emit **FKF111** (Error) instead of FKF101 (Warning)

Use `[ForgeIgnore]` to explicitly exclude members that are intentionally unmapped.

```csharp
[Forge]
public static partial class MyForges
{
    [ForgeMethod(StrictMapping = true)]
    public static partial PersonDto ToDto(Person source);
    // Any unmapped or unused members will now cause a build error
}
```

#### `ShareReference` (`ForgePolicy`, default: `ForgePolicy.Inherit`)

Controls how Forge handles same-type **mutable collection** members (e.g. `List<T>` on both source and destination).

- **`ForgePolicy.Inherit`** (default) — inherit from global default (false / deep-copy)
- **`ForgePolicy.False`** — the generated code uses a copy constructor (`new List<T>(source.X)`) so the destination owns an independent collection. Mutations to the destination's collection do not affect the source.
- **`ForgePolicy.True`** — the generated code uses direct reference assignment (`dto.Tags = source.Tags`). Faster and allocation-free, but the source and destination share the same collection instance, so mutations leak across.

**Affected collection types** (ShareReference=true uses reference-sharing; ShareReference=false uses copy constructor):

| Type | Behavior | Notes |
|------|----------|-------|
| `List<T>` | Copy when false, Share when true | Most common mutable collection |
| `HashSet<T>` | Copy when false, Share when true | Unordered unique items |
| `Dictionary<K,V>` | Copy when false, Share when true | Key-value pairs |
| `T[]` | Copy when false, Share when true | Fixed-size arrays |
| `Collection<T>` | Copy when false, Share when true | Observable collection wrapper |
| Interface types (`IList`, `ICollection`, `IDictionary`, `IEnumerable`, `IReadOnlyList`, `IReadOnlyCollection`, `IReadOnlyDictionary`, `ISet`) | Copy when false, Share when true | Interface forms of mutable collections |

**Types that are ALWAYS direct-assigned (no ShareReference effect)**:
- **Immutable types** (`ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`, `ImmutableDictionary<K,V>`) — safe to share because mutations are impossible
- **Primitives & strings** (`int`, `string`, `bool`, etc.) — immutable and copied by value
- **Same-type custom classes** (e.g. `Address Home` on both sides) — Forge always reference-shares these and emits **FKF312** (Info). To deep-copy a custom class, use a distinct DTO type combined with `AllowNestedForging` and an explicit forge method.

When this flag is `true` and applies to a member, **FKF311** (Info) is emitted per member as an audit trail.

```csharp
[Forge]
public static partial class MyForges
{
    // Default (Inherit): deep-copy. Tags in PersonDto is a new List<string>, independent of source.
    public static partial PersonDto ToDto(Person source);

    // Explicit opt-out (True): reference-share. Tags in PersonUpdate IS source.Tags.
    [ForgeMethod(ShareReference = ForgePolicy.True)]
    public static partial PersonUpdate ToUpdate(Person source);
}
```

Can be overridden per-member via [`ForgeMap.ShareReference`](#forgemap-name).
See [Reference semantics for same-type collections](#reference-semantics-for-same-type-collections) below for the full precedence rules.

#### `GenerateExpression` (`bool`, default: `false`)

When true, the generator emits an additional static `Expression<Func<TSource, TDest>>` property
named `{MethodName}Expression` alongside the partial method body. The expression property is
suitable for use with EF Core / LINQ providers in `IQueryable.Select(...)` — the same mapping
runs as SQL against a database instead of materialising every row in memory.

The imperative method is unaffected. Setting this flag is purely additive: both the regular method
and the expression property exist after compilation.

```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonDto ToDto(Person source);
}

// Use the imperative method as before:
var dto = PersonForges.ToDto(person);

// Or push the projection to the database:
var dtos = await dbContext.People
    .Where(p => p.IsActive)
    .Select(PersonForges.ToDtoExpression)
    .ToListAsync();
```

**Requirements:**

- **EF Core 8 or later** if you intend to use the expression against a database. Earlier EF Core
  versions are out of support and not guaranteed to translate every shape Forge emits.

**Constraints and exclusions:**

- Cannot be used with update methods (void return, two parameters) — emits **FKF504** (Error)
  and blocks generation for the class.
- Before/after hooks are silently omitted from the expression property — emits **FKF505** (Warning).
  The hooks still run when the imperative method is invoked.
- Members whose conversion has no translatable expression-tree encoding are silently omitted from
  the expression property — emits **FKF506** (Info) per member with the reason. Affected cases:
  - Custom `[ForgeConverter]` calls (user methods can't translate to SQL)
  - `IgnoreIfNull`, `IgnoreIfDefault`, and `Condition` (runtime guards have no expression-tree
    equivalent — the imperative method still applies the guard; the expression omits the member)
  - Collection materializers other than `.ToList()` / `.ToArray()` (HashSet, ImmutableArray, etc.)
- Nested forge methods are **inlined** into the expression body (EF Core cannot translate
  `Expression.Invoke`). A cycle in the nested chain emits **FKF507** (Error). Inlining depth
  greater than four levels emits **FKF508** (Warning), and exceeding seven levels emits **FKF509** (Error).

See [projections.md](projections.md) for the full coverage matrix and translation reference.

---

## `[ForgeIgnore]`

**Namespace:** `FreakyKit.Forge`
**Target:** Property or Field

Excludes a property or field from forge mapping. By default, the member is skipped on **both** sides — no `FKF100`/`FKF101` warnings are emitted.

### Context Requirements

`[ForgeIgnore]` is primarily meaningful on **destination type members**. When placed on a destination type property or field, it prevents that member from being assigned during mapping.

If `[ForgeIgnore]` is detected on a member of a type that is not being used as a destination in any forge operation, the generator emits **FKF528** (Warning) to alert you that the attribute has no effect.

```csharp
// ✓ CORRECT: [ForgeIgnore] on destination type member
public class Source { public string Name { get; set; } }
public class Dest
{
    public string Name { get; set; }
    [ForgeIgnore]
    public string InternalField { get; set; }  // correctly excluded from mapping
}

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}

// ✗ INEFFECTIVE: [ForgeIgnore] on source type (emits FKF528)
public class Source
{
    public string Name { get; set; }
    [ForgeIgnore]
    public string InternalField { get; set; }  // FKF528 Warning — has no effect here
}
```

### Properties

#### `Side` (`ForgeIgnoreSide`, default: `ForgeIgnoreSide.Both`)

Controls which side of the mapping this ignore applies to.

| Value | Effect |
|-------|--------|
| `ForgeIgnoreSide.Both` | Member excluded on both source and destination sides (default) |
| `ForgeIgnoreSide.Source` | Member excluded only when it appears on the source side. Suppresses FKF101. The destination side still participates in matching. |
| `ForgeIgnoreSide.Destination` | Member excluded only when it appears on the destination side. Suppresses FKF100. The source side still participates in matching. |

```csharp
public class Source
{
    public string Name { get; set; }
    [ForgeIgnore] public string InternalId { get; set; }  // skipped on both sides (default)

    [ForgeIgnore(Side = ForgeIgnoreSide.Source)]
    public string AuditField { get; set; }  // not mapped from source, but dest can still use [ForgeMap] to reach another source member
}

public class Dest
{
    public string Name { get; set; }
    [ForgeIgnore(Side = ForgeIgnoreSide.Destination)]
    public int ComputedScore { get; set; }  // not populated by forge, but source's ComputedScore still participates
}
```

---

## `[ForgeMap]`

**Namespace:** `FreakyKit.Forge`
**Target:** Property, Field, or Constructor Parameter

Maps a property, field, or constructor parameter to a differently-named member on the counterpart type. The constructor parameter specifies the target member name.

### Context Requirements

`[ForgeMap]` is primarily meaningful on **destination type members** and **constructor parameters**. When placed on a destination type property or field, it customizes how that member maps from the source type.

If `[ForgeMap]` is detected on a member of a type that is not being used as a destination in any forge operation, the generator emits **FKF527** (Warning) to alert you that the attribute has no effect.

```csharp
// ✓ CORRECT: [ForgeMap] on destination type member
public class Source { public string FullName { get; set; } }
public class Dest
{
    [ForgeMap("FullName")]
    public string CompleteName { get; set; }  // correctly maps from source.FullName
}

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}

// ✗ INEFFECTIVE: [ForgeMap] on source type (emits FKF527)
public class Source
{
    [ForgeMap("CompleteName")]
    public string FullName { get; set; }  // FKF527 Warning — has no effect here
}
public class Dest { public string CompleteName { get; set; } }
```

### Constructor

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | `string` | The name of the counterpart member to map to/from |

### Properties

#### `DefaultValue` (`object?`, default: `null`)

Provides a fallback value for `Nullable<T>` → `T` mappings. When set, the generator emits `source.Prop ?? defaultValue` instead of `source.Prop.Value`, preventing `InvalidOperationException` at runtime. The `FKF201` warning is suppressed when a default value is provided.

Can be placed on either the source or destination member. Accepts any compile-time constant (numbers, strings, bools, etc.).

```csharp
public class Source { [ForgeMap("Age", DefaultValue = 0)] public int? Age { get; set; } }
public class Dest   { public int Age { get; set; } }
// Generates: __result.Age = source.Age ?? 0;
```

#### `IgnoreIfNull` (`ForgePolicy`, default: `ForgePolicy.Inherit`)

Controls whether the assignment for this member is wrapped in a null check. When set to `ForgePolicy.True`, the destination member is only assigned when the source value is not null. Useful for update methods where you want to preserve existing values.

- **`ForgePolicy.Inherit`** (default) — inherit the method-level setting, or the global default (false) if method-level is not set
- **`ForgePolicy.True`** — wrap assignment in null check
- **`ForgePolicy.False`** — assignment happens even if source is null

Can be placed on either the source or destination member. Overrides the method-level `IgnoreIfNull` setting (per-member takes priority).

```csharp
public class Source { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)] public string? Name { get; set; } }
public class Dest   { public string Name { get; set; } = ""; }
// Generates: if (source.Name != null) __result.Name = source.Name;
```

`IgnoreIfNull` (like `IgnoreIfDefault` and `Condition`) requires a runtime `if` around the assignment. It cannot be applied to an `init`-only or `required` destination member — those can only be set inside the constructor's object initializer, which has no way to conditionally skip a member. Doing so emits **FKF316** (Error) and blocks generation for that method.

#### `IgnoreIfDefault` (`bool`, default: `false`)

When true, the assignment for this member is wrapped in a default-value check: the destination member is only assigned when the source value is not equal to its type's default (null for references, 0 for numeric types, false for bool, Guid.Empty for Guid, etc.). Useful for PATCH/partial-update APIs where you want to preserve existing values when the client didn't provide a value.

Can be placed on either the source or destination member.

```csharp
[ForgeMap("Age", IgnoreIfDefault = true)]
public int Age { get; set; }
// Generates: if (!EqualityComparer<int>.Default.Equals(source.Age, default)) __result.Age = source.Age;
```

Combining with `IgnoreIfNull`:

```csharp
[ForgeMap("Name", IgnoreIfNull = true, IgnoreIfDefault = true)]
public string? Name { get; set; }
// Generates: if (source.Name != null && !EqualityComparer<string>.Default.Equals(source.Name, default)) __result.Name = source.Name;
```

`IgnoreIfDefault` has no expression-tree equivalent: if the method also has `GenerateExpression = true`, the member is omitted from the generated `Expression` property and **FKF506** (Info) is emitted — the imperative method still applies the guard normally.

Like `IgnoreIfNull`, `IgnoreIfDefault` can't be applied to an `init`-only or `required` destination member — emits **FKF316** (Error) and blocks generation.

#### `Condition` (`string?`, default: `null`)

When set, specifies the name of a static method that determines whether this member should be assigned. The method must accept the source type as a parameter and return `bool`. When the method returns `false`, the assignment is skipped.

Useful for conditional PATCH updates or complex validation logic. The method must be:
- Static
- Accessible (public or internal)
- Accept exactly one parameter of the source type
- Return `bool`

Resolution first looks on the current forge class, then falls back to classes listed in `[ForgeUses]`, in declaration order — the same lookup used for nested forge and converter methods. If the name matches a method in more than one included class, the first one wins and **FKF513** (warning) reports which class was used and which was shadowed, so the choice is never silent.

`Condition` has no expression-tree equivalent: if the method also has `GenerateExpression = true`, the conditioned member is omitted from the generated `Expression` property and **FKF506** (Info) is emitted — the imperative method still applies the guard normally.

Like `IgnoreIfNull`/`IgnoreIfDefault`, `Condition` can't be applied to an `init`-only or `required` destination member — emits **FKF316** (Error) and blocks generation.

```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod]
    public static partial PersonDto ToDto(Person source);

    [ForgeMap("Salary", Condition = nameof(CanUpdateSalary))]
    public decimal? Salary { get; set; }

    internal static bool CanUpdateSalary(Person source) => source.IsManager;
}
// Generates: if (CanUpdateSalary(source)) __result.Salary = source.Salary;
```

Combining with other conditions:

```csharp
[ForgeMap("Email", IgnoreIfDefault = true, Condition = nameof(CanUpdateEmail))]
public string Email { get; set; }
// Generates: if (!EqualityComparer<string>.Default.Equals(source.Email, default) && CanUpdateEmail(source))
//     __result.Email = source.Email;
```

#### `ShareReference` (`ForgePolicy`, default: `ForgePolicy.Inherit`)

Per-member override of [`ForgeMethod.ShareReference`](#forgemethod) for same-type mutable collections. Controls whether this specific member uses reference-sharing or deep-copying.

- **`ForgePolicy.Inherit`** (default) — inherit the method-level value, or the global default (false / deep-copy) if method-level is not set explicitly
- **`ForgePolicy.True`** — this member is reference-shared regardless of method-level setting
- **`ForgePolicy.False`** — this member is deep-copied regardless of method-level setting

Can be placed on either the source or destination member. See [Reference semantics for same-type collections](#reference-semantics-for-same-type-collections) for full precedence rules.

```csharp
[ForgeMethod(ShareReference = ForgePolicy.True)]                       // method default: share all
public static partial PersonDto ToDto(Person source);

public class Person
{
    // Inherits method-level share (True)
    public List<string> Tags { get; set; }

    // Per-member override — this one deep-copies even though method says share
    [ForgeMap("History", ShareReference = ForgePolicy.False)]
    public List<string> History { get; set; }
}
// Generates:
//   __result.Tags = source.Tags;                                                       // shared
//   __result.History = source.History != null ? new List<string>(source.History) : null; // copied
```

#### `NullFallback` (`NullFallback`, default: `NullFallback.Null`)

Controls what happens when a source member is null during nested forging.

**PRECONDITIONS** (all must be true, or this property is silently ignored):
- Source member must be a reference type (class, interface, nullable struct)
- Destination member must map to a nested-forged type (a forge method must exist for that type)
- The parent forge method must have `[ForgeMethod(AllowNestedForging = true)]`

When conditions are met:
- **`NullFallback.Null`** (default) — generate `source.Member != null ? ToDto(...) : null`
- **`NullFallback.DefaultConstruct`** — generate `source.Member != null ? ToDto(...) : new DestType()`

When conditions are NOT met, this property has no effect and is silently ignored.

Can only be placed on the destination member. Cannot be combined with `IgnoreIfNull` on the same member (emits `FKF315` error).

```csharp
public class Address { public string City { get; set; } = ""; }
public class AddressDto { public string City { get; set; } = ""; }
public class Source { public Address? Home { get; set; } }
public class Dest
{
    // When source.Home is null, creates a new AddressDto() instead of null
    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
    public AddressDto Home { get; set; } = new();
}

[Forge]
public static partial class MyForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial Dest ToDest(Source source);
    public static partial AddressDto ToAddressDto(Address source);
}

// Generates: __result.Home = source.Home != null ? ToAddressDto(source.Home) : new AddressDto();
```

### Usage Patterns

**Source-side mapping:** The attribute value names the destination member.

```csharp
public class Source { [ForgeMap("Name")] public string FirstName { get; set; } }
public class Dest   { public string Name { get; set; } }
// Generates: __result.Name = source.FirstName;
```

**Destination-side mapping:** The attribute value names the source member.

```csharp
public class Source { public string FirstName { get; set; } }
public class Dest   { [ForgeMap("FirstName")] public string Name { get; set; } }
// Generates: __result.Name = source.FirstName;
```

**Both sides with common key:** Both members use the same key to find each other.

```csharp
public class Source { [ForgeMap("CommonKey")] public string SrcName { get; set; } }
public class Dest   { [ForgeMap("CommonKey")] public string DstName { get; set; } }
// Generates: __result.DstName = source.SrcName;
```

**Constructor parameter mapping:** When a destination constructor parameter has a different name from the source property, place `[ForgeMap]` on the parameter to redirect the match.

```csharp
public class Source { public string FullName { get; set; } }
public class Dest
{
    public string Name { get; }
    public Dest([ForgeMap("FullName")] string name) { Name = name; }
}
// Generates: var __result = new Dest(source.FullName);
```

Without `[ForgeMap]`, the generator looks for a source member named `name` and emits `FKF501` if none is found.

### Diagnostics

- `FKF103` (Info) — custom mapping applied
- `FKF104` (Error) — target member not found
- `FKF105` (Warning) — duplicate target (multiple members map to the same key)

---

## `[ForgeDictionary]`

**Namespace:** `FreakyKit.Forge`
**Target:** Method (optional; applies to the entire forge method)

Controls dictionary mapping behavior when converting between dictionaries and domain objects. Apply to any `[ForgeMethod]` where the source parameter or return type is a dictionary.

**Auto-detection:** The generator automatically detects `Dictionary<string, T>` parameters/returns and generates appropriate mapping code. Use `[ForgeDictionary]` to customize the behavior via policies.

### Properties

#### `KeyCasing` (`KeyCasingPolicy`, default: `KeyCasingPolicy.Exact`)

Controls how dictionary keys are matched against property names:

| Policy | Behavior | Example |
|--------|----------|---------|
| **Exact** (default) | Match property name exactly | `FirstName` → `"FirstName"` |
| **IgnoreCase** | Case-insensitive matching | `FirstName` matches `"firstname"`, `"FIRSTNAME"`, etc. |
| **CamelCase** | Convert property to camelCase | `FirstName` → `"firstName"` |
| **SnakeCase** | Convert property to snake_case | `FirstName` → `"first_name"` |

```csharp
[ForgeMethod]
[ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase)]
public static partial Person FromDict(Dictionary<string, object> dict);
// Will look for "firstName", "age" in the dictionary
```

#### `MissingKeyPolicy` (`MissingKeyPolicy`, default: `MissingKeyPolicy.Throw`)

Controls behavior when a dictionary key is not found (dict→object only):

| Policy | Behavior | Use Case |
|--------|----------|----------|
| **Throw** (default) | Throw `KeyNotFoundException` | Strict validation—all keys required |
| **UseDefault** | Assign `default(T)` | Permissive—use default for missing keys |
| **Skip** | Don't assign—leave at default | Same as UseDefault but more explicit |
| **ReturnNull** | Assign null (nullable types only) | Allow null for missing optional fields |

```csharp
[ForgeMethod]
[ForgeDictionary(MissingKey = MissingKeyPolicy.UseDefault)]
public static partial Person FromDict(Dictionary<string, object> dict);
// Missing keys will use default values instead of throwing
```

#### `NullValue` (`NullValuePolicy`, default: `NullValuePolicy.Include`)

Controls whether null values are included when converting object to dictionary:

| Policy | Behavior | Use Case |
|--------|----------|----------|
| **Include** (default) | Add all properties including nulls | Include everything in output |
| **Skip** | Only add non-null values | Omit null properties from result |

```csharp
[ForgeMethod]
[ForgeDictionary(NullValue = NullValuePolicy.Skip)]
public static partial Dictionary<string, object> ToDict(Person person);
// Null properties will be excluded from the generated dictionary
```

### Examples

**JSON deserialization with CamelCase keys:**
```csharp
public class ApiResponse
{
    public string FirstName { get; set; } = "";
    public int Age { get; set; }
}

[Forge]
public static partial class ApiForges
{
    [ForgeMethod]
    [ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase)]
    public static partial ApiResponse FromJson(Dictionary<string, object> json);
}

// JSON: { "firstName": "John", "age": 30 }
var response = ApiForges.FromJson(apiData);  // Maps correctly
```

**Configuration with defaults:**
```csharp
[Forge]
public static partial class ConfigForges
{
    [ForgeMethod]
    [ForgeDictionary(
        KeyCasing = KeyCasingPolicy.IgnoreCase,
        MissingKeyPolicy = MissingKeyPolicy.UseDefault
    )]
    public static partial AppSettings FromConfig(Dictionary<string, object> config);
}
```

**Omit null values:**
```csharp
[Forge]
public static partial class ApiForges
{
    [ForgeMethod]
    [ForgeDictionary(NullValuePolicy = NullValuePolicy.Skip)]
    public static partial Dictionary<string, object> ToApiFormat(UserDto user);
}

var user = new UserDto { Id = 1, Name = "Alice", Email = null };
var apiData = ApiForges.ToApiFormat(user);
// Result: { "Id": 1, "Name": "Alice" } — Email omitted
```

### Supported Dictionary Types

- `Dictionary<string, object>` ✅ (with casting)
- `Dictionary<string, string>` ✅ (with parsing for primitives)
- `IReadOnlyDictionary<string, T>` ✅
- `IDictionary<string, T>` ✅

**Non-string keys are not supported** (emits FKF700 diagnostic).

### Type Conversion

**Dictionary<string, object>:** Values are cast to the target type. Type mismatches throw `InvalidCastException` at runtime.

**Dictionary<string, string>:** Values are parsed using type-specific parsers:
- Primitives: `int.Parse()`, `bool.Parse()`, `double.Parse()`, etc.
- Enums: `Enum.Parse<EnumType>(value)`
- DateTime: `DateTime.Parse(value, CultureInfo.InvariantCulture)`
- Guid: `Guid.Parse(value)`

### Diagnostics

- **FKF700** — Dictionary key type is not string (only `Dictionary<string, T>` supported)
- **FKF701** — Unsupported dictionary value type (complex types, collections)
- **FKF702** — ReturnNull policy used on non-nullable type

---

## Reference semantics for same-type collections

When a member has the **same exact type** on source and destination (e.g. `List<string>` → `List<string>`), Forge has two possible semantics:

1. **Deep-copy** (default) — emit `new List<T>(source.X)` so the destination owns an independent instance. Mutations to the destination's collection do not affect the source.
2. **Reference-share** — emit direct assignment `dto.X = source.X` so source and destination share the same instance. Faster and allocation-free, but mutations leak across.

The choice is controlled by the `ShareReference` flag, which can appear in three places.

### Precedence

When multiple `ShareReference` values could apply to the same member, the most specific **explicit** value wins. Inheritance is resolved top-to-bottom until an explicit (non-Inherit) value is found:

```
1. Destination-side  [ForgeMap(ShareReference = X)]   ← most specific
   If explicit (True/False), use it. If Inherit, continue.
2. Source-side [ForgeMap(ShareReference = X)]
   If explicit (True/False), use it. If Inherit, continue.
3. Method-level [ForgeMethod(ShareReference = X)]
   If explicit (True/False), use it. If Inherit, continue.
4. Default: ForgePolicy.False (deep-copy)            ← least specific
```

**Key behavior:**
- `ForgePolicy.Inherit` means "use the next level up" — it's transparent in the chain
- Explicit `True` or `False` always wins and stops the chain immediately
- When both source-side and destination-side are explicit with different values, the **destination-side wins** and **FKF313** (Warning) is emitted to surface the conflict

**Example:**
```csharp
[ForgeMethod(ShareReference = ForgePolicy.Inherit)]  // Explicitly inherit from default
public static partial PersonDto ToDto(Person source);

public class Person
{
    // No [ForgeMap] → inherits method-level (Inherit) → inherits default (False/deep-copy)
    public List<string> Tags { get; set; }

    // Explicit override with True
    [ForgeMap("History", ShareReference = ForgePolicy.True)]
    public List<string> History { get; set; }
}
// Generates:
//   __result.Tags = new List<string>(source.Tags);    // deep-copy (inherited default)
//   __result.History = source.History;                 // reference-shared (explicit)
```

### What's affected

| Type | Default behavior |
|---|---|
| `string`, `int`, `bool`, primitives | Direct assignment (immutable / value type) |
| `Address` (custom class, same type both sides) | Reference-shared **always**, emits FKF312 |
| `List<T>`, `Dictionary<K,V>`, `HashSet<T>` | **Deep-copy** by default (new in this release) |
| `T[]` | **Copy via `.ToArray()`** by default |
| `IList<T>`, `ICollection<T>`, `IEnumerable<T>` | **Copy via `new List<T>(source)`** |
| `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyDictionary<K,V>` | **Copy via `new List<T>` / `new Dictionary<K,V>`** |
| `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>` | Direct assignment (immutable, safe to share) |
| `ReadOnlyCollection<T>` | **Copy via `new ReadOnlyCollection<T>(new List<T>(source))`** |
| `Collection<T>` | **Copy via `new Collection<T>(new List<T>(source))`** |

### Why this is the default

Most users of object-mapping libraries expect the destination to be a separate object — that's the entire point of having a DTO. Sharing a `List<T>` between source entity and DTO is a footgun: a `dto.Tags.Add(...)` call would mutate the source entity's `Tags` collection too. The deep-copy default makes Forge match user expectations and matches AutoMapper / Mapperly / Mapster, which all deep-copy by default.

The opt-out exists for hot paths where you've measured the allocation overhead and decided you want the speed.

### Custom classes are different

Forge does **not** auto-clone same-type custom classes. If you have:

```csharp
public class Source { public Address Home { get; set; } }
public class Dest   { public Address Home { get; set; } }   // same exact type
```

Forge emits `dto.Home = source.Home` (reference share) and **FKF312** (Info) flags it. To get a deep copy, change `Dest.Home` to a distinct DTO type and use `AllowNestedForging`:

```csharp
public class Source { public Address Home { get; set; } }
public class Dest   { public AddressDto Home { get; set; } }  // distinct type

[Forge]
public static partial class MyForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial Dest ToDest(Source source);
}
// Generates: __result.Home = source.Home != null ? ToAddressDto(source.Home) : null;
```

### Related diagnostics

- **FKF311** (Info) — fires per-member when a same-type mutable collection is reference-shared (audit trail when you opt out of the default).
- **FKF312** (Info) — fires for same-type custom class members (these are always reference-shared).
- **FKF313** (Warning) — fires when source-side and destination-side `[ForgeMap]` set `ShareReference` to different values. Destination-side wins.

See [docs/diagnostics.md](diagnostics.md) for full diagnostic reference.

---

## `[ForgeConverter]`

**Namespace:** `FreakyKit.Forge`
**Target:** Method (`static` method only)

Marks a static method as a type converter for forge mapping. When a member type mismatch is encountered, the generator resolves it using a priority chain: nullable handling, enum mapping, collection mapping, then type converters, then nested forging. Converters are checked **after** collection mapping but **before** nested forging. If nothing resolves the mismatch, `FKF200` is emitted.

### Context Requirements

`[ForgeConverter]` can only be used on methods in classes decorated with `[Forge]`. If a `[ForgeConverter]` attribute is found on a method in a class without `[Forge]`, the generator emits **FKF526** (Error) and does not register the converter.

```csharp
// ✗ INVALID: No [Forge] on class
public static class MyNonForgeClass
{
    [ForgeConverter]
    public static string ConvertInt(int value) => value.ToString();  // FKF526 Error
}

// ✓ VALID: Class has [Forge]
[Forge]
public static partial class MyForges
{
    [ForgeConverter]
    public static string ConvertInt(int value) => value.ToString();
}
```

### Method Requirements

The converter method must be:
- `static`
- Non-void return type (the destination type)
- Exactly one parameter (the source type)
- Non-generic (no type parameters)
- In the same forge class

Methods that violate these requirements are silently ignored by the generator and emit **FKF221** (Warning) from the analyzer. Without the warning, a misconfigured converter can silently fail to resolve a type mismatch, causing an unexpected FKF200 error.

```csharp
[Forge]
public static partial class MyForges
{
    public static partial Dest ToDest(Source source);

    [ForgeConverter]
    public static string ConvertDateTime(DateTime value) => value.ToString("yyyy-MM-dd");
    // Used when source.Birthday (DateTime) maps to dest.Birthday (string)
    // Generates: __result.Birthday = ConvertDateTime(source.Birthday);
}
```

### Diagnostics

- `FKF220` (Info) — converter used for a member mapping
- `FKF221` (Warning) — converter method has an invalid signature and will be ignored

---

## `[ForgePolymorphic]`

**Namespace:** `FreakyKit.Forge`
**Targets:** Methods (`AllowMultiple = true`)

Declares a polymorphic dispatch arm on a forge method. The method generates a switch expression that dispatches to other forge methods based on the runtime type of the source parameter. The method itself performs no property mapping — it is pure dispatch.

### Constructor Parameters

#### `DerivedSourceType` (`Type`)

The derived source type to match in the switch expression pattern. Must be the same as or a subtype of the dispatch method's source parameter type.

#### `MappingMethodName` (`string`)

The name of the forge method to call for this derived type. Use `nameof()` for rename safety. Must exist in the same forge class or in a `[ForgeUses]` included class.

### Behavior

- Arms are emitted in **user-declared order** (the order `[ForgePolymorphic]` attributes appear on the method).
- The default arm always throws `InvalidOperationException`. There is no implicit base-type fallback.
- To add a base-type fallback, declare it as an explicit `[ForgePolymorphic]` arm (e.g., `[ForgePolymorphic(typeof(Animal), nameof(MapBase))]`).
- Each referenced method's return type must be assignable to the dispatch method's return type (inheritance or interface implementation).
- `[ForgeMethod]` options (`GenerateExpression`, `AllowFlattening`, `ShouldIncludeFields`, etc.) are incompatible — emit error if set to non-default values.

### Example — Inheritance Hierarchy

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
//     Dog dog => MapDog(dog),
//     Cat cat => MapCat(cat),
//     _ => throw new InvalidOperationException(...)
// };
```

### Example — With Base Fallback

```csharp
[ForgePolymorphic(typeof(Dog), nameof(MapDog))]
[ForgePolymorphic(typeof(Animal), nameof(MapBase))]
public static partial AnimalDto MapAny(Animal source);
// Dog arm checked first, then Animal catches everything else
```

### Example — Interface Return Type

```csharp
[ForgePolymorphic(typeof(Dog), nameof(MapDog))]
[ForgePolymorphic(typeof(Cat), nameof(MapCat))]
public static partial IAnimalDto MapAny(Animal source);
// DogDto and CatDto must implement IAnimalDto
```

### Diagnostics

| ID | Severity | Condition |
|---|---|---|
| `FKF800` | Error | Referenced method not found |
| `FKF801` | Error | Method return type not assignable to dispatch return type |
| `FKF802` | Error | Derived source type not assignable from method source parameter type |
| `FKF803` | Error | Unreachable pattern (less-derived type appears before derived type) |
| `FKF804` | Error | Incompatible `[ForgeMethod]` options set on polymorphic method |
| `FKF805` | Error | `GenerateExpression = true` on polymorphic method |
| `FKF806` | Error | Duplicate derived source type |
| `FKF807` | Error | `[ForgePolymorphic]` on a method without `[Forge]` on containing class |

---

## `ForgeIgnoreSide` (Enum)

**Namespace:** `FreakyKit.Forge`

Controls which side of the mapping a `[ForgeIgnore]` attribute applies to.

| Value | Numeric Value | Description |
|-------|---------------|-------------|
| `Both` | `0` | Member excluded on both source and destination sides (default) |
| `Source` | `1` | Excluded only on the source side — suppresses FKF101 |
| `Destination` | `2` | Excluded only on the destination side — suppresses FKF100 |

---

## `ForgeMode` (Enum)

**Namespace:** `FreakyKit.Forge`

Controls which methods in a forge class are treated as forge methods.

| Value | Numeric Value | Description |
|-------|---------------|-------------|
| `Implicit` | `0` | All properly-shaped static partial methods are forge methods |
| `Explicit` | `1` | Only `[ForgeMethod]`-decorated methods are forge methods |

### What Is a "Properly-Shaped" Method?

A method is a valid forge method candidate if it is:

1. `static`
2. `partial` (declaration only — no body)
3. Returns a non-void type (the destination), OR is void with 2 parameters (update mode)
4. Has exactly one parameter (create mode) or exactly two parameters (update mode)
5. Has no type parameters (not generic)

---

## Design Decision: Flattening vs Nested Forging

When mapping hierarchical source models to flat DTOs, you have two main options: **flattening** and **nested forging**. Choosing the right approach depends on your use case.

### Flattening (`AllowFlattening = true` on `[ForgeMethod]`)

**When to use:**
- Combining multiple source levels into single destination properties (e.g., `Customer.Address.City` → `CustomerAddressCity`)
- Arbitrary-depth nesting (one level, two levels, three levels, and beyond)
- Implicit, automatic discovery — no intermediate forge method needed
- DTOs with denormalized, prefixed fields

**Pros:**
- No separate forge method required
- Automatic member discovery by name pattern
- Simpler for one-off flattening scenarios
- Clean, denormalized DTO design
- Supports arbitrary nesting depth — traverse as many levels as needed
- Automatic null-safety with `?.` for reference types and `.` for value types

**Cons:**
- Names must follow prefix pattern (e.g., `Customer.Address.City` → `CustomerAddressCity`)
- Only property traversal — fields are not considered even when `ShouldIncludeFields = true`

**Example:**
```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod(AllowFlattening = true)]
    public static partial PersonDto ToDto(Person source);
    // source.Company.Address.City → auto-mapped to dest.CompanyAddressCity
}
```

### Nested Forging (`AllowNestedForging = true` on `[ForgeMethod]`)

**When to use:**
- Type mismatches between source and destination (e.g., `Address` entity → `AddressDto`)
- Multi-level hierarchies (3+ levels of nesting)
- Explicit control over sub-mappings
- Reusing forge methods across multiple parent types
- Building modular forge class hierarchies

**Pros:**
- Explicit control — you decide which forge method handles each sub-type
- Handles type mismatches automatically
- Reusable across multiple parent mappings
- Scales to arbitrary nesting depth
- Type-safe and testable in isolation

**Cons:**
- Requires separate forge method for each nested type
- More setup code (additional `[Forge]` classes or methods)
- Automatic discovery limited to current class (use `[ForgeUses]` for cross-class discovery)

**Example:**
```csharp
[Forge]
public static partial class AddressForges
{
    public static partial AddressDto ToDto(Address source);
}

[Forge]
[ForgeUses(typeof(AddressForges))]
public static partial class PersonForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
    // source.Address (Address entity) → discovered in AddressForges as ToDto(...)
}
```

### Decision Table

| Scenario | Use Flattening | Use Nested Forging |
|----------|----------------|--------------------|
| Single-level denormalization (`Address.City` → `AddressCity`) | ✅ | ✔ (overcomplicated) |
| Two-level flattening (`Address.Coords.Lat` → `AddressCoordsLat`) | ✅ | ✔ (works but unnecessary) |
| Multi-level flattening (3+ levels) | ✅ | ✔ (works but unnecessary) |
| Type mismatch (Entity → DTO) | ❌ | ✅ |
| Reuse across multiple parents | ❌ | ✅ |
| Simple attribute copy (same types) | ✅ | ✔ (works but unnecessary) |
| Custom transformation needed | ❌ | ✅ (with [ForgeConverter]) |

---

## Attribute Feature Interaction Matrix

Quick reference for which `[ForgeMethod]` features work together:

| Feature A | Feature B | Compatible? | Notes |
|-----------|-----------|-------------|-------|
| `AllowFlattening` | `AllowNestedForging` | ✅ | Both can be true; flattening applies to unmatched members, nested forging applies to matched object members |
| `AllowNestedForging` | `GenerateExpression` | ✅ | Nested forge calls are inlined into expressions (FKF507 detects cycles) |
| `AllowFlattening` | `GenerateExpression` | ✅ | Flattened paths become expression-tree property chains |
| `IgnoreIfNull` | `GenerateExpression` | ❌ | `IgnoreIfNull` has no expression-tree equivalent; member is silently omitted from expression (FKF506) |
| `IgnoreIfDefault` | `GenerateExpression` | ❌ | `IgnoreIfDefault` has no expression-tree equivalent; member is silently omitted from expression (FKF506) |
| `Condition` | `GenerateExpression` | ❌ | The condition guard has no expression-tree equivalent; member is silently omitted from expression (FKF506) |
| `ShareReference` | `AllowNestedForging` | ✅ | ShareReference applies to collections; nested forging applies to object members |
| `GenerateExpression` | Update methods (void) | ❌ | Expressions invalid for void returns; emits FKF504 (Error) |
| `AllowFlattening` | `StrictMapping` | ✅ | Flattened members count as mapped (no FKF100/FKF110) |
| `AllowNestedForging` | `StrictMapping` | ✅ | Nested mapped members count as matched (no FKF100/FKF110) |
| `[ForgeConverter]` | `GenerateExpression` | ❌ | Converter calls can't translate to SQL; member is silently omitted from expression (FKF506) |

| `[ForgePolymorphic]` | Any `[ForgeMethod]` option | ❌ | Polymorphic dispatch methods are pure switch expressions; all `[ForgeMethod]` options are incompatible (FKF804/FKF805) |

**Rule of thumb:** Features interact smoothly unless one is expression-tree related (`GenerateExpression`) and the other has no SQL translation (`IgnoreIfNull`, `IgnoreIfDefault`, `Condition`, `[ForgeConverter]` calls, custom materialization). `[ForgePolymorphic]` methods are pure dispatch and incompatible with all `[ForgeMethod]` options.

---

## `ForgePolicy` (Enum)

**Namespace:** `FreakyKit.Forge`

A tri-state enum for properties that support method-level configuration with per-member overrides. Distinguishes between "not set" (inherit from above) and "explicitly false", which is important for correct precedence evaluation.

| Value | Numeric Value | Description |
|-------|---------------|-------------|
| `Inherit` | `0` | Inherit the setting from method-level configuration, or use the global default if unset at method level (default) |
| `True` | `1` | Explicitly set to true |
| `False` | `2` | Explicitly set to false |

Used for:
- `[ForgeMethod] ShareReference` and `IgnoreIfNull`
- `[ForgeMap] ShareReference` and `IgnoreIfNull`

---

## `ForgeMapping` (Enum)

**Namespace:** `FreakyKit.Forge`

Controls how enum-to-enum mappings are generated.

| Value | Numeric Value | Description |
|-------|---------------|-------------|
| `Cast` | `0` | Direct cast: `(DestEnum)source.Value`. Works when both enums share the same underlying integer values. |
| `ByName` | `1` | Switch expression mapping by member name. Safer when enums may have different underlying values. |
