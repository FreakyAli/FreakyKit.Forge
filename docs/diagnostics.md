# Diagnostics Reference

FreakyKit.Forge emits 46 diagnostics across 7 categories. Error-severity diagnostics block source generation entirely for the affected forge class — no partial output is emitted.

## Mode & Visibility

### FKF001 — Explicit mode activated

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.Mode |
| **Message** | Forge class '{0}' uses explicit method selection mode. Only methods decorated with [ForgeMethod] will be treated as forge methods. |

Emitted on the forge class when `Mode = ForgeMode.Explicit` is set on `[Forge]`. Informational only — reminds you that unmarked methods will be ignored.

### FKF002 — Method ignored in explicit mode

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.Mode |
| **Message** | Method '{0}' in forge class '{1}' is ignored because explicit mode is active. Add [ForgeMethod] to include this method. |

Emitted when a method has the right shape to be a forge method but lacks the `[ForgeMethod]` attribute in a class using `ForgeMode.Explicit`. Add `[ForgeMethod]` to include the method, or remove it if it's intentionally excluded.

```csharp
[Forge(Mode = ForgeMode.Explicit)]
public static partial class MyForges
{
    [ForgeMethod]
    public static partial Dest ToDest(Source s);     // OK

    public static partial Other ToOther(Source s);   // FKF002
}
```

### FKF003 — Forge class not static

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Mode |
| **Message** | Forge class '{0}' is not static. Forge classes must be declared static. |

Emitted when a class has `[Forge]` but is not declared `static`. The generator produces `static` method implementations and requires a static containing class. Add the `static` modifier to the class.

```csharp
// Wrong — FKF003
[Forge]
public partial class MyForges { ... }

// Correct
[Forge]
public static partial class MyForges { ... }
```

### FKF004 — Forge class not partial

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Mode |
| **Message** | Forge class '{0}' is not partial. Forge classes must be declared partial so the generator can add the implementation. |

Emitted when a class has `[Forge]` but is not declared `partial`. The source generator adds a second partial class declaration containing the method bodies; without `partial`, the generated code cannot be merged. Add the `partial` modifier to the class.

```csharp
// Wrong — FKF004
[Forge]
public static class MyForges { ... }

// Correct
[Forge]
public static partial class MyForges { ... }
```

### FKF005 — Forge attribute on non-class type

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Mode |
| **Message** | [Forge] on '{0}' has no effect. Only static partial classes are supported as forge containers. |

Emitted when `[Forge]` is applied to a struct, interface, record struct, or other non-class type. The source generator only processes static partial classes.

```csharp
// Wrong — FKF005
[Forge]
public partial struct MyForges { }

// Correct
[Forge]
public static partial class MyForges { ... }
```

### FKF010 — Private forge method ignored

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.Mode |
| **Message** | Private method '{0}' in forge class '{1}' is ignored. Set ShouldIncludePrivate = true on [Forge] to include private methods. |

Emitted when a private method has the forge method shape but `ShouldIncludePrivate` is false (the default). Set `ShouldIncludePrivate = true` on `[Forge]` to opt in.

### FKF011 — Private visibility enabled

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.Mode |
| **Message** | Forge class '{0}' has ShouldIncludePrivate = true. Private forge methods will be included. |

Informational. Emitted on the class when `ShouldIncludePrivate = true`.

---

## Method Shape

### FKF020 — Forge method declares a body

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Forge method '{0}' must not have an implementation body. Remove the body; the generator will provide it. |

Forge methods must be declaration-only partial methods. The source generator provides the implementation. Remove the body.

```csharp
// Wrong — has a body
public static partial PersonDto ToDto(Person source)
{
    return new PersonDto(); // FKF020
}

// Correct — declaration only
public static partial PersonDto ToDto(Person source);
```

### FKF030 — Forge method name overloaded

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Forge method name '{0}' in class '{1}' is used more than once. Forge method names must be unique within a forge class. |

Two or more forge methods in the same class share the same name. Forge method names must be unique within a forge class. Rename one of the methods.

```csharp
[Forge]
public static partial class MyForges
{
    public static partial DtoA ToDest(SourceA source);  // FKF030
    public static partial DtoB ToDest(SourceB source);  // FKF030 — same name
}
```

### FKF040 — Update mode activated

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Forge method '{0}' uses update mode. The destination object will be modified in place. |

Emitted when a forge method uses the update mapping shape: `void` return type with two parameters (source + destination). The destination object's members will be overwritten in place, with no construction or return.

```csharp
public static partial void Update(Person source, PersonDto existing);
// Generates: existing.Name = source.Name;
```

### FKF041 — Update destination has no settable members

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Update forge method '{0}' destination type '{1}' has no settable members. |

The destination type of an update forge method has no settable properties or fields. There is nothing to update.

A member is considered **non-settable** if it is:

- A property with no setter (get-only)
- A property with an `init`-only setter (`{ get; init; }`)
- A `readonly` field
- A `const` field

If every matching destination member falls into one of these categories, FKF041 is emitted.

### FKF042 — Zero members mapped

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Forge method '{0}' produces no member assignments. Source type '{1}' and destination type '{2}' have no matchable members. |

Emitted when a forge method results in zero assignments — source and destination types share no matching member names. The generated method body will be effectively empty. This is almost always a mistake: check that the types are correct and that member names align (case-insensitive).

Not emitted for collection projection methods (where both parameter and return type are collections) since those produce a LINQ expression rather than member assignments.

### FKF043 — Flattening enabled but no members flattened

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Forge method '{0}' has AllowFlattening = true but no destination members were matched via flattening. |

Emitted when `AllowFlattening = true` is set on a `[ForgeMethod]` but no destination member was matched by decomposing a nested source property. This means the flattening option had no effect — either no destination member names follow the `NavigationPropertyNestedProperty` convention (e.g., `AddressCity` for `Address.City`), or `AllowFlattening` is unnecessary and can be removed.

```csharp
// FKF043: AllowFlattening is on but Dest has no AddressCity-style members
[ForgeMethod(AllowFlattening = true)]
public static partial Dest ToDest(Source source);

// Fix: either remove AllowFlattening or add a flattened member like AddressCity to Dest
```

### FKF050 — Before hook detected

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Before hook '{0}' detected for forge method '{1}'. |

A partial method named `OnBefore{MethodName}` was found in the forge class. It will be called before the mapping assignments.

```csharp
[Forge]
public static partial class MyForges
{
    public static partial PersonDto ToDto(Person source);
    static partial void OnBeforeToDto(Person source);  // FKF050
}
```

### FKF051 — After hook detected

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | After hook '{0}' detected for forge method '{1}'. |

A partial method named `OnAfter{MethodName}` was found in the forge class. It will be called after the mapping assignments, before the return statement.

```csharp
[Forge]
public static partial class MyForges
{
    public static partial PersonDto ToDto(Person source);
    static partial void OnAfterToDto(Person source, PersonDto result);  // FKF051
}
```

---

## Member Discovery

### FKF400 — Field ignored

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MemberDiscovery |
| **Message** | Field '{0}' on type '{1}' is ignored because ShouldIncludeFields is false. Set ShouldIncludeFields = true on [ForgeMethod] to include fields. |

A public field was found on the source or destination type but excluded from member discovery because `ShouldIncludeFields` is false (the default). Set `ShouldIncludeFields = true` on `[ForgeMethod]` to include fields.

### FKF401 — Fields enabled

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MemberDiscovery |
| **Message** | Forge method '{0}' has ShouldIncludeFields = true. Fields will be included in member discovery. |

Informational. Emitted when `ShouldIncludeFields = true` is set on a `[ForgeMethod]` attribute.

---

## Member Matching

### FKF100 — Destination member missing source

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Destination member '{0}.{1}' has no matching member in source type '{2}'. It will be left at its default value. |

A property (or field) exists on the destination type but no member with a matching name was found on the source type. The member will be left at its default value. This is a warning, not an error — generation proceeds.

> **Note:** Read-only destination members (get-only properties, init-only properties, readonly fields, const fields) are excluded from this check because the generator never assigns them. No FKF100 is emitted for members that cannot be written to.

### FKF101 — Source member unused

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Source member '{0}.{1}' has no matching member in destination type '{2}' and will not be mapped. |

A member exists on the source type but the destination type has no corresponding member. The source member is simply not mapped. This is a warning, not an error.

### FKF102 — Member ignored via [ForgeIgnore]

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Member '{0}' on type '{1}' is excluded from mapping via [ForgeIgnore]. |

The member is marked with `[ForgeIgnore]` and will not participate in forge mapping. No FKF100/FKF101 warnings are emitted for ignored members.

> **Note:** This diagnostic is declared and reserved but is not currently emitted by the analyzer or generator. Ignored members are silently excluded. The diagnostic ID is reserved for future verbose output.

```csharp
public class Source
{
    public string Name { get; set; }
    [ForgeIgnore] public string InternalId { get; set; }  // silently skipped
}
```

### FKF103 — Custom member mapping

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Member '{0}' on type '{1}' is mapped to counterpart '{2}' via [ForgeMap]. |

A `[ForgeMap]` attribute is applied to this member, mapping it to a differently-named member on the counterpart type.

> **Note:** This diagnostic is declared and reserved but is not currently emitted by the analyzer or generator. Custom mappings are applied silently. The diagnostic ID is reserved for future verbose output.

```csharp
public class Source { [ForgeMap("Name")] public string FirstName { get; set; } }
// Custom mapping applied silently
```

### FKF104 — ForgeMap target not found

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Member '{0}' on type '{1}' maps to '{2}' via [ForgeMap], but no member named '{2}' was found on the counterpart type. |

The `[ForgeMap]` attribute specifies a target member name that does not exist on the counterpart type. Check the target name for typos.

### FKF105 — Duplicate ForgeMap target

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Multiple members map to the same target key '{0}'. Member '{1}' on type '{2}' conflicts with a previous mapping. |

Two or more members on the same type map to the same counterpart member name. The later mapping overwrites the earlier one.

```csharp
public class Source
{
    [ForgeMap("Name")] public string First { get; set; }
    [ForgeMap("Name")] public string Last { get; set; }  // FKF105 — conflicts
}
```

### FKF106 — Flattened mapping applied

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Destination member '{0}' was mapped via flattening to source path '{1}.{2}'. |

The destination member was matched by flattening a nested source property. For example, `AddressCity` maps to `source.Address.City`.

> **Note:** This diagnostic is emitted by the **generator** only, not by the analyzer.

```csharp
[ForgeMethod(AllowFlattening = true)]
public static partial Dest ToDest(Source source);
// FKF106: AddressCity → Address.City
```

### FKF107 — Read-only destination member skipped

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Destination member '{0}.{1}' matches a source member but is read-only and cannot be assigned. Add a setter or exclude it with [ForgeIgnore]. |

Emitted when a destination member matches a source member by name but has no setter (get-only property or readonly field). The mapping is silently skipped. Either add a setter to the destination member, supply the value through a constructor parameter, or exclude the member with `[ForgeIgnore]` to suppress this diagnostic.

### FKF108 — Write-only source member skipped

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Source member '{0}.{1}' has no getter and cannot be read. It will not be mapped. |

Emitted when a source property has only a setter and no getter. Since the generator reads from the source, a write-only member cannot participate in mapping and is excluded from member discovery.

### FKF109 — Member both ignored and explicitly mapped

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Member '{0}' on type '{1}' has both [ForgeIgnore] and [ForgeMap]. [ForgeIgnore] takes precedence — [ForgeMap] has no effect. |

A member has both `[ForgeIgnore]` and `[ForgeMap]` applied, which is a conflicting configuration. `[ForgeIgnore]` always wins — the member is excluded from mapping and the `[ForgeMap]` rename has no effect. Remove one of the attributes.

```csharp
// Wrong — conflicting attributes (FKF109)
[ForgeIgnore]
[ForgeMap("Name")]
public string FirstName { get; set; }

// Fix A: keep only [ForgeIgnore] to exclude the member
[ForgeIgnore]
public string FirstName { get; set; }

// Fix B: keep only [ForgeMap] to rename the member
[ForgeMap("Name")]
public string FirstName { get; set; }
```

---

## Strict Mapping (Drift Detection)

### FKF110 — Strict: destination member missing source

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Destination member '{0}.{1}' has no matching member in source type '{2}'. StrictMapping is enabled — all destination members must be mapped. |

Emitted instead of FKF100 when `StrictMapping = true` on the `[ForgeMethod]`. Every destination member must have a corresponding source member. This catches mapping drift when destination types gain new members that the source doesn't satisfy.

To fix:
- Add a matching member to the source type
- Exclude the destination member with `[ForgeIgnore]`
- Remove `StrictMapping = true` to downgrade to a warning

```csharp
[ForgeMethod(StrictMapping = true)]
public static partial Dest ToDest(Source source);
// FKF110 if Dest has a member not present in Source
```

### FKF111 — Strict: source member unused

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Source member '{0}.{1}' has no matching member in destination type '{2}'. StrictMapping is enabled — all source members must be consumed or explicitly ignored. |

Emitted instead of FKF101 when `StrictMapping = true` on the `[ForgeMethod]`. Every source member must have a corresponding destination member or be excluded with `[ForgeIgnore]`. This catches mapping drift when source types gain new members that aren't being mapped.

To fix:
- Add a matching member to the destination type
- Exclude the source member with `[ForgeIgnore]`
- Remove `StrictMapping = true` to downgrade to a warning

```csharp
[ForgeMethod(StrictMapping = true)]
public static partial Dest ToDest(Source source);
// FKF111 if Source has a member not present in Dest
```

### FKF112 — ForgeMap target is the member's own name

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MemberMatching |
| **Message** | Member '{0}' on type '{1}' has [ForgeMap("{2}")] which maps to its own name. [ForgeMap] has no effect — remove it. |

Emitted when a `[ForgeMap]` attribute specifies the same name as the member it's applied to. The rename is a no-op — the member would have been matched by name convention anyway. This is almost always a copy-paste mistake.

```csharp
// Wrong — FKF112: Name already maps to Name by convention
[ForgeMap("Name")]
public string Name { get; set; }

// Fix: remove [ForgeMap] or correct the target name
[ForgeMap("FullName")]
public string Name { get; set; }
```

---

## Type Safety

### FKF200 — Incompatible member types

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': source type '{1}' is incompatible with destination type '{2}'. No forge conversion is available. |

A source and destination member share a name but have different types, and no forge method, converter, or automatic conversion exists to bridge them. This is an error — generation is blocked.

To fix:
- Provide a forge method that converts between the two types and set `AllowNestedForging = true`
- Add a `[ForgeConverter]` method that bridges the types
- Change one of the types to match
- Exclude the mismatched member with `[ForgeIgnore]`

```csharp
public class Source { public int Value { get; set; } }
public class Dest   { public string Value { get; set; } }  // int vs string — FKF200
```

### FKF201 — Nullable value type to non-nullable mapping

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': mapping nullable value type '{1}' to non-nullable '{2}' will use .Value which may throw at runtime. |

A `Nullable<T>` value type is being mapped to its non-nullable counterpart `T` using `.Value`. This works but throws `InvalidOperationException` if the source value is null at runtime.

```csharp
public class Source { public int? Age { get; set; } }
public class Dest   { public int Age { get; set; } }
// Generates: __result.Age = source.Age.Value;  // FKF201
```

### FKF202 — Nullable mapping applied

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': nullable mapping applied from '{1}' to '{2}'. |

The source and destination types differ only in nullability. The generator handles this automatically (direct assignment for `T` → `Nullable<T>`, `.Value` for `Nullable<T>` → `T`).

### FKF203 — Lossy implicit conversion

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': implicit conversion from '{1}' to '{2}' may lose precision or data. |

A source and destination member differ in type, but an implicit conversion exists that may lose precision or data. For example, `float` → `double` (loss of precision due to representation differences), or `long` → `int` (overflow potential).

The generator emits this warning to flag lossy conversions. If you're confident the loss is acceptable, you can suppress it or use an explicit `[ForgeConverter]` to document the intentional conversion. Safe implicit widening conversions (e.g., `byte` → `int`) do not emit this warning.

```csharp
public class Source { public float Value { get; set; } }
public class Dest   { public double Value { get; set; } }
// Generates: __result.Value = source.Value;  // FKF203: float → double may lose precision
```

### FKF210 — Enum cast mapping

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': enum cast from '{1}' to '{2}'. |

The source and destination members are different enum types. A direct cast (`(DestEnum)source.Value`) is generated. This is the default behavior.

### FKF211 — Enum name-based mapping

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': enum name-based mapping from '{1}' to '{2}'. |

The source and destination members are different enum types. A switch expression mapping by member name is generated. Enabled with `MappingStrategy = ForgeMapping.ByName`.

### FKF212 — Enum member missing in destination

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Enum member '{0}' in source type '{1}' has no corresponding member in destination type '{2}'. |

A member of the source enum type has no matching member (by name) in the destination enum type. The generated switch expression will throw for this value at runtime.

```csharp
public enum SourceStatus { Active, Inactive, Pending }
public enum DestStatus { Active, Inactive }  // FKF212: Pending is missing
```

### FKF230 — Enum ↔ string mapping applied

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': enum ↔ string mapping from '{1}' to '{2}'. |

One member is an enum and the other is a string. The generator automatically converts between them:
- Enum → string: `source.Status.ToString()`
- String → enum: `Enum.Parse<StatusEnum>(source.Status)` (throws if invalid)
- With fallback: `Enum.TryParse<StatusEnum>(source.Status, out var result) ? result : fallback` (when `[ForgeMap]` provides a `DefaultValue`)

```csharp
public class Order { public Status Status { get; set; } }
public class OrderDto { public string Status { get; set; } }
// FKF230: Enum → string mapping applied (uses ToString())

public class Order { public string Status { get; set; } }
public class OrderDto
{
    [ForgeMap("Status", DefaultValue = Status.Unknown)]
    public Status Status { get; set; }
}
// FKF230: String → enum mapping applied (uses TryParse with fallback)
```

### FKF220 — Type converter used

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}': type converter '{1}' was used to convert from '{2}' to '{3}'. |

A method marked with `[ForgeConverter]` was used to bridge the type mismatch for this member.

```csharp
[ForgeConverter]
public static string ConvertDateTime(DateTime value) => value.ToString("yyyy-MM-dd");
// FKF220: Birthday converter used from DateTime to string
```

### FKF221 — Invalid converter signature

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Method '{0}' is marked with [ForgeConverter] but has an invalid signature: {1}. The converter will be ignored. |

A method marked with `[ForgeConverter]` does not meet the requirements and will be silently ignored by the generator. This can cause an unexpected FKF200 error when users assume the converter is registered.

A valid converter must be: `static`, non-void, non-generic, and take exactly one parameter.

```csharp
// Wrong — two parameters (FKF221)
[ForgeConverter]
public static string ConvertDate(DateTime value, string format) => value.ToString(format);

// Wrong — void return (FKF221)
[ForgeConverter]
public static void ConvertDate(DateTime value) { }

// Correct
[ForgeConverter]
public static string ConvertDate(DateTime value) => value.ToString("yyyy-MM-dd");
```

### FKF222 — Duplicate converter for same type pair

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Forge class '{0}' has multiple [ForgeConverter] methods that convert from '{1}' to '{2}'. Only one converter per type pair is allowed; duplicates will be ignored. |

Emitted when two or more valid `[ForgeConverter]` methods in the same forge class handle the same source-to-destination type pair. The generator picks one and ignores the rest. Remove the duplicates and keep only the converter you intend to use.

```csharp
// FKF222: both converters handle DateTime → string
[ForgeConverter]
public static string ConvertDate1(DateTime value) => value.ToString("yyyy-MM-dd");

[ForgeConverter]
public static string ConvertDate2(DateTime value) => value.ToString("dd/MM/yyyy");
```

---

## Nested / Collections

### FKF300 — Nested forging disabled

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.Nested |
| **Message** | Member '{0}': source type '{1}' differs from destination type '{2}'. A forge method exists for this conversion but AllowNestedForging is false. |

A member pair has different types and a forge method exists that could convert between them, but `AllowNestedForging` is false on the current method. Set `AllowNestedForging = true` on `[ForgeMethod]` to enable it. Without it, the member is skipped.

```csharp
[Forge]
public static partial class MyForges
{
    public static partial AddressDto ToAddressDto(Address source);

    // FKF300 — forge method exists but AllowNestedForging is false
    public static partial PersonDto ToDto(Person source);

    // Fix: add [ForgeMethod(AllowNestedForging = true)]
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDtoFixed(Person source);  // OK
}
```

### FKF310 — Collection mapping applied

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.Nested |
| **Message** | Member '{0}': collection mapping from '{1}' to '{2}'. |

The source and destination members are both collection types. The generator maps element-by-element using LINQ (`.ToList()`, `.ToArray()`, or `.Select(x => ...).ToList()` for different element types).

### FKF311 — Same-type collection reference-shared

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.Nested |
| **Message** | Member '{0}' is reference-shared with the source collection because ShareReference is true. Mutations to the destination will affect the source. |

When `ShareReference = true` is set (method-level via `[ForgeMethod]` or per-member via `[ForgeMap]`), Forge emits direct reference assignment for same-type mutable collections rather than the deep-copy default. This diagnostic fires per affected member as an audit trail: the source and destination will share the same collection instance, so mutations leak across.

```csharp
[ForgeMethod(ShareReference = true)]
public static partial PersonDto ToDto(Person source);
// __result.Tags = source.Tags;    ← reference share, FKF311 fires for Tags
```

If you didn't intend reference sharing, drop the `ShareReference = true` flag and Forge will deep-copy by default. See [Reference semantics for same-type collections](attributes.md#reference-semantics-for-same-type-collections) for the full reference.

### FKF312 — Same-type reference member shared

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.Nested |
| **Message** | Member '{0}' is the same type '{1}' on both source and destination and is shared by reference. Mutations to the destination will affect the source. Use a distinct DTO type with AllowNestedForging + a forge method to deep-copy. |

When a same-type **custom class** member appears on both source and destination (e.g. `Address Home { get; set; }` on both sides), Forge cannot auto-clone the value — it only generates property-by-property mapping for explicit forge methods. The destination receives the same instance the source had. Mutations leak across.

This diagnostic always fires for affected members; it cannot be silenced via `ShareReference` (which only affects collections). To get a deep copy, change the destination property to a distinct DTO type and use `AllowNestedForging` with a forge method:

```csharp
// Wrong — shared reference, FKF312:
public class Source { public Address Home { get; set; } }
public class Dest   { public Address Home { get; set; } }   // same type → ref share

// Right — distinct DTO type + forge method:
public class Source { public Address Home { get; set; } }
public class Dest   { public AddressDto Home { get; set; } }

[Forge]
public static partial class MyForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial Dest ToDest(Source source);
}
```

### FKF313 — Conflicting ShareReference between source and destination

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.Nested |
| **Message** | Member '{0}': source-side [ForgeMap] sets ShareReference={1} but destination-side sets ShareReference={2}. The destination-side value ({2}) is used. |

Both the source-side and destination-side `[ForgeMap]` explicitly set `ShareReference` with different values. The destination-side wins (it bears the consequences of the mapping decision), but the warning surfaces the conflict so you can resolve it.

```csharp
public class Source
{
    [ForgeMap("Tags", ShareReference = true)]    // entity says "share is fine"
    public List<string> Tags { get; set; }
}

public class Dest
{
    [ForgeMap("Tags", ShareReference = false)]   // DTO says "I want my own copy" — wins
    public List<string> Tags { get; set; }
}
// FKF313 fires. Forge generates the deep-copy form.
```

To silence the warning, remove one of the conflicting attributes.

---

## Construction

### FKF500 — Constructor ambiguity

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Construction |
| **Message** | Type '{0}' has multiple constructors that are equally viable for forge construction. |

The destination type has multiple public constructors where all parameters can be satisfied from source members. The generator cannot choose between them. Add a parameterless constructor or reduce to a single viable constructor.

### FKF501 — Missing constructor parameter

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Construction |
| **Message** | Constructor parameter '{0}' on type '{1}' has no matching source member in '{2}'. |

A required constructor parameter on the destination type has no matching source member (by name and type, case-insensitive). The constructor cannot be satisfied.

### FKF502 — No viable constructor

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Construction |
| **Message** | Type '{0}' has no viable constructor for forge construction. Provide a parameterless constructor or a constructor whose parameters can all be satisfied from source type '{1}'. |

No public constructor on the destination type can be used. Either there are no public constructors at all, or all constructors have parameters that can't be matched from the source type.

### FKF503 — Destination type not instantiable

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Construction |
| **Message** | Destination type '{0}' cannot be constructed because it is {1}. Map to a concrete type instead. |

Emitted when the destination type cannot be instantiated with `new`: abstract classes, interfaces, and static classes all trigger this error. Use a concrete, non-static class as the mapping destination.

```csharp
// Wrong — FKF503: abstract class cannot be constructed
public abstract class Dest { ... }
public static partial Dest ToDest(Source source);

// Wrong — FKF503: interface cannot be constructed
public static partial IDest ToDest(Source source);

// Wrong — FKF503: static class cannot be constructed
public static class Dest { ... }
public static partial Dest ToDest(Source source);

// Correct
public class ConcreteDest : IDest { ... }
public static partial ConcreteDest ToDest(Source source);
```

### FKF504 — Expression generation incompatible with update method

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Forge method '{0}' has GenerateExpression = true but is an update method (void return, two parameters). Expressions can only be generated for create methods. |

`Expression<Func<TSource, TDest>>` models a pure function from source to destination. Update methods
modify state in place and have no return value, so no expression can be generated. Drop the flag,
or split the update method into a create method.

```csharp
// Wrong
[ForgeMethod(GenerateExpression = true)]
public static partial void Update(Source source, Dest existing);  // FKF504

// Correct: drop the flag, or convert to a create method
[ForgeMethod(GenerateExpression = true)]
public static partial Dest ToDest(Source source);
```

See [projections.md](projections.md) for the full projection-expression reference.

### FKF505 — Hooks ignored in generated expression

| | |
|--|--|
| **Severity** | Warning |
| **Category** | FreakyKit.Forge.MethodShape |
| **Message** | Forge method '{0}' has GenerateExpression = true but defines a before/after hook; the hook will be invoked from the imperative method but not from the generated expression property. |

Expression trees can't invoke arbitrary side-effectful methods. Before/after hooks remain wired
into the imperative partial method body but are omitted from the generated expression property.
If the hook performs state changes you need at query time, those changes won't happen when the
expression is used in `IQueryable.Select`.

### FKF506 — Member excluded from generated expression

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.TypeSafety |
| **Message** | Member '{0}' was excluded from the generated expression property: {1}. The imperative method still maps this member normally. |

Some mapping cases have no equivalent encoding inside an expression tree:

- **Custom converter** — user-defined static methods aren't translatable to SQL.
- **`IgnoreIfNull`** — conditional skipping doesn't exist in expression trees; every binding evaluates.
- **Non-translatable collection materializer** — EF translates only `.ToList()` and `.ToArray()`. Destinations like `HashSet<T>`, `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`, `ReadOnlyCollection<T>` are excluded.

The imperative method continues to map the member normally. The expression property emits with the
remaining translatable members.

### FKF507 — Circular nested forge in expression property

| | |
|--|--|
| **Severity** | Error |
| **Category** | FreakyKit.Forge.Nested |
| **Message** | Expression property for '{0}' cannot be generated because the nested forge call chain contains a cycle: {1}. Inlining would produce infinite source. |

Expression properties inline nested forge methods because EF cannot translate `Expression.Invoke`.
When two forge methods (or a chain) mutually reference each other via `AllowNestedForging = true`,
the inliner would recurse forever. Break the cycle by removing one direction, by using a converter,
or by dropping `GenerateExpression` on the involved methods.

```csharp
// Wrong: Address.Parent loops back to ToAddressDto
public class Address { public Address Parent { get; set; } }
[ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
public static partial AddressDto ToAddressDto(Address source);  // FKF507
```

### FKF508 — Deep nested-forge inlining in expression property

| | |
|--|--|
| **Severity** | Info |
| **Category** | FreakyKit.Forge.Nested |
| **Message** | Expression property for '{0}' inlines nested forge methods {1} levels deep. The generated source size grows multiplicatively; consider whether flattening or a converter would be cleaner. |

Each level of nested-forge inlining substitutes the full body of the nested expression into the
outer one. Deep chains produce large generated source files. This diagnostic fires when depth
exceeds five to surface the cost. No action is required — the expression still emits and runs
correctly. Consider flattening with `AllowFlattening = true` for shallow-but-wide member access if
the generated source is becoming unwieldy.
