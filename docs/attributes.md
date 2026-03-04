# Attributes Reference

## `[ForgeClass]`

**Namespace:** `FreakyKit.Forge`
**Target:** Class (`static partial class` only)

Marks a static partial class as a forge class. The source generator discovers all valid forge methods within this class and generates their implementations.

### Properties

#### `Mode` (`ForgeMode`, default: `ForgeMode.Implicit`)

Controls which methods in the class are treated as forge methods.

- **`ForgeMode.Implicit`** — all properly-shaped static partial methods are automatically treated as forge methods. No additional attributes needed on methods.
- **`ForgeMode.Explicit`** — only methods explicitly decorated with `[Forge]` are treated as forge methods. Unmarked candidate methods emit `FKF002`.

```csharp
// Implicit (default) — both methods are forged
[ForgeClass]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    public static partial PersonSummary ToSummary(Person source);
}

// Explicit — only ToDto is forged, ToSummary emits FKF002
[ForgeClass(Mode = ForgeMode.Explicit)]
public static partial class PersonForges
{
    [Forge]
    public static partial PersonDto ToDto(Person source);
    public static partial PersonSummary ToSummary(Person source);
}
```

#### `IncludePrivateMethods` (`bool`, default: `false`)

When true, private forge methods are included in generation. When false, private forge methods emit `FKF010` and are ignored.

```csharp
[ForgeClass(IncludePrivateMethods = true)]
public static partial class PersonForges
{
    private static partial PersonDto ToDto(Person source);  // included
}
```

---

## `[Forge]`

**Namespace:** `FreakyKit.Forge`
**Target:** Method (`static partial` method only)

Marks a method as a forge method and configures its mapping behavior. In `ForgeMode.Explicit`, this attribute is required. In `ForgeMode.Implicit`, it is optional and provides per-method configuration.

### Properties

#### `IncludeFields` (`bool`, default: `false`)

When true, public fields on the source and destination types are included in member discovery alongside properties. When false, fields are excluded and emit `FKF400`.

```csharp
public class Source
{
    public string Name;     // field
    public int Age { get; set; }  // property
}

[ForgeClass]
public static partial class MyForges
{
    // Without IncludeFields: only Age is mapped, Name emits FKF400
    public static partial Dest ToDest(Source source);

    // With IncludeFields: both Name and Age are mapped
    [Forge(IncludeFields = true)]
    public static partial Dest ToDestWithFields(Source source);
}
```

#### `AllowNestedForging` (`bool`, default: `false`)

When true, the generator calls an existing forge method to convert nested types whose names match but whose types differ. When false, a type mismatch where a forge method exists emits `FKF300`.

Also enables collection mapping with different element types via `.Select()`.

```csharp
[ForgeClass]
public static partial class PersonForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [Forge(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
    // Generates: __result.Home = ToAddressDto(source.Home);
    // Generates: __result.Addresses = source.Addresses.Select(x => ToAddressDto(x)).ToList();
}
```

#### `EnumMappingStrategy` (`ForgeEnumMapping`, default: `ForgeEnumMapping.Cast`)

Controls how enum-to-enum mappings are generated when source and destination members share the same name but have different enum types.

- **`ForgeEnumMapping.Cast`** — generates a direct cast: `(DestEnum)source.Value`
- **`ForgeEnumMapping.ByName`** — generates a switch expression that maps each member by name

```csharp
[Forge(EnumMappingStrategy = ForgeEnumMapping.ByName)]
public static partial PersonDto ToDto(Person source);
// Generates a switch expression for enum members
```

#### `AllowFlattening` (`bool`, default: `false`)

When true, the generator attempts to flatten nested source properties into flat destination members. When a destination member has no direct match, the generator tries prefix matching: `AddressCity` → `source.Address.City`.

Only one level of nesting is supported. Flattening only traverses **properties** on intermediate types — fields are not considered for traversal, even when `IncludeFields = true`.

```csharp
[Forge(AllowFlattening = true)]
public static partial PersonDto ToDto(Person source);
// dest.AddressCity = source.Address.City
```

#### `GenerateReverse` (`bool`, default: `false`)

When true, the generator also produces a reverse mapping method that maps from the destination type back to the source type. Must be used with `ReverseName`.

```csharp
[Forge(GenerateReverse = true, ReverseName = "FromDto")]
public static partial PersonDto ToDto(Person source);
// Also generates: Person FromDto(PersonDto source)
```

#### `ReverseName` (`string`, default: `""`)

The name of the reverse mapping method. Required when `GenerateReverse` is true.

---

## `[ForgeIgnore]`

**Namespace:** `FreakyKit.Forge`
**Target:** Property or Field

Excludes a property or field from forge mapping entirely. Members marked with this attribute are skipped — no `FKF100`/`FKF101` warnings are emitted. Emits `FKF102` (info).

```csharp
public class Source
{
    public string Name { get; set; }
    [ForgeIgnore] public string InternalId { get; set; }  // skipped entirely
}
```

---

## `[ForgeMap]`

**Namespace:** `FreakyKit.Forge`
**Target:** Property or Field

Maps a property or field to a differently-named member on the counterpart type. The constructor parameter specifies the target member name.

### Constructor

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | `string` | The name of the counterpart member to map to/from |

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
- In the same forge class

```csharp
[ForgeClass]
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

---

## `ForgeMode` (Enum)

**Namespace:** `FreakyKit.Forge`

Controls which methods in a forge class are treated as forge methods.

| Value | Value | Description |
|-------|-------|-------------|
| `Implicit` | `0` | All properly-shaped static partial methods are forge methods |
| `Explicit` | `1` | Only `[Forge]`-decorated methods are forge methods |

### What Is a "Properly-Shaped" Method?

A method is a valid forge method candidate if it is:

1. `static`
2. `partial` (declaration only — no body)
3. Returns a non-void type (the destination), OR is void with 2 parameters (update mode)
4. Has exactly one parameter (create mode) or exactly two parameters (update mode)
5. Has no type parameters (not generic)

---

## `ForgeEnumMapping` (Enum)

**Namespace:** `FreakyKit.Forge`

Controls how enum-to-enum mappings are generated.

| Value | Value | Description |
|-------|-------|-------------|
| `Cast` | `0` | Direct cast: `(DestEnum)source.Value`. Works when both enums share the same underlying integer values. |
| `ByName` | `1` | Switch expression mapping by member name. Safer when enums may have different underlying values. |
