# Common Patterns & Cookbook

Copy-paste recipes for the most frequent Forge use cases. Each pattern is self-contained — paste it into your project and adapt the type names.

> **Migrating from another library?** See the dedicated migration guides: [AutoMapper](migrate-from-automapper.md) · [Mapperly](migrate-from-mapperly.md) · [Mapster](migrate-from-mapster.md) · [Facet](migrate-from-facet.md)

---

## 1. Basic Property Mapping

The simplest case: two types with matching property names.

```csharp
public class Person    { public string Name { get; set; } = ""; public int Age { get; set; } }
public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }

[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}

// Usage:
var dto = PersonForges.ToDto(person);
var dto2 = person.ToDto(); // extension method (on by default)
```

---

## 2. Rename Properties with `[ForgeMap]`

When source and destination have different property names.

```csharp
public class Employee  { public string FullName { get; set; } = ""; }
public class EmployeeDto
{
    [ForgeMap("FullName")]
    public string DisplayName { get; set; } = "";
}

[Forge]
public static partial class EmployeeForges
{
    public static partial EmployeeDto ToDto(Employee source);
}
// Generates: __result.DisplayName = source.FullName;
```

---

## 3. Nested Object Mapping

Map a child object using a separate forge method.

```csharp
public class Address    { public string City { get; set; } = ""; public string Zip { get; set; } = ""; }
public class AddressDto { public string City { get; set; } = ""; public string Zip { get; set; } = ""; }

public class Customer    { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
public class CustomerDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

[Forge]
public static partial class CustomerForges
{
    public static partial AddressDto ToAddressDto(Address source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial CustomerDto ToDto(Customer source);
}
// Generates: __result.Home = source.Home != null ? ToAddressDto(source.Home) : null;
```

---

## 4. Flatten Nested Properties

Map `source.Address.City` to `dest.AddressCity` without writing a nested forge method.

```csharp
public class Address { public string City { get; set; } = ""; public string Zip { get; set; } = ""; }
public class Source  { public Address Address { get; set; } = new(); }
public class FlatDto { public string AddressCity { get; set; } = ""; public string AddressZip { get; set; } = ""; }

[Forge]
public static partial class FlatForges
{
    [ForgeMethod(AllowFlattening = true)]
    public static partial FlatDto ToDto(Source source);
}
// Generates:
//   __result.AddressCity = source.Address?.City;
//   __result.AddressZip = source.Address?.Zip;
```

---

## 5. Update an Existing Object (Patch Pattern)

Modify an existing object in place — useful for PATCH APIs.

```csharp
public class UpdateRequest { public string? Name { get; set; } public string? Email { get; set; } }
public class User          { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

[Forge]
public static partial class UserForges
{
    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
    public static partial void ApplyUpdate(UpdateRequest source, User existing);
}
// Generates:
//   if (source.Name != null) existing.Name = source.Name;
//   if (source.Email != null) existing.Email = source.Email;
```

---

## 6. Collection Mapping

Map a list of entities to a list of DTOs.

```csharp
[Forge]
public static partial class OrderForges
{
    public static partial OrderDto ToDto(Order source);

    // Collection projection — delegates to ToDto per element
    public static partial List<OrderDto>? ToDtos(List<Order>? source);
}
// Generates: return source != null ? source.Select(x => ToDto(x)).ToList() : null;
```

---

## 7. Enum Mapping (Cast vs ByName)

```csharp
public enum SourceStatus { Active, Inactive, Pending }
public enum DestStatus   { Active, Inactive, Pending }

public class Source { public SourceStatus Status { get; set; } }
public class Dest   { public DestStatus Status { get; set; } }

[Forge]
public static partial class StatusForges
{
    // Default (Cast) — fastest, works when underlying values match
    [ForgeMethod]
    public static partial Dest ToDto(Source source);
    // Generates: __result.Status = (DestStatus)source.Status;

    // ByName — safer when values might differ
    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
    public static partial Dest ToDtoSafe(Source source);
    // Generates a switch expression mapping each member by name
}
```

---

## 8. Enum-String Conversion

```csharp
public class Order    { public OrderStatus Status { get; set; } }
public class OrderDto { public string Status { get; set; } = ""; }

[Forge]
public static partial class OrderForges
{
    public static partial OrderDto ToDto(Order source);
    // Generates: __result.Status = source.Status.ToString();

    public static partial Order FromDto(OrderDto source);
    // Generates: __result.Status = Enum.Parse<OrderStatus>(source.Status);
}
```

With a fallback for invalid strings:

```csharp
public class OrderDto
{
    [ForgeMap("Status", DefaultValue = OrderStatus.Unknown)]
    public OrderStatus Status { get; set; }
}
// Generates: Enum.TryParse<OrderStatus>(source.Status, out var __parsed) ? __parsed : OrderStatus.Unknown;
```

---

## 9. Custom Type Converter

Bridge types that have no automatic conversion.

```csharp
public class Source { public DateTime Birthday { get; set; } }
public class Dest   { public string Birthday { get; set; } = ""; }

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);

    [ForgeConverter]
    public static string DateToString(DateTime value) => value.ToString("yyyy-MM-dd");
}
// Generates: __result.Birthday = DateToString(source.Birthday);
```

---

## 10. EF Core Projection Expression

Generate an expression tree for `IQueryable.Select()` — the mapping runs as SQL.

```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonDto ToDto(Person source);
}

// Use in EF Core:
var dtos = await dbContext.People
    .Where(p => p.IsActive)
    .Select(PersonForges.ToDtoExpression)
    .ToListAsync();
```

---

## 11. Before/After Hooks

Run custom logic before or after the generated mapping.

```csharp
[Forge]
public static partial class AuditForges
{
    public static partial AuditDto ToDto(AuditEntry source);

    static partial void OnBeforeToDto(AuditEntry source)
    {
        // Validate or log before mapping
    }

    static partial void OnAfterToDto(AuditEntry source, AuditDto result)
    {
        result.MappedAt = DateTime.UtcNow;
    }
}
```

---

## 12. Cross-Class Method Sharing with `[ForgeUses]`

Reuse forge methods from other classes without duplicating them.

```csharp
[Forge]
public static partial class AddressForges
{
    public static partial AddressDto ToAddressDto(Address source);
}

[Forge]
public static partial class CompanyForges
{
    public static partial CompanyDto ToCompanyDto(Company source);
}

[Forge]
[ForgeUses(typeof(AddressForges), typeof(CompanyForges))]
public static partial class PersonForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
    // Discovers ToAddressDto from AddressForges
    // Discovers ToCompanyDto from CompanyForges
}
```

---

## 13. Strict Mapping (Drift Detection)

Fail the build when source or destination types add new properties that aren't mapped.

```csharp
[Forge]
public static partial class StrictForges
{
    [ForgeMethod(StrictMapping = true)]
    public static partial PersonDto ToDto(Person source);
    // FKF110 error if Dest has unmapped members
    // FKF111 error if Source has unused members

    // Use [ForgeIgnore] to explicitly exclude intentionally unmapped members
}
```

---

## 14. Conditional Property Mapping

Only map a property when a condition is met.

```csharp
public class Person
{
    public decimal? Salary { get; set; }
    public bool IsManager { get; set; }
}

public class Dest
{
    [ForgeMap("Salary", Condition = nameof(PersonForges.CanUpdateSalary))]
    public decimal? Salary { get; set; }
}

[Forge]
public static partial class PersonForges
{
    [ForgeMethod]
    public static partial Dest ToDto(Person source);

    internal static bool CanUpdateSalary(Person source) => source.IsManager;
}
// Generates: if (CanUpdateSalary(source)) __result.Salary = source.Salary;
```

---

## 15. Dictionary to/from Object

```csharp
[Forge]
public static partial class ConfigForges
{
    // Object to dictionary
    [ForgeMethod]
    [ForgeDictionary(NullValue = NullValuePolicy.Skip)]
    public static partial Dictionary<string, object> ToDict(AppSettings settings);

    // Dictionary to object with camelCase keys and defaults for missing
    [ForgeMethod]
    [ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase, MissingKey = MissingKeyPolicy.UseDefault)]
    public static partial AppSettings FromDict(Dictionary<string, object> dict);
}
```

---

## 16. Nullable with Default Value

Safely map `int?` to `int` with a fallback.

```csharp
public class Source
{
    [ForgeMap("Age", DefaultValue = 0)]
    public int? Age { get; set; }
}

public class Dest { public int Age { get; set; } }
// Generates: __result.Age = source.Age ?? 0;
```

---

## 17. Constructor Mapping

Map to an immutable type via its constructor.

```csharp
public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
public class Dest
{
    public string Name { get; }
    public int Age { get; }
    public Dest(string name, int age) { Name = name; Age = age; }
}

[Forge]
public static partial class ImmutableForges
{
    public static partial Dest ToDto(Source source);
}
// Generates: var __result = new Dest(source.Name, source.Age);
```

With renamed constructor parameters:

```csharp
public class Dest
{
    public string FullName { get; }
    public Dest([ForgeMap("Name")] string fullName) { FullName = fullName; }
}
// Generates: var __result = new Dest(source.Name);
```

---

## 18. Init-Only / Record Types

```csharp
public record PersonRecord(string Name, int Age);

public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }

[Forge]
public static partial class RecordForges
{
    public static partial PersonRecord ToRecord(Source source);
}
// Generates via constructor: var __result = new PersonRecord(source.Name, source.Age);
```

---

## 19. Explicit Mode (Opt-In Methods)

Only forge methods you explicitly mark.

```csharp
[Forge(Mode = ForgeMode.Explicit)]
public static partial class SelectiveForges
{
    [ForgeMethod]
    public static partial PersonDto ToDto(Person source);      // forged

    public static partial PersonSummary ToSummary(Person source); // ignored (FKF002 warning)
}
```

---

## 20. Reference Sharing for Performance

Skip deep-copy on hot paths where you've measured the allocation cost.

```csharp
[Forge]
public static partial class HotPathForges
{
    // Method-level: share all collections
    [ForgeMethod(ShareReference = ForgePolicy.True)]
    public static partial PersonDto ToDto(Person source);

    // Per-member override: share most but deep-copy History
    // On Dest type:
    // [ForgeMap("History", ShareReference = ForgePolicy.False)]
    // public List<string> History { get; set; }
}
```

## 21. Polymorphic Mapping (Derived Type Dispatch)

Generate a switch expression that dispatches to the correct forge method based on runtime type. Useful for EF Core TPH inheritance.

```csharp
public class Animal { public string Name { get; set; } }
public class Dog : Animal { public string Breed { get; set; } }
public class Cat : Animal { public bool Indoor { get; set; } }

public class AnimalDto { public string Name { get; set; } }
public class DogDto : AnimalDto { public string Breed { get; set; } }
public class CatDto : AnimalDto { public bool Indoor { get; set; } }

[Forge]
public static partial class AnimalForges
{
    public static partial DogDto MapDog(Dog source);
    public static partial CatDto MapCat(Cat source);

    public static partial AnimalDto MapBase(Animal source);

    // Pure dispatch — no property mapping on this method
    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
    public static partial AnimalDto MapAny(Animal source);

    // Optional base fallback — add as explicit arm
    // [ForgePolymorphic(typeof(Animal), nameof(MapBase))]
}
```

## 22. Mapping Profiles / Inheritance with `[ForgeIncludes]`

Reuse base-type mappings across forge classes without redeclaring them. When `DerivedSource : BaseSource` and `DerivedDto : BaseDto`, the base method's property assignments are inlined into the derived method.

```csharp
public class BaseEntity { public int Id { get; set; } public DateTime CreatedAt { get; set; } }
public class BaseDto { public int Id { get; set; } public DateTime CreatedAt { get; set; } }

public class Person : BaseEntity { public string Name { get; set; } }
public class PersonDto : BaseDto { public string Name { get; set; } }

[Forge]
public static partial class BaseForges
{
    public static partial BaseDto ToBaseDto(BaseEntity source);
}

[Forge]
[ForgeIncludes(typeof(BaseForges))]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
    // Inherits Id and CreatedAt mappings from BaseForges
    // Adds Name mapping locally
}
```

Combine with `[ForgeUses]` for both inheritance and cross-class nested forging:

```csharp
[Forge]
[ForgeIncludes(typeof(BaseForges))]   // Inherit base assignments
[ForgeUses(typeof(AddressForges))]     // Discover nested forge methods
public static partial class PersonForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
}
```
