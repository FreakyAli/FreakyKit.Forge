# Diagnostics Reference

FreakyKit.Forge emits 31 diagnostics across 7 categories. Error-severity diagnostics block source generation entirely for the affected forge class — no partial output is emitted.

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
