# Features Guide

Comprehensive documentation for every Forge feature, with examples and configuration options.

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

## Dictionary Mapping

Dictionaries are mapped when both source and destination are `Dictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`, or `IReadOnlyDictionary<TKey, TValue>`. Keys must share the same type.

**Same value type** — top-level projection uses the copy constructor:

```csharp
public static partial Dictionary<string, Item> Copy(Dictionary<string, Item> source);

// Generates:
// if (source == null) return null;
// return new Dictionary<string, Item>(source);
```

**Different value types** — requires a forge method for the value type, used via `AllowNestedForging`:

```csharp
public static partial OrderDto MapOrder(Order source);
public static partial Dictionary<string, OrderDto> MapOrders(Dictionary<string, Order> source);

// Generates:
// if (source == null) return null;
// var __result = new Dictionary<string, OrderDto>(source.Count);
// foreach (var __kvp in source)
//     __result[__kvp.Key] = MapOrder(__kvp.Value);
// return __result;
```

Dictionary properties on source/destination types are handled automatically. When value types differ, set `AllowNestedForging = true` on the forge method:

```csharp
[ForgeMethod(AllowNestedForging = true)]
public static partial Dest ToDto(Source source);

// Source.Items: Dictionary<string, Order>  →  Dest.Items: Dictionary<string, OrderDto>
// Generates: __result.Items = source.Items != null
//     ? source.Items.ToDictionary(__kvp => __kvp.Key, __kvp => MapOrder(__kvp.Value))
//     : null;
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

## Conditional Mapping

Skip assignments based on source value conditions. Useful for update methods or partial updates where you want to preserve existing values.

### Ignore If Null

Skip assignments when the source value is null.

**Method-level** — applies to all assignments:

```csharp
[Forge]
public static partial class MyForges
{
    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
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
    [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)]
    public string? Name { get; set; }
    public string? Email { get; set; }
}

// Generates:
// if (source.Name != null) __result.Name = source.Name;
// __result.Email = source.Email;  (no null check)
```

### Ignore If Default

Skip assignments when the source value is at its type's default (0, false, null, Guid.Empty, etc.). Useful for PATCH endpoints where default means "don't update this field":

```csharp
public class UpdateDto
{
    [ForgeMap("Price", IgnoreIfDefault = true)]
    public decimal NewPrice { get; set; }

    [ForgeMap("Active", IgnoreIfDefault = true)]
    public bool IsActive { get; set; }
}

// Generates:
// if (source.NewPrice != default(decimal)) __result.Price = source.NewPrice;
// if (source.IsActive != default(bool)) __result.Active = source.IsActive;
```

### Custom Condition

Skip assignments based on a custom predicate method. Useful for complex validation logic:

```csharp
[Forge]
public static partial class OrderForges
{
    public static partial OrderDto ToDto(Order source);

    [ForgeMap("DiscountedPrice", Condition = nameof(IsEligibleForDiscount))]
    public decimal Price { get; set; }

    private static bool IsEligibleForDiscount(Order source) => source.TotalAmount > 100;
}

// Generates:
// if (IsEligibleForDiscount(source)) __result.DiscountedPrice = source.Price;
```

The condition method must:
- Be `static`
- Accept exactly one parameter of the source type
- Return `bool`
- Be declared on the same forge class or discovered via `[ForgeUses]`

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

## Mapping Profiles / Inheritance

Reuse base-type property assignments across derived methods using `[ForgeIncludes]`:

```csharp
[Forge]
public static partial class AddressForges
{
    public static partial AddressDto ToDto(Address source);
}

[Forge]
[ForgeIncludes(typeof(AddressForges))]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    // If Person contains an Address property and PersonDto contains AddressDto,
    // the Address mapping from AddressForges is automatically inlined
}
```

The generator:
- Discovers compatible methods in included forge classes
- Inlines their property assignments into the derived method
- Validates type compatibility via `ClassifyConversion`
- Handles diamond dependencies via deduplication
- Skips members not found on destination or already provided by constructor

## Polymorphic Mapping

Map derived types with pure dispatch using `[ForgePolymorphic]`:

```csharp
[Forge]
public static partial class AnimalForges
{
    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
    public static partial AnimalDto ToDto(Animal source);

    public static partial DogDto MapDog(Dog source);
    public static partial CatDto MapCat(Cat source);
}
```

Generates a switch expression:

```csharp
__result = source switch
{
    Dog d => MapDog(d),
    Cat c => MapCat(c),
    _ => throw new NotSupportedException(...)
};
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
| `IgnoreIfNull` | `ForgePolicy` | `Inherit` | Wrap all assignments in null checks — skip when source is null. `Inherit` uses default (skip checks), `True` forces null checks on all members, `False` forces no null checks. Can be overridden per-member via `[ForgeMap(IgnoreIfNull = ...)]` |
| `StrictMapping` | `bool` | `false` | Escalate unmapped/unused member warnings to errors (drift detection) |
| `GenerateExpression` | `bool` | `false` | Emit a static `Expression<Func<TSource, TDest>>` property alongside the partial method body, usable in `IQueryable.Select(...)` with EF Core 8+. See [docs/projections.md](docs/projections.md). |
| `ShareReference` | `ForgePolicy` | `Inherit` | Controls how same-type mutable collection members are handled. `Inherit` uses default (copy), `True` assigns by reference (faster, shared instance), `False` copy-constructs (independent instance). Can be overridden per-member via `[ForgeMap(ShareReference = ...)]` |

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
| `IgnoreIfNull` | `ForgePolicy` | When `True`, wraps the assignment in `if (source.X != null)` — skips when source is null. When `Inherit`, uses `[ForgeMethod(IgnoreIfNull)]` setting. |
| `ShareReference` | `ForgePolicy` | Per-member override of `[ForgeMethod(ShareReference)]`. Destination-side `[ForgeMap]` wins over source-side over method-level. Controls deep-copy vs reference-share for same-type collection members. |
| `NullFallback` | `NullFallback` | For nested forging with `AllowNestedForging = true`: when source member is null, return null (default) or `DefaultConstruct` (construct empty instance). Only applies when source is reference type and destination has a forge method. |
| `IgnoreIfDefault` | `bool` | When true, wraps the assignment in `if (source.X != default)` — skips when source is at its type's default value (0, false, null, Guid.Empty, etc.). Useful for partial updates where default means "don't change". |
| `Condition` | `string` | Name of a static `bool` method on the forge class that accepts the source type and returns whether this member should be assigned. When the method returns false, the assignment is skipped. Example: `Condition = nameof(IsValidPrice)` |

### `[ForgeConverter]`

Applied to a `static` method. Marks it as a type converter. The method must be non-void, non-generic, and take exactly one parameter. Invalid signatures emit FKF221.

### `[ForgeDictionary]`

Applied to a `[ForgeMethod]` when the method signature involves `Dictionary<string, T>` types. Controls how dictionary-to-object and object-to-dictionary conversions work.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `KeyCasing` | `KeyCasingPolicy` | `Exact` | How to match dictionary keys against destination member names. `Exact` (match as-is), `IgnoreCase` (case-insensitive), `CamelCase` (transform "PersonFirstName" → "personFirstName"), or `SnakeCase` ("person_first_name") |
| `MissingKey` | `MissingKeyPolicy` | `Throw` | What to do when a required key is missing during dict-to-object mapping. `Throw` (error), `UseDefault` (assign default value), `Skip` (leave uninitialized), or `ReturnNull` (assign null, only for nullable types) |
| `NullValue` | `NullValuePolicy` | `Include` | How to handle null values in object-to-dictionary conversion. `Include` (add to dict with null value), or `Skip` (don't add to dict) |

### `[ForgeUses]`

Applied to a `static partial class`. Declares that this forge class uses other forge classes for nested method discovery. When a nested forge method lookup fails in the current class, the generator searches included classes in order.

```csharp
[Forge]
[ForgeUses(typeof(AddressForges), typeof(CompanyForges))]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    // If ToDto needs to map nested Address or Company types,
    // the generator searches AddressForges then CompanyForges for the appropriate methods
}
```

### `[ForgeIncludes]`

Applied to a `static partial class`. Declares that this forge class includes property assignments from other forge classes. Compatible methods from included classes are discovered and their assignments are inlined into derived methods.

```csharp
[Forge]
[ForgeIncludes(typeof(AddressForges))]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    // Address-to-AddressDto mapping assignments are inlined if Person has Address and PersonDto has AddressDto
}
```

### `[ForgePolymorphic]`

Applied to a `static partial` method with `AllowMultiple = true`. Declares a polymorphic type mapping arm. The method becomes a pure dispatch method that generates a switch expression over derived source types.

| Parameter | Type | Description |
|-----------|------|-------------|
| `derivedSourceType` | `Type` | The derived source type to match in the switch expression pattern |
| `mappingMethodName` | `string` | The name of the forge method to call for this derived type. Must exist in the same forge class or in a `[ForgeUses]` included class. |

```csharp
[Forge]
public static partial class AnimalForges
{
    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
    public static partial AnimalDto ToDto(Animal source);

    public static partial DogDto MapDog(Dog source);
    public static partial CatDto MapCat(Cat source);
}
```

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

### `ForgePolicy`

Tri-state enum for attribute properties that can be inherited or explicitly overridden.

| Value | Description |
|-------|-------------|
| `Inherit` | Use the method-level setting, or the global default if unset at method level |
| `True` | Explicitly set to true |
| `False` | Explicitly set to false |

### `ForgeMapping`

| Value | Description |
|-------|-------------|
| `Cast` | Direct cast: `(DestEnum)source.Value` |
| `ByName` | Switch expression mapping by member name |

### `NullFallback`

Controls behavior when a source member is null during nested forging.

| Value | Description |
|-------|-------------|
| `Null` | Return null for the destination member (default) |
| `DefaultConstruct` | Construct a default instance of the destination type (e.g., `new DestType()` or empty collection) |

### `KeyCasingPolicy`

Controls how dictionary keys are matched against destination member names.

| Value | Description |
|-------|-------------|
| `Exact` | Match member name exactly (default) |
| `IgnoreCase` | Case-insensitive matching |
| `CamelCase` | Transform member name to camelCase (e.g., "PersonFirstName" → "personFirstName") |
| `SnakeCase` | Transform member name to snake_case (e.g., "person_first_name") |

### `MissingKeyPolicy`

Controls behavior when a required key is not found during dict-to-object mapping.

| Value | Description |
|-------|-------------|
| `Throw` | Throw `KeyNotFoundException` (default, safest) |
| `UseDefault` | Assign the default value (0, null, etc.) |
| `Skip` | Skip the assignment entirely (member left uninitialized) |
| `ReturnNull` | Assign null (only valid for nullable types) |

### `NullValuePolicy`

Controls how null values are handled in object-to-dictionary conversion.

| Value | Description |
|-------|-------------|
| `Include` | Include all values in the dictionary, even if null (default) |
| `Skip` | Skip null values; do not add them to the dictionary |
