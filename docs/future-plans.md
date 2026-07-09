# Future Plans

Features and fixes under consideration for future versions of FreakyKit.Forge. Each section includes enough detail to serve as a starting point for implementation.

**Pending Removal (marked for deprecation):**
- FKF507 diagnostic descriptor — kept for backward compatibility in v1.x, scheduled for removal in v2.0

## Priority Matrix

Each feature is prioritized using an **Impact × Effort** matrix:

- **P1 — Do first.** Critical bugs or high-impact features with low-to-medium implementation effort. Correctness, security, and infrastructure issues belong here.
- **P2 — Do next.** Either high-impact features requiring significant effort, or moderate-impact fixes/features with low effort.
- **P3 — Backlog.** Valuable features with open design questions, high complexity, or niche use cases. Need more design work before implementation.

| # | Feature | Priority | Impact | Effort | Notes |
|---|---------|----------|--------|--------|-------|
| 1 | Expression assignment mutability | P2 | Medium | Low | Refactor to immutable pattern |
| 2 | Computed properties | P2 | High | Medium | `[ForgeComputed]` for derived members |
| 3 | Conditional/predicate mapping | P2 | High | Medium | `IgnoreIfDefault` + custom predicates |
| 4 | Multi-level deep flattening | P2 | Medium | Medium | Support 2+ levels of nesting |
| 5 | Cross-class nested forge | P2 | High | Medium-High | Discover methods in other classes |
| 6 | Tri-state ShareReference | P2 | Low | Low | Better per-member overrides |
| 7 | Polymorphic mapping | P3 | Medium | Medium | EF Core TPH inheritance |
| 8 | Dictionary mapping | P3 | Medium | Medium | Dict ↔ typed object conversion |
| 9 | Mapping profiles/inheritance | P3 | Medium | Medium-High | Cross-class reuse via `[ForgeIncludes]` |
| 10 | Reverse mapping | P3 | Medium | Medium | Auto-generate bidirectional mappings |
| 11 | Generic forge methods | P3 | High | High | Type parameter support |

---

## P2 Features

### 1. Expression Assignment Mutability — `P2`

**Why**

Late mutation of `ExpressionAssignment` in `MemberAssignmentModel` violates immutability principles and makes caching unsafe. If caching is ever added to the generator pipeline, mutable fields will cause subtle correctness bugs.

**Design**

Replace mutable field with immutable reconstruction pattern. When `ForgeGenerator.ResolveExpressionInlining()` needs to update an assignment's expression, create a new `MemberAssignmentModel` with the updated expression rather than mutating the existing one.

```csharp
// Current (bad):
assignment.ExpressionAssignment = inlined;

// New (good):
assignment = assignment.WithExpressionAssignment(inlined);
```

**Complexity**

Low. Straightforward refactor of 2-3 mutation sites.

**Impact**

Medium. Internal-only change with no API surface change. Enables future caching optimizations without risk.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/Models/MemberAssignmentModel.cs` — Change setter to init-only, add `WithExpressionAssignment()` method
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Replace mutations in `ResolveExpressionInlining()` with reconstructions

**Suggested Approach**

1. Add helper method `WithExpressionAssignment(string expr)` to `MemberAssignmentModel`
2. Find all mutation sites in `ForgeGenerator.cs`
3. Replace `assignment.ExpressionAssignment = value` with `assignment = assignment.WithExpressionAssignment(value)`
4. Run existing tests (no new tests needed)

---

### 2. Computed Properties — `P2`

**Why**

Some destination properties don't map 1:1 from source — they're derived from multiple source members (e.g., `FullName = FirstName + " " + LastName`). Currently users must use after-hooks or manually compute these, losing the compile-time safety and performance benefits of Forge.

**Design**

Add `[ForgeComputed]` attribute on static methods in the forge class. The generator discovers these methods via Roslyn symbol analysis and emits direct method calls in the generated code.

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);

    [ForgeComputed(nameof(PersonDto.FullName))]
    private static string ComputeFullName(Person source)
        => source.FirstName + " " + source.LastName;
}
// Generates: __result.FullName = ComputeFullName(source);
```

**Complexity**

Medium. Need to discover `[ForgeComputed]` methods, validate signatures match destination property types, and emit calls in the right sequence (after construction, before return).

**Impact**

High. Solves real-world mapping scenarios with no reflection overhead.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgeComputedAttribute.cs` — New attribute
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Discover computed methods, emit calls
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `IReadOnlyList<ComputedPropertyMapping> ComputedProperties`
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — New diagnostics for validation errors

**Suggested Approach**

1. Define `[ForgeComputed(string destinationMemberName)]` attribute with `AllowMultiple = false`
2. In `ExtractMethod`, scan the forge class for methods decorated with `[ForgeComputed]`
3. Validate: return type matches destination property type, parameter is source type
4. Store computed methods in `ForgeMethodModel`
5. In `GenerateMethodBody`, emit calls after all regular assignments but before return
6. Add analyzer diagnostics: destination member doesn't exist, type mismatch, invalid signature

---

### 3. Conditional/Predicate Mapping — `P2`

**Why**

PATCH/partial-update APIs need to skip fields based on conditions: null values, default values (0, false, Guid.Empty), or custom predicates. `IgnoreIfNull` only handles nullable references; there's no way to express "skip if default" or "skip if predicate returns false" without after-hooks.

**Design**

Extend `[ForgeMap]` with two new optional properties:

1. `IgnoreIfDefault` — Wrap assignment in `if (!EqualityComparer<T>.Default.Equals(source.X, default))`
2. `Condition` — Reference a static method on the forge class that returns `bool`

```csharp
[ForgeMap("Name", IgnoreIfDefault = true)]
public string? NewName { get; set; }  // Skip if null or default(string)

[ForgeMap("Priority", Condition = nameof(ShouldMapPriority))]
public int NewPriority { get; set; }  // Skip based on custom predicate
```

**Complexity**

Medium. `IgnoreIfDefault` is straightforward (similar to `IgnoreIfNull`). `Condition` requires method resolution and signature validation similar to existing `[ForgeConverter]` logic.

**Impact**

High. Enables proper partial-update/PATCH semantics without workarounds.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgeMapAttribute.cs` — Add `IgnoreIfDefault` and `Condition` properties
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Emit conditional checks in assignment generation
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — New diagnostics for condition method resolution

**Suggested Approach**

1. Add boolean `IgnoreIfDefault` property to `ForgeMapAttribute`
2. Add string `Condition` property to `ForgeMapAttribute` (method name reference)
3. In `GenerateAssignment`, check `IgnoreIfDefault` flag and wrap in `if` with `EqualityComparer<T>.Default.Equals` check
4. In `GenerateAssignment`, check `Condition` flag, resolve the method, and wrap in `if` with that method call
5. Validate condition methods: must be static, take source type param, return bool
6. Add analyzer diagnostics for missing/invalid condition methods

---

### 4. Multi-Level Deep Flattening — `P2`

**Why**

Current flattening is limited to one level of nesting. Real-world domain models in ERP, CRM, and e-commerce have 3-4 levels of hierarchy that users need to flatten into a single DTO property (e.g., `source.Customer.BillingAddress.PostalCode` → `dest.CustomerBillingAddressPostalCode`). Users currently hand-write these or use nested forging.

**Design**

Extend `TryResolveFlattenedMapping` to recursively walk prefix matches at configurable depth. For destination member `CustomerBillingAddressPostalCode`:
1. Match prefix `Customer` from source (type A)
2. Type A has member `BillingAddress` (type B)
3. Type B has member `PostalCode` → match

Each intermediate step adds null-safety chaining: `source.Customer?.BillingAddress?.PostalCode`. For expression trees, convert to nested ternaries.

**Complexity**

Medium. Prefix-matching algorithm must handle ambiguity (what if `CustomerBilling` is also a member?) and performance (avoid exponential splits). Use greedy longest-prefix matching with configurable depth limit (default 2-3 levels).

**Impact**

Medium. Solves real-world flattening scenarios; reduces need for nested forging in some cases.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Extend `TryResolveFlattenedMapping` to handle multi-level recursion
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Diagnostic for ambiguous flattening (multiple valid prefixes)

**Suggested Approach**

1. Rename `TryResolveFlattenedMapping` to `TryResolveFlattenedMappingRecursive` with depth parameter
2. For each destination member name, try greedy longest-prefix matches against source members
3. If match found, recursively search that matched type's members for remaining suffix
4. Emit null-coalescing chains or nested ternaries as needed
5. Limit depth to prevent exponential behavior (suggest max 4 levels)

---

### 5. Cross-Class Nested Forge — `P2`

**Why**

Nested forging currently only searches the containing forge class. If `AddressForges.ToDto(Address)` is in a separate class from `PersonForges`, nested forge lookup fails and users must either duplicate methods or consolidate everything into one large, unmaintainable class.

**Design**

Add optional `[ForgeUses(Type[])]` attribute to forge class. When looking up nested forge methods with `AllowNestedForging = true`, scan included classes in addition to the current class.

```csharp
[Forge]
[ForgeUses(typeof(AddressForges), typeof(CompanyForges))]
public static partial class PersonForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
    // Discovers AddressForges.ToDto for source.Home and CompanyForges methods
}
```

**Complexity**

Medium-high. Requires cross-class symbol resolution during incremental generation. Existing pipeline processes each forge class independently; may need `Collect()` + `Combine()` step.

**Impact**

High. Enables modular, composable forge class hierarchies at scale.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgeUsesAttribute.cs` — New attribute
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Cross-class method lookup in nested forge resolution
- `src/FreakyKit.Forge.Generator/Models/ForgeClassModel.cs` — Add `IReadOnlyList<INamedTypeSymbol> IncludedForgeClasses`
- Incremental pipeline may need restructuring if direct symbol lookup isn't sufficient

**Suggested Approach**

1. Define `[ForgeUses(params Type[] forgeClasses)]` attribute
2. In `ExtractForgeClass`, parse the attribute and store included class types
3. In nested forge lookup, after searching current class, search included classes
4. Validate included classes are also decorated with `[Forge]`
5. Add analyzer diagnostic: circular includes, missing/non-forge includes

---

### 6. Tri-State ShareReference — `P2`

**Why**

Current `ShareReference` and `IgnoreIfNull` on `[ForgeMap]` are `bool` defaulting to `false`. C# compilers omit attribute arguments matching defaults, making `ShareReference = false` indistinguishable from "not set". This prevents per-member override of method-level `ShareReference = true`.

Example: Method sets `ShareReference = true` globally, but one member needs `ShareReference = false` (copy not share). Currently impossible to express.

**Design**

Replace `bool` properties with tri-state enum:

```csharp
public enum ForgeOptionalBool
{
    Inherit = 0,  // default — use method/class level
    True = 1,
    False = 2
}

public sealed class ForgeMapAttribute : Attribute
{
    public ForgeOptionalBool ShareReference { get; set; }  // was bool
    public ForgeOptionalBool IgnoreIfNull { get; set; }    // was bool
}
```

**Complexity**

Low code change; **breaking** API change. Existing code writing `ShareReference = true` must change to `ShareReference = ForgeOptionalBool.True`.

**Impact**

Low practical impact (few users need per-member override), but **breaking change** requires major version bump.

**Files to Modify**

- `src/FreakyKit.Forge/Enums/ForgeOptionalBool.cs` — New enum
- `src/FreakyKit.Forge/Attributes/ForgeMapAttribute.cs` — Change property types and defaults
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Handle tri-state enum values in code generation
- Update documentation and migration guide

**Suggested Approach**

1. Define `ForgeOptionalBool` enum with `Inherit`, `True`, `False` values
2. Update `ForgeMapAttribute` property types
3. In code generation, check: if `Inherit`, use method-level setting; else use explicit value
4. Document as breaking change requiring v2.0
5. Consider providing a compatibility layer or migration tool

---

## P3 Features — Backlog

### 7. Polymorphic Mapping / Derived Type Support — `P3`

**Why**

Applications using Entity Framework with Table-Per-Hierarchy (TPH) inheritance produce query results typed as the base entity. Mapping these to correct derived DTOs requires runtime type checks or discriminator switches that users currently hand-write. Forge should generate this dispatch logic.

**Design**

Add `[ForgePolymorphic]` attribute (repeatable) on a forge method to generate a switch expression with type patterns. The return type contract must be satisfied by all switch arms.

**Return Type Contract:**

The method's declared return type `TReturn` must be satisfied by ALL `[ForgePolymorphic]` method return types. Only two patterns are valid:

1. **Inheritance hierarchy** (recommended): Each `[ForgePolymorphic]` method returns a type derived from `TReturn`
   - `AnimalDto` is the base class
   - `DogDto : AnimalDto` and `CatDto : AnimalDto`
   - Switch arms return derived types; implicit upcast to `AnimalDto`
   - Validation: Check `ISymbol.IsAssignableTo(TReturn)` for each method's return type

2. **Common interface**: Each `[ForgePolymorphic]` method returns a type implementing `TReturn` (if `TReturn` is an interface)
   - `IAnimalDto` is the common interface
   - `DogDto : IAnimalDto` and `CatDto : IAnimalDto`
   - Switch arms return implementing types; implicit conversion to interface
   - Validation: Check `ISymbol.AllInterfaces.Contains(TReturn)` for each method's return type

**No explicit casting, no sibling types**: All switch arms must be directly assignable to `TReturn` without cast expressions. Unrelated sibling types (e.g., `DogDto` and `CatDto` both deriving from separate base) are not permitted.

```csharp
// Pattern 1: Inheritance hierarchy (recommended)
[Forge]
public static partial class AnimalForges
{
    public static partial AnimalDto MapBase(Animal source);
    public static partial DogDto MapDog(Dog source) where Dog : Animal;  // returns DogDto : AnimalDto
    public static partial CatDto MapCat(Cat source) where Cat : Animal;  // returns CatDto : AnimalDto

    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
    public static partial AnimalDto MapAny(Animal source);
}
// Generates:
// return source switch
// {
//     Dog dog => MapDog(dog),      // Returns DogDto, implicitly upcast to AnimalDto
//     Cat cat => MapCat(cat),      // Returns CatDto, implicitly upcast to AnimalDto
//     _ => MapBase(source)         // Returns AnimalDto
// };
```

```csharp
// Pattern 2: Common interface
[Forge]
public static partial class AnimalForges
{
    public static partial IAnimalDto MapBase(Animal source);
    public static partial DogDto MapDog(Dog source);      // Returns DogDto : IAnimalDto
    public static partial CatDto MapCat(Cat source);      // Returns CatDto : IAnimalDto

    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
    public static partial IAnimalDto MapAny(Animal source);
}
// Generates:
// return source switch
// {
//     Dog dog => MapDog(dog),      // Returns DogDto (implicitly implements IAnimalDto)
//     Cat cat => MapCat(cat),      // Returns CatDto (implicitly implements IAnimalDto)
//     _ => MapBase(source)         // Returns IAnimalDto
// };
```

**Validation & Diagnostics (Strict Assignability Enforcement):**

Before generating switch expression, the generator must validate that **each** `[ForgePolymorphic]` method's return type is directly assignable to `TReturn`:

1. For each `[ForgePolymorphic(derivedType, methodName)]` mapping:
   - Resolve the method symbol
   - Get the method's return type `MethodReturnType`
   - Check: Is `MethodReturnType` assignable to `TReturn`?
     - If `TReturn` is a class: `MethodReturnType` must be a derived class (inheritance check)
     - If `TReturn` is an interface: `MethodReturnType` must implement that interface
     - If unrelated: emit FKF8xx diagnostic and skip polymorphic generation
2. If any method fails assignability check: emit diagnostic with specific type mismatch details
3. If all pass: emit switch expression with all arms directly assignable (no casts)

**Complexity**

Medium. Main challenges: return type compatibility validation, switch expression generation with implicit upcasting, pattern ordering (derived before base), unreachable pattern detection.

**Impact**

Medium. Solves EF Core TPH scenarios; eliminates hand-written dispatch logic.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgePolymorphicAttribute.cs` — New attribute with `AllowMultiple = true`
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Detect `[ForgePolymorphic]`, validate return types, generate switch expression
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `IReadOnlyList<PolymorphicMapping> PolymorphicMappings`, `bool IsPolymorphicDispatch`
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Diagnostics: return type mismatch, unreachable patterns, invalid method refs

**Suggested Approach**

1. Define `[ForgePolymorphic(Type derivedSourceType, string mappingMethodName)]` attribute
2. In `ExtractMethod`, collect all `[ForgePolymorphic]` attributes
3. **Validation phase:**
   - Verify each derived source type is assignable from method's source parameter type
   - Verify mapping method exists and has signature `MethodName(derivedSourceType) → ReturnType`
   - **Verify mapping method's return type is assignable to the main method's return type** (inheritance, interface, or error)
4. If validation passes, emit switch expression:
   - Generate pattern for each `[ForgePolymorphic]` mapping (ordered: derived types first)
   - Generate default arm calling `MapBase` or throwing
5. Emit FKF8xx diagnostics for return type mismatches, unreachable patterns, invalid references
4. Generate switch expression: order patterns by inheritance depth (derived first)
5. Default arm calls base mapping or throws `InvalidOperationException`
6. Add analyzer diagnostics: unreachable patterns, missing methods, type mismatches

---

### 8. Dictionary Mapping — `P3`

**Why**

Many APIs return data as dictionaries (JSON deserialization, configuration systems, dynamic data). Being able to forge a typed object from a dictionary and vice versa bridges the gap between dynamic and static typing entirely at compile time with zero runtime overhead.

**Design**

Detect when method parameter or return type is `Dictionary<string, T>` and generate type-safe, policy-driven access patterns. Behavior controlled via optional `[ForgeDictionary]` attribute on the method or destination type.

**Dictionary to object (dict→object):**
```csharp
[ForgeDictionary(KeyCasing = KeyCasingPolicy.Exact, MissingKeyBehavior = MissingKeyPolicy.Throw)]
public static partial PersonDto FromDict(Dictionary<string, object> source);
// Generates (for each destination member):
// if (!source.ContainsKey("Name")) throw new KeyNotFoundException("Name");
// __result.Name = (string)source["Name"];  // Direct cast for object dict
//
// For numeric: if (!source.ContainsKey("Age")) throw ...;
// __result.Age = (int)source["Age"];  // Cast to target type
```

**Dictionary to object with string values (parse mode):**
```csharp
[ForgeDictionary(KeyCasing = KeyCasingPolicy.Exact, MissingKeyBehavior = MissingKeyPolicy.UseDefault)]
public static partial PersonDto FromDict(Dictionary<string, string> source);
// Generates:
// __result.Name = source.ContainsKey("Name") ? source["Name"] : null;
// __result.Age = source.ContainsKey("Age") ? int.Parse(source["Age"]) : 0;
```

**Object to dictionary (object→dict):**
```csharp
[ForgeDictionary(KeyCasing = KeyCasingPolicy.Exact, NullValueBehavior = NullValuePolicy.Include)]
public static partial Dictionary<string, object> ToDict(PersonDto source);
// Generates:
// var __result = new Dictionary<string, object>();
// __result["Name"] = source.Name;  // Always include, even if null
// __result["Age"] = source.Age;
```

**Type Conversion & Error Policy:**

**Dict-to-object conversions:**
1. **Dictionary<string, object>**: Direct cast `(TargetType)source["Key"]`
   - Runtime cast failure if value is wrong type or null → InvalidCastException propagates
   - For nullable destination: null values are allowed, propagate directly
   - For non-nullable destination: null values cause InvalidCastException

2. **Dictionary<string, string>**: Parse or convert with fallback
   - Primitive types: `int.Parse()`, `bool.Parse()`, `decimal.Parse()`, etc.
   - Enum types: `Enum.Parse<EnumType>(value)`
   - Parse failures throw `FormatException` (propagate or use MissingKeyBehavior fallback)
   - DateTime: `DateTime.Parse()` or `DateTime.TryParse()` with MissingKeyBehavior fallback
   - Nullable types: if value is null or missing, null is assigned; otherwise parse

3. **Unsupported type conversions** (emit diagnostic, skip member):
   - Complex types without custom converter → FKF7xx diagnostic, member excluded
   - Collections (List<T>, IEnumerable<T>) from dict values → FKF7xx diagnostic
   - Nested objects requiring deep mapping → suggest using nested forge instead

**Missing key behavior (configurable via `[ForgeDictionary]`):**
- `Throw` (default): `if (!source.ContainsKey(key)) throw new KeyNotFoundException(key);`
- `UseDefault`: `__result.Member = source.ContainsKey(key) ? source[key] : default(T);`
- `Skip`: No assignment at all if key missing (member left uninitialized or at default)
- `ReturnNull`: Only valid for nullable destination types; assign null if key missing

**Key casing policy (configurable via `[ForgeDictionary]`):**
- `Exact` (default): Match member name exactly (`Name` → `"Name"`)
  - Direct key lookup: `source["Name"]`
  - If key not found, triggers `MissingKeyPolicy` (Throw, UseDefault, Skip, ReturnNull)
- `IgnoreCase`: Case-insensitive key lookup with fallback to `MissingKeyPolicy`
  - Lookup: `source.Keys.FirstOrDefault(k => k.Equals(memberName, StringComparison.OrdinalIgnoreCase))`
  - If `FirstOrDefault` returns null (no matching key), triggers `MissingKeyPolicy` instead of throwing KeyNotFoundException
  - Code pattern: `var resolvedKey = source.Keys.FirstOrDefault(...); if (resolvedKey == null) { apply MissingKeyPolicy } else { access source[resolvedKey] }`
- `CamelCase`: Transform member name to camelCase (`PersonFirstName` → `"personFirstName"`), then apply exact lookup
  - Triggers `MissingKeyPolicy` if transformed key not found
- `SnakeCase`: Transform member name to snake_case (`PersonFirstName` → `"person_first_name"`), then apply exact lookup
  - Triggers `MissingKeyPolicy` if transformed key not found

**Null value behavior in object-to-dict (configurable via `[ForgeDictionary]`):**
- `Include` (default): All values included, even if null → `__result["Name"] = source.Name;`
- `Skip`: Skip null values → `if (source.Name != null) __result["Name"] = source.Name;`

**Complexity**

Medium-high. Type conversion logic must handle primitives (cast vs parse), enums, nullable types, and error cases. Key lookup with casing options adds conditional logic. Member discovery is inverted for dict→object. Validation must reject unsupported types with clear diagnostics.

**Impact**

Medium. Solves dynamic-to-static mapping scenarios; commonly needed for JSON deserialization, configuration readers, and API integrations.

**Files to Modify**

- `src/FreakyKit.Forge/Enums/KeyCasingPolicy.cs` — New enum: Exact, IgnoreCase, CamelCase, SnakeCase
- `src/FreakyKit.Forge/Enums/MissingKeyPolicy.cs` — New enum: Throw, UseDefault, Skip, ReturnNull
- `src/FreakyKit.Forge/Enums/NullValuePolicy.cs` — New enum: Include, Skip
- `src/FreakyKit.Forge/Attributes/ForgeDictionaryAttribute.cs` — New attribute with properties: `KeyCasingPolicy`, `MissingKeyPolicy`, `NullValuePolicy`
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — 
  - Detect dictionary type in `ExtractMethod`
  - New private method `GenerateDictToObjectAssignment()` for dict→object conversions
  - New private method `GenerateObjectToDictAssignment()` for object→dict conversions
  - Type support validation: reject unsupported types with FKF7xx diagnostic
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `bool IsDictionaryMapping`, `DictionaryMappingInfo DictInfo` (stores conversion mode, policies)
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — New diagnostics:
  - FKF7xx: Unsupported dictionary value type (complex type, collection, etc.)
  - FKF7xx: Dictionary<TKey, TValue> with non-string key type not supported
  - FKF7xx: Parse error for Dictionary<string, string> with incompatible destination type

**Suggested Approach**

1. Define policy enums: `KeyCasingPolicy`, `MissingKeyPolicy`, `NullValuePolicy`
2. Define `[ForgeDictionary]` attribute with configurable policies (defaults to safe: Exact, Throw, Include)
3. In `ExtractMethod`, detect dictionary types:
   - Accept `Dictionary<string, object>`, `Dictionary<string, string>`, `IReadOnlyDictionary<string, T>` variants
   - Reject any other TKey or unsupported TValue types with FKF7xx diagnostic
4. Validate all destination members for type convertibility (reject complex types, collections)
5. Emit conversion code based on dictionary value type:
   - `Dictionary<string, object>`: Direct cast `(TargetType)source[GetKey(keyName)]`
   - `Dictionary<string, string>`: Conditional parse `source.ContainsKey(key) ? TypeConvert(source[key]) : default`
6. Implement key lookup with casing policy:
   - Exact: `source[memberName]`
   - IgnoreCase: `source[source.Keys.FirstOrDefault(k => k.Equals(memberName, OrdinalIgnoreCase))]`
   - CamelCase/SnakeCase: Transform member name before lookup
7. Implement missing-key handling:
   - Throw: Guard with ContainsKey check, throw if not found
   - UseDefault: Ternary with default(T)
   - Skip: Emit no assignment
   - ReturnNull: Ternary with null (only for nullable types, else emit diagnostic)
8. For object→dict, respect NullValuePolicy:
   - Include: Always assign
   - Skip: Guard with null check before assignment
9. Emit FKF7xx diagnostics for unsupported conversions, impossible combinations (ReturnNull on non-nullable), parse failures on string dict

---

### 9. Mapping Profiles / Inheritance — `P3`

**Why**

Large projects have shared base types (e.g., `BaseEntity` with `Id`, `CreatedAt`, `UpdatedAt`) that must be mapped consistently across many forge classes. Currently each class redeclares the mapping or relies on independent member-name matching, leading to duplication and inconsistency.

**Design**

Add `[ForgeIncludes]` attribute to include mappings from another forge class:

```csharp
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
    // PersonDto : BaseDto, Person : BaseEntity
    // Base mappings inherited/inlined
}
```

**Two implementation options:**
- **Delegate**: Call included forge method for base members (simple, creates runtime dependency)
- **Inline**: Copy base assignments into derived forge method (no dependency, more generated code)

**Complexity**

Medium-high. Requires cross-class symbol resolution during incremental generation. Pipeline currently processes each forge class independently; may need aggregation step. Must handle circular/diamond includes.

**Impact**

Medium. Enables DRY principle for multi-class forge hierarchies.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgeIncludesAttribute.cs` — New attribute
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Cross-class method lookup, delegation/inlining logic
- `src/FreakyKit.Forge.Generator/Models/ForgeClassModel.cs` — Add `IReadOnlyList<ForgeMethodModel> IncludedMethods`
- Incremental pipeline may need `Collect()` + `Combine()` steps for multi-class aggregation

**Suggested Approach**

1. Define `[ForgeIncludes(params Type[] forgeClasses)]` attribute
2. In `ExtractForgeClass`, parse attribute and resolve included forge class symbols
3. Extract forge methods from included classes (same process as primary class)
4. Store in `ForgeClassModel.IncludedMethods`
5. In `GenerateMethodBody`, decide: emit call to included method (v1) or copy its assignments (later)
6. Validate: included classes exist, have `[Forge]`, no circular includes

---

### 10. Reverse Mapping — `P3`

**Why**

Many applications need bidirectional mapping — e.g., entity→DTO for API responses, DTO→entity for writes. Currently users must write both methods manually, duplicating logic and maintenance burden.

**Design**

Auto-generate a reverse method from an existing forward mapping, with explicit validation of invertibility:

```csharp
[ForgeMethod(GenerateReverse = true)]
public static partial PersonDto ToDto(Person source);
// Also generates: public static partial Person FromDto(PersonDto source);
// Only if forward mapping is invertible per validation rules below
```

**Invertibility Validation (Gating):**

Before generating reverse method, validate that the forward mapping contains **only** these safe patterns:
- Direct 1:1 member assignments (same type or compatible conversion)
- `[ForgeMap]` name remappings (reversible: `FirstName → FullName` reverses to `FullName → FirstName`)
- `[ForgeIgnoreReverse]` marked members (explicitly one-way)
- Simple nested forge calls where reverse method exists (e.g., if forward calls `ToAddressDto`, reverse calls `FromAddressDto`)

**Non-invertible patterns (emit diagnostic, skip reverse generation):**
- `[ForgeComputed]` members (computed properties have no source to reverse from)
- `IgnoreIfNull` or `IgnoreIfDefault` on any member (reverse doesn't know original null/default state)
- `NullFallback` with `DefaultConstruct` (reverse can't distinguish null from constructed default)
- `Condition` (predicate-based) mappings (reverse can't invert conditional logic)
- Custom `[ForgeConverter]` converters (not guaranteed to be reversible)
- Nested forge with `AllowFlattening = true` (multi-level flattening not invertible)
- Members with `[ForgeIgnore]` on source side (those members unmapped in forward)

**Reverse Method Generation (Separate Code Path):**

1. In `ForgeGenerator`, after validating forward mapping is invertible, emit reverse method **separately**
2. Reverse method does NOT share code paths with forward generation
3. Build bidirectional mapping table: track each forward `srcMember → dstMember` and emit reverse `dstMember → srcMember`
4. For nested forge calls: emit corresponding reverse call (e.g., `ToDto` → `FromDto`)
5. Constructor handling: may differ from forward (destination's constructor in reverse may not match source's)
6. Preserve null-safety: reverse method honors the same `ShareReference` and value-type handling as forward

**Complexity**

Medium. Main challenges: exhaustive validation of forward mapping for invertibility, maintaining bidirectional name mapping state, detecting when reverse nested forge methods don't exist, constructor inference on reverse type.

**Impact**

Medium-high. Common pattern in real-world applications; eliminates significant boilerplate for read-write APIs.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgeMethodAttribute.cs` — Add `bool GenerateReverse`, optional `string ReverseMethodName`
- `src/FreakyKit.Forge/Attributes/ForgeIgnoreReverseAttribute.cs` — New attribute for one-way members
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `bool ShouldGenerateReverse`, `string? ReverseMethodName`, `bool IsInvertible` (computed during extraction)
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — 
  - New private method `ValidateInvertible(ForgeMethodModel)` returns bool + diagnostics list
  - New private method `GenerateReverseMethod(ForgeMethodModel)` as separate code path from `GenerateMethodBody`
  - In main extraction, after forward body generation, call `ValidateInvertible` before attempting reverse
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — New diagnostics:
  - FKF6xx: Non-invertible mapping (computed, conditional, flattened, etc.)
  - FKF6xx: Reverse forge method not found (for nested reverse calls)
  - FKF6xx: GenerateReverse requires explicit `ReverseMethodName` when name inference impossible

**Suggested Approach**

1. Add `GenerateReverse` flag to `[ForgeMethod]` (default false)
2. Add `ReverseMethodName` optional string property (defaults to null; auto-infer `ToX` → `FromX` if possible)
3. In `ExtractMethod`, after collecting member mappings for forward method:
   - Call `ValidateInvertible(method)` which checks each member assignment against invertibility rules
   - If any non-invertible pattern found, return false + emit diagnostic
   - Store result in `ForgeMethodModel.IsInvertible`
4. If `GenerateReverse = true` AND `IsInvertible = false`, emit diagnostic and skip reverse generation
5. If `GenerateReverse = true` AND `IsInvertible = true`:
   - Call separate `GenerateReverseMethod` with no code sharing with forward path
   - Build name map: for each forward `src.X → dst.Y`, record reverse `dst.Y → src.X`
   - For each mapped member, emit reverse assignment
   - For nested forge members, emit reverse method call (validate reverse method exists)
6. Infer reverse method name: if forward is `ToDto`, reverse is `FromDto`; if inference fails and `ReverseMethodName` not provided, emit diagnostic
7. Add `[ForgeIgnoreReverse]` support: members with this attribute are skipped in reverse generation (one-way mapping)

---

### 11. Generic Forge Methods — `P3`

**Why**

Users frequently need to map generic wrapper types (`Result<T>`, `ApiResponse<T>`, `PagedList<T>`) where the wrapper is the same but the payload type varies. Currently users must write a separate method for each concrete type, which is pure boilerplate.

**Design**

Support type parameters on forge methods with deterministic nested type mapping discovery:

```csharp
public static partial ApiResponse<TDto> MapResponse<TEntity, TDto>(
    ApiResponse<TEntity> source) 
    where TDto : class;
// Generates:
// if (source.Data != null)
// {
//     __result.Data = MapResponse<Company, CompanyDto>(source.Data);
// }
// __result.StatusCode = source.StatusCode;
// __result.Message = source.Message;
```

**Type Parameter Resolution Rule (V1):**

When a nested member has type `TEntity` (a type parameter), the generator:
1. Identifies the corresponding type parameter `TDto` from the method signature
2. Emits a call to the **same generic method** with substituted type arguments
3. At call sites like `MapResponse<Person, PersonDto>(apiResp)`, the concrete types are `TEntity=Person, TDto=PersonDto`
4. If nested `Data` contains a `Company`, the generated call becomes `MapResponse<Company, CompanyDto>(nestedData)` — **that overload must exist**
5. Constraint validation ensures `CompanyDto` satisfies any where clauses (e.g., `where TDto : class`)

**Forbidden patterns** (emit diagnostics):
- Nested member of unconstrained type parameter (no matching T_dto)
- Type parameter used in nested forging without a paired mapping parameter
- Circular type parameter chains

This is **self-recursive generic mapping only in V1** — no discovery of unrelated generic methods.

**Complexity**

High. Type parameter tracking through nested member resolution, validation of generic method availability at all instantiation points, code generation with preserved type parameter names, and circular dependency detection.

**Impact**

High. Enables generic mapping scenarios with zero boilerplate for wrapper types and generic DTOs.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Parse type parameters, substitute in nested forge discovery, emit generic method calls with substituted type arguments
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `IReadOnlyList<TypeParameter> TypeParameters`, add `TypeParameter[] TypeParameterMap` for tracking TEntity↔TDto pairs
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Diagnostics: unresolved type params, unpaired type parameters, constraint violations, missing generic overloads

**Suggested Approach**

1. In `ExtractMethod`, detect and parse type parameters; build TEntity↔TDto mapping from method signature
2. In nested forge discovery, when encountering a nested member of type `TParam` (a type parameter):
   - Look up the paired type parameter from the map (e.g., `TEntity` pairs with `TDto`)
   - Emit a call to the same method name with substituted type arguments: `MethodName<NestedType, PairedType>(nestedMember)`
   - Verify that overload exists (may be discovered in same pass or assumed to exist by caller)
3. Validate type parameters satisfy where clause constraints
4. In code generation, preserve type parameter names exactly as written in the method signature
5. For V1, do NOT support:
   - Cross-method generic discovery (e.g., calling a different generic method)
   - Inference of type parameters from usage (require explicit type arguments at call sites)
   - Partial generic application (all type parameters must be provided)
6. Emit diagnostics: unresolved type params, unpaired type parameters, missing generic overloads, constraint violations, circular type dependencies

---

## Technical Debt & Bug Fixes

Critical correctness bugs and usability improvements identified through code audit. Organized by severity.

### Critical Bugs

#### Collection Fallback Type Safety — Type Mismatch

**Why**

When `NullFallback == DefaultConstruct` is set on a collection member (e.g., `List<Address>`), the generated fallback uses hardcoded `Enumerable.Empty<object>()` instead of `Enumerable.Empty<ElementType>()`. This causes type mismatches and runtime cast failures when the destination collection has a specific element type.

**Design**

Infer the correct element type from the destination collection type (e.g., if destination is `List<AddressDto>`, use `Enumerable.Empty<AddressDto>()`). The fallback type must match the collection's element type for type-safe code generation.

**Complexity**

Low. Extract element type from destination collection INamedTypeSymbol and substitute into fallback expression.

**Impact**

Critical for correctness. Generated code compiles but fails at runtime with InvalidCastException.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Compute element type when generating collection fallback expressions (lines 769-778)

**Suggested Approach**

1. When building `NullFallback` expression for collection, extract destination collection's element type
2. Replace hardcoded `Enumerable.Empty<object>()` with `Enumerable.Empty<ElementType>()`
3. Add tests verifying null-fallback collections type-check and run correctly

---

### Medium-Priority Bugs

#### Flattening Members Silently Excluded From Expression Properties

**Why**

When flattening is applied and the destination member uses an expression property (`GenerateExpression = true`), the code converts null-conditional chaining (`?.`) to nested ternaries for imperative code (required for expression-tree compatibility). However, the expression path sets `exprAssign = null`, silently excluding the flattened member from the expression property with no diagnostic warning.

**Design**

Either: (1) Set `exprAssign` to the ternary expression so flattened members ARE included in expression properties, OR (2) Emit FKF506 diagnostic to warn users that flattened members cannot be used in `IQueryable.Select()` expressions.

**Complexity**

Low. Either add ternary expression support to expression paths or emit diagnostic after conversion.

**Impact**

Medium. Silent exclusion causes users to assume flattened members work in LINQ queries, leading to runtime query failures.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Lines 418-449 (TryResolveFlattenedMapping); either generate ternary for `exprAssign` or emit FKF506

**Suggested Approach**

1. In `TryResolveFlattenedMapping`, after converting flatten expression to ternary, check if `GenerateExpression = true`
2. Option A: Set `exprAssign` to the ternary expression (convert nested ternaries to expression-tree compatible form)
3. Option B: If conversion is complex, emit FKF506 and skip expression property for this member
4. Add test cases for flattening + expression properties

---

#### Diagnostic ID Allocation Scheme Missing

**Why**

Diagnostic IDs are assigned sequentially (FKF001-508) without a reserved-range policy. Future features (P2, P3) will need new diagnostics, but there's no pre-allocated range. This risks ID collisions or out-of-order assignments that break backward compatibility.

**Design**

Define a structured diagnostic ID allocation scheme with reserved ranges for future features. Document which ranges are reserved for what purpose (e.g., FKF501-600 for new features).

**Complexity**

Low. Documentation only; no code changes.

**Impact**

Medium. Enables safe diagnostic ID allocation in future versions.

**Files to Modify**

- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Add comments documenting reserved ID ranges

**Suggested Approach**

1. Document diagnostic ID ranges:
   - FKF001-099: Mode & class-level validation (existing)
   - FKF100-199: Member matching & discovery (existing)
   - FKF200-299: Type conversion & compatibility (existing)
   - FKF300-399: Nested forging & circularity (existing)
   - FKF400-499: Deprecated/reserved
   - FKF500-599: **RESERVED for future P2/P3 features**
   - FKF600-699: **RESERVED for performance/optimization warnings**
2. Add header comment explaining the scheme
3. Mark FKF507 as permanently deprecated with note

---

### Low-Priority Issues

#### Expression Nesting Depth Limit Not Enforced

**Why**

FKF508 (ExpressionDeepNesting) is emitted as Info when nesting depth exceeds 5. However, there's no hard limit—arbitrarily deep nesting is allowed, just warned about. Very deep nesting can generate megabytes of source code, causing compiler errors with unclear root causes.

**Design**

Implement a hard expression-depth limit (e.g., 10 levels). When exceeded, emit Error FKF5xx and either truncate the expression or fall back to method calls instead of inlining.

**Complexity**

Low. Add a configurable depth check in the expression inlining loop.

**Impact**

Low. Prevents surprise compiler errors on deeply nested mappings.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Expression inlining depth tracking (lines 1440-1447)

**Suggested Approach**

1. Define const MAX_EXPRESSION_DEPTH = 10
2. When depth exceeds limit, emit Error FKF5xx "Expression nesting depth limit exceeded"
3. Alternatively, auto-fallback to method calls for deep chains instead of inlining
4. Add tests with various nesting depths to verify limit enforcement

---

#### Missing Validation: Constructor Parameter Accessibility

**Why**

When resolving parameterized constructors, the code verifies that constructor parameters can be satisfied from source members, but does NOT verify that parameters themselves are accessible (e.g., `internal` parameters in types from other assemblies). Generated code compiles but fails at runtime with "inaccessible due to protection level."

**Design**

Before using a constructor parameter in generated code, verify the parameter's `DeclaredAccessibility` is `Public`. Emit diagnostic if not.

**Complexity**

Low. Add accessibility check in `DetermineConstruction`.

**Impact**

Low. Edge case affecting cross-assembly scenarios.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — DetermineConstruction method (lines 1606-1676); add accessibility validation

**Suggested Approach**

1. For each constructor parameter, check `parameter.DeclaredAccessibility == Accessibility.Public`
2. If not, emit diagnostic FKF5xx "Constructor parameter inaccessible from public generated code"
3. Skip this constructor and try others if available

---

#### Diagnostic Aggregation Masks Primary Errors

**Why**

The generator collects diagnostics throughout extraction and continues processing even after critical errors. This causes multiple confusing diagnostics to appear (e.g., FKF020 "has a body" + FKF100 "member missing source") when only FKF020 is the real blocker. Users must fix FKF020 first before understanding the actual mapping issues.

**Design**

Implement early termination per method on critical errors. Stop analyzing a problematic method after emitting a fatal diagnostic, preventing cascading secondary errors.

**Complexity**

Low. Add early-return logic after critical diagnostics.

**Impact**

Low. Improves usability by reducing noise.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Error collection and early return (lines 186-210)

**Suggested Approach**

1. When FKF020 (has a body), FKF003-005 (invalid class), or other fatal diagnostics are emitted, return immediately from method processing
2. Skip member-mapping analysis for that method
3. Prevents secondary "missing member" diagnostics from appearing

---

#### Message Clarity: NullableValueType Fallback Not Mentioned

**Why**

When a nullable value type (e.g., `int?`) maps to non-nullable (e.g., `int`) without `DefaultValue`, FKF201 warns that `.Value` may throw. However, the diagnostic message doesn't mention that setting `DefaultValue` on `[ForgeMap]` prevents the warning entirely.

**Design**

Update FKF201 diagnostic message to mention `DefaultValue` as an escape hatch.

**Complexity**

Low. Update diagnostic message.

**Impact**

Low. Users may miss that `DefaultValue` solves the problem.

**Files to Modify**

- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — FKF201 diagnostic descriptor

**Suggested Approach**

1. Update FKF201 message to include: "... or set DefaultValue on [ForgeMap] to provide a fallback value instead."

---

#### Missing Validator: [ForgeConverter] Discoverability

**Why**

FKF221 validates that a `[ForgeConverter]` method has correct signature (static, non-void, non-generic, 1 param). However, it doesn't validate that the method is discoverable (public/internal visibility). If a converter is private or has inaccessible parameter types, the generator silently skips it and later emits FKF200 (incompatible types) instead.

**Design**

When validating `[ForgeConverter]`, also check that the method is publicly or internally accessible.

**Complexity**

Low. Add visibility checks to FKF221 validation.

**Impact**

Low. Users add a `[ForgeConverter]` expecting it to work, but if it's private, it's silently ignored.

**Files to Modify**

- `src/FreakyKit.Forge.Analyzer/` — ForgeConverterValidator (wherever FKF221 is emitted)

**Suggested Approach**

1. In FKF221 validation, add checks: method must be `public` or `internal`; parameter/return types must be accessible
2. Emit FKF221 with additional detail if visibility is wrong

---

#### Documentation Gap: Attribute Feature Interaction Matrix

**Why**

Documentation explains what `AllowNestedForging` and `AllowFlattening` do individually, but doesn't provide guidance on when to use each. Users are confused: "Should I use nested forging or flattening for `Customer.Address.City` → `CustomerAddressCity`?"

**Design**

Add a decision matrix in docs explaining the trade-offs and recommending flattening for DTO fields, nested forging for type mismatches.

**Complexity**

Low. Documentation only.

**Impact**

Low. Improves user onboarding.

**Files to Modify**

- `docs/attributes.md` — Add "Design Decision: Flattening vs Nested Forging" section

**Suggested Approach**

1. Create comparison table: Flattening (one-level, implicit) vs Nested Forging (type-aware, multi-level)
2. Add examples for each use case
3. Document when to use which feature

---

#### Documentation Gap: ShareReference Semantics on Collections

**Why**

`ShareReference` on `[ForgeMethodAttribute]` affects "mutable same-type collections" (documented as a concept, but the exact list of collection types is only in private helper code `IsMutableSameTypeCollection()`). Users can't reliably predict which collection types are deep-copied vs reference-shared without reading source.

**Design**

Add a table to `[ForgeMethod]` documentation listing all collection types affected by `ShareReference`.

**Complexity**

Low. Documentation only.

**Impact**

Low. Users can predict behavior without reading source code.

**Files to Modify**

- `docs/attributes.md` — [ForgeMethod] documentation; add table of mutable collection types

**Suggested Approach**

1. List all collection types checked by `IsMutableSameTypeCollection()`: List<T>, HashSet<T>, LinkedList<T>, Stack<T>, Queue<T>, etc.
2. Document which types trigger `ShareReference` behavior
3. Clarify which types are always copied (IEnumerable, IList, etc.)
