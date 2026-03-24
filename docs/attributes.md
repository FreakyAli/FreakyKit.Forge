# Attributes Reference

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

---

## `[ForgeMethod]`

**Namespace:** `FreakyKit.Forge`
**Target:** Method (`static partial` method only)

Marks a method as a forge method and configures its mapping behavior. In `ForgeMode.Explicit`, this attribute is required. In `ForgeMode.Implicit`, it is optional and provides per-method configuration.

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

When true, the generator attempts to flatten nested source properties into flat destination members. When a destination member has no direct match, the generator tries prefix matching: `AddressCity` → `source.Address.City`.

Only one level of nesting is supported. Flattening only traverses **properties** on intermediate types — fields are not considered for traversal, even when `ShouldIncludeFields = true`.

```csharp
[ForgeMethod(AllowFlattening = true)]
public static partial PersonDto ToDto(Person source);
// dest.AddressCity = source.Address.City
```

#### `IgnoreIfNull` (`bool`, default: `false`)

When true, all property assignments are wrapped in a null check. The destination member is only assigned when the source value is not null. Particularly useful for update methods where you want to preserve existing values when the source field is null.

Can be overridden per-member using `ForgeMapAttribute.IgnoreIfNull`.

```csharp
[Forge]
public static partial class MyForges
{
    [ForgeMethod(IgnoreIfNull = true)]
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

---

## `[ForgeIgnore]`

**Namespace:** `FreakyKit.Forge`
**Target:** Property or Field

Excludes a property or field from forge mapping. By default, the member is skipped on **both** sides — no `FKF100`/`FKF101` warnings are emitted.

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

#### `IgnoreIfNull` (`bool`, default: `false`)

When true, the assignment for this member is wrapped in a null check: the destination member is only assigned when the source value is not null. Useful for update methods where you want to preserve existing values.

Can be placed on either the source or destination member. Overrides the method-level `IgnoreIfNull` setting (per-member takes priority).

```csharp
public class Source { [ForgeMap("Name", IgnoreIfNull = true)] public string? Name { get; set; } }
public class Dest   { public string Name { get; set; } = ""; }
// Generates: if (source.Name != null) __result.Name = source.Name;
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

## `[ForgeConverter]`

**Namespace:** `FreakyKit.Forge`
**Target:** Method (`static` method only)

Marks a static method as a type converter for forge mapping. When a member type mismatch is encountered, the generator resolves it using a priority chain: nullable handling, enum mapping, collection mapping, then type converters, then nested forging. Converters are checked **after** collection mapping but **before** nested forging. If nothing resolves the mismatch, `FKF200` is emitted.

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

## `ForgeMapping` (Enum)

**Namespace:** `FreakyKit.Forge`

Controls how enum-to-enum mappings are generated.

| Value | Numeric Value | Description |
|-------|---------------|-------------|
| `Cast` | `0` | Direct cast: `(DestEnum)source.Value`. Works when both enums share the same underlying integer values. |
| `ByName` | `1` | Switch expression mapping by member name. Safer when enums may have different underlying values. |
