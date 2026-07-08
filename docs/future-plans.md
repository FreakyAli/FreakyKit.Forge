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

Add `[ForgePolymorphic]` attribute (repeatable) on a forge method to generate a switch expression with type patterns:

```csharp
[Forge]
public static partial class AnimalForges
{
    public static partial AnimalDto MapBase(Animal source);
    public static partial DogDto MapDog(Dog source);
    public static partial CatDto MapCat(Cat source);

    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
    public static partial AnimalDto MapAny(Animal source);
}
// Generates: return source switch { Dog => MapDog(...), Cat => MapCat(...), _ => MapBase(...) };
```

**Complexity**

Medium. Main challenges: verify derived types are assignable, verify referenced methods exist, order patterns (derived before base), handle fallback.

**Impact**

Medium. Solves EF Core TPH scenarios; eliminates hand-written dispatch logic.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgePolymorphicAttribute.cs` — New attribute with `AllowMultiple = true`
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Detect `[ForgePolymorphic]`, generate switch expression
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `IReadOnlyList<PolymorphicMapping> PolymorphicMappings`
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Diagnostics for unreachable patterns, invalid method refs

**Suggested Approach**

1. Define `[ForgePolymorphic(Type derivedType, string mappingMethodName)]` attribute
2. In `ExtractMethod`, collect all `[ForgePolymorphic]` attributes
3. Validate: each derived type is assignable from source parameter, mapping method exists and has correct signature
4. Generate switch expression: order patterns by inheritance depth (derived first)
5. Default arm calls base mapping or throws `InvalidOperationException`
6. Add analyzer diagnostics: unreachable patterns, missing methods, type mismatches

---

### 8. Dictionary Mapping — `P3`

**Why**

Many APIs return data as dictionaries (JSON deserialization, configuration systems, dynamic data). Being able to forge a typed object from a dictionary and vice versa bridges the gap between dynamic and static typing entirely at compile time with zero runtime overhead.

**Design**

Detect when method parameter or return type is `Dictionary<string, T>` and generate appropriate key-based access patterns:

**Dictionary to object:**
```csharp
public static partial PersonDto FromDict(Dictionary<string, object> source);
// Generates:
// __result.Name = (string)source["Name"];
// __result.Age = (int)source["Age"];
```

**Object to dictionary:**
```csharp
public static partial Dictionary<string, object> ToDict(PersonDto source);
// Generates:
// var __result = new Dictionary<string, object>();
// __result["Name"] = source.Name;
// __result["Age"] = source.Age;
```

**Complexity**

Medium. Member discovery logic is inverted (for dict→object, destination members become the key source). Type conversion requires cast expressions for `object` and parse expressions for `string` keys. Construction uses indexer syntax.

**Impact**

Medium. Solves dynamic-to-static mapping scenarios; commonly needed for API integrations.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Detect dictionary types, generate indexer-based assignments
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `bool IsDictionaryMapping`
- `src/FreakyKit.Forge/Attributes/ForgeDictionaryAttribute.cs` — Optional attribute for configuration (key casing, missing-key behavior)
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Diagnostics for unsupported dictionary types

**Suggested Approach**

1. In `ExtractMethod`, detect `Dictionary<TKey, TValue>` parameters/returns
2. For dict→object: use destination type members as keys (keys inferred from member names)
3. For object→dict: use source type members (values assigned from source)
4. Generate `(TargetType)source["Key"]` for object extraction with `ContainsKey` checks
5. Generate `__result["Key"] = source.Property` for dict building
6. Support typed dictionaries (`Dictionary<string, object>` and `Dictionary<string, string>`) in v1

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

Auto-generate a reverse method from an existing forward mapping:

```csharp
[ForgeMethod(GenerateReverse = true)]
public static partial PersonDto ToDto(Person source);
// Also generates: public static partial Person FromDto(PersonDto source);
```

The reverse method must correctly handle:
- `[ForgeMap]` name remappings (if `FirstName` → `FullName`, reverse maps `FullName` → `FirstName`)
- One-way properties (some members should not be reverse-mapped)
- Constructor vs property setters on the reverse target type

**Complexity**

Medium. Main challenges: bidirectional name mapping tracking, one-way property declarations, constructor handling on reverse direction, nested reverse forge methods.

**Impact**

Medium-high. Common pattern in real-world applications; eliminates significant boilerplate.

**Files to Modify**

- `src/FreakyKit.Forge/Attributes/ForgeMethodAttribute.cs` — Add `bool GenerateReverse`, optional `string ReverseMethodName`
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `bool ShouldGenerateReverse`, `string? ReverseMethodName`
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — After generating forward method, generate reverse with inverted member mappings
- `src/FreakyKit.Forge/Attributes/ForgeIgnoreReverseAttribute.cs` — New attribute for one-way members

**Suggested Approach**

1. Add `GenerateReverse` flag to `[ForgeMethod]`
2. After generating forward method, track all name mappings in a bidirectional map
3. Generate reverse method with inverted mappings (B → A where forward was A → B)
4. Support `[ForgeIgnoreReverse]` on members to exclude them from reverse
5. Handle constructor-based construction on reverse (may differ from forward)
6. For nested forging, recursively look up reverse forge methods

---

### 11. Generic Forge Methods — `P3`

**Why**

Users frequently need to map generic wrapper types (`Result<T>`, `ApiResponse<T>`, `PagedList<T>`) where the wrapper is the same but the payload type varies. Currently users must write a separate method for each concrete type, which is pure boilerplate.

**Design**

Support type parameters on forge methods:

```csharp
public static partial ApiResponse<TDto> MapResponse<TEntity, TDto>(
    ApiResponse<TEntity> source) 
    where TDto : class;
// Generates:
// __result.Data = /* discover mapping TEntity → TDto */;
// __result.StatusCode = source.StatusCode;
// __result.Message = source.Message;
```

Type parameter resolution: for nested member `Data : TEntity`, discover a forge method that maps `TEntity → TDto`. Can be parameterized over the same method or a different method.

**Complexity**

High. Must resolve type parameters, validate constraints, generate type-safe code for all instantiations. A reasonable v1 scope: support single or two type parameters with known mapping method names (constraint-based discovery).

**Impact**

High. Enables generic mapping scenarios with zero boilerplate.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Parse type parameters, resolve in nested forge discovery, validate constraints
- `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` — Add `IReadOnlyList<TypeParameter> TypeParameters`
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Diagnostics for unresolved type params, constraint violations

**Suggested Approach**

1. In `ExtractMethod`, detect and parse type parameters and where clauses
2. For nested forge member of type `TEntity`, try to discover mapping method for `TEntity → TDto`
3. Validate type parameters satisfy where clause constraints
4. In code generation, preserve type parameter names in generated code
5. For v1, require explicit type arguments or constraint-based discovery (no inference ambiguity)
6. Emit diagnostics: unresolved type params, violated constraints, ambiguous mappings
