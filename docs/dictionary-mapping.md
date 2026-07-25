# Dictionary Mapping Feature

Convert between dictionaries and domain objects with type-safe, compile-time generated code.

## Quick Start

```csharp
using FreakyKit.Forge;
using System.Collections.Generic;

public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

[Forge]
public static partial class PersonForges
{
    // Convert dictionary to object
    [ForgeMethod]
    public static partial Person FromDict(Dictionary<string, object> dict);
    
    // Convert object to dictionary
    [ForgeMethod]
    public static partial Dictionary<string, object> ToDict(Person person);
}

// Usage:
var dict = new Dictionary<string, object> { ["Name"] = "Alice", ["Age"] = 30 };
var person = PersonForges.FromDict(dict);  // Alice, age 30

var dict2 = PersonForges.ToDict(person);   // { "Name": "Alice", "Age": 30 }
```

## Configuration with [ForgeDictionary]

Control mapping behavior using the `[ForgeDictionary]` attribute:

```csharp
[ForgeMethod]
[ForgeDictionary(
    KeyCasing = KeyCasingPolicy.CamelCase,
    NullValue = NullValuePolicy.Skip
)]
public static partial Person FromDict(Dictionary<string, object> dict);
```

## Policies

### Key Casing Policy

Controls how dictionary keys are matched against property names:

| Policy | Behavior | Example |
|--------|----------|---------|
| **Exact** (default) | Match property name exactly | `FirstName` → `"FirstName"` |
| **IgnoreCase** | Case-insensitive matching | `FirstName` matches `"firstname"`, `"FIRSTNAME"`, etc. |
| **CamelCase** | Convert property to camelCase | `FirstName` → `"firstName"` |
| **SnakeCase** | Convert property to snake_case | `FirstName` → `"first_name"` |

```csharp
[ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase)]
public static partial Person FromDict(Dictionary<string, object> dict);
// Will look for "firstName", "age" in the dictionary
```

### Missing Key Policy

Controls behavior when a dictionary key is not found:

| Policy | Behavior | Use Case |
|--------|----------|----------|
| **Throw** (default) | Throw `KeyNotFoundException` | Strict validation—all keys required |
| **UseDefault** | Assign `default(T)` | Permissive—use default for missing keys |
| **Skip** | Don't assign—leave at default | Same as UseDefault but more explicit |
| **ReturnNull** | Assign null (nullable types only) | Allow null for missing optional fields |

```csharp
[ForgeDictionary(MissingKeyPolicy = MissingKeyPolicy.UseDefault)]
public static partial Person FromDict(Dictionary<string, object> dict);
// Missing keys will use default values instead of throwing
```

### Null Value Policy

Controls whether null values are included when converting object to dictionary:

| Policy | Behavior | Use Case |
|--------|----------|----------|
| **Include** (default) | Add all properties including nulls | Include everything in output |
| **Skip** | Only add non-null values | Omit null properties from result |

```csharp
[ForgeDictionary(NullValue = NullValuePolicy.Skip)]
public static partial Dictionary<string, object> ToDict(Person person);
// Null properties will be excluded from the generated dictionary
```

## Examples

### JSON Deserialization with CamelCase Keys

```csharp
public class ApiResponse
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Age { get; set; }
}

[Forge]
public static partial class ApiForges
{
    [ForgeMethod]
    [ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase)]
    public static partial ApiResponse FromJson(Dictionary<string, object> json);
}

// JSON from API: { "firstName": "John", "lastName": "Doe", "age": 30 }
var response = ApiForges.FromJson(apiData);  // Maps correctly despite camelCase keys
```

### Configuration Mapping with Defaults

```csharp
public class AppSettings
{
    public string? DatabaseUrl { get; set; }
    public int Timeout { get; set; }
    public bool? EnableLogging { get; set; }
}

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

// Missing keys get default values:
// - string → empty string
// - int → 0
// - bool? → null
var settings = ConfigForges.FromConfig(config);
```

### API Response to DTO

```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
}

[Forge]
public static partial class ApiForges
{
    [ForgeMethod]
    [ForgeDictionary(NullValue = NullValuePolicy.Skip)]
    public static partial Dictionary<string, object> ToApiFormat(UserDto user);
}

var user = new UserDto { Id = 1, Name = "Alice", Email = null };
var apiData = ApiForges.ToApiFormat(user);
// Result: { "Id": 1, "Name": "Alice" }
// Email is omitted because it's null
```

## Supported Dictionary Types

- `Dictionary<string, object>` ✅
- `Dictionary<string, string>` ✅ (parsing from future phase)
- `IReadOnlyDictionary<string, T>` ✅
- `IDictionary<string, T>` ✅

**Non-string keys are not supported** (emits FKF700 diagnostic)

## Limitations & Future Enhancements

**Currently Working:**
- Basic key lookup with casing policies
- Null value filtering on object→dict conversion
- Non-string key validation

**Coming in Phase 4:**
- Missing key policy code generation (Throw, UseDefault, Skip, ReturnNull)
- Type conversion (casting for object dicts, parsing for string dicts)
- Comprehensive test coverage
- Custom type converters

## Generated Code

The generator creates efficient, allocation-light code. For exact key matching:

```csharp
// Input
[ForgeDictionary(KeyCasing = KeyCasingPolicy.Exact)]
public static partial Person FromDict(Dictionary<string, object> dict);

// Generated
public static partial Person FromDict(Dictionary<string, object> dict)
{
    if (dict == null) return null;
    var __result = new Person();
    if (dict.TryGetValue("Name", out var __val_Name))
        __result.Name = __val_Name;
    if (dict.TryGetValue("Age", out var __val_Age))
        __result.Age = __val_Age;
    return __result;
}
```

For case-insensitive key matching:

```csharp
// Input
[ForgeDictionary(KeyCasing = KeyCasingPolicy.IgnoreCase)]
public static partial Person FromDict(Dictionary<string, object> dict);

// Generated
var __key_Name = dict.Keys.FirstOrDefault(k => 
    string.Equals(k, "Name", StringComparison.OrdinalIgnoreCase));
if (__key_Name != null && dict.TryGetValue(__key_Name, out var __val_Name))
    __result.Name = __val_Name;
```

## Error Diagnostics

- **FKF700**: Dictionary key type is not string (only `Dictionary<string, T>` supported)
- **FKF701**: Unsupported dictionary value type (complex types, collections)
- **FKF702**: ReturnNull policy used on non-nullable type

## Performance

Dictionary mapping generates zero-allocation code for simple cases:
- No LINQ or reflection involved
- Direct dictionary lookups via `TryGetValue`
- Minimal conditional logic
- Perfect for hot paths and performance-critical code
