# Future Plans

Features and fixes under consideration for future versions of FreakyKit.Forge. Each section includes enough detail to serve as a starting point for implementation.

**Completed:**
- ✅ Null-object fallback (NullFallback enum) — P1
- ✅ Circular forge detection (FKF301) — P2
- ✅ String injection in null fallback — P1
- ✅ Null reference safety in symbol resolution — P1
- ✅ Redundant cycle detection removal (FKF507 dead code) — P2
- ✅ Edge case tests (partial: large members, deep generics, null fallback boundary) — P2

**Pending Removal (marked for deprecation):**
- FKF507 diagnostic descriptor — kept for backward compatibility in v1.x, scheduled for removal in v2.0

## Priority Matrix

Each feature is prioritized using an **Impact × Effort** matrix:

- **P1 — Do first.** Critical bugs or high-impact features with low-to-medium implementation effort. Correctness, security, and infrastructure issues belong here.
- **P2 — Do next.** Either high-impact features requiring significant effort (cross-class nested forging, conditional mapping), or moderate-impact fixes/features with low effort.
- **P3 — Backlog.** Valuable features with open design questions, high complexity, or niche use cases. Need more design work before implementation.

| # | Feature | Priority | Impact | Effort | Notes |
|---|---------|----------|--------|--------|-------|
| 1 | Expression assignment mutability | P2 | Medium | Low | Refactor to immutable pattern |
| 2 | Missing edge case tests | P2 | Medium | Medium | Large member counts, deep generics |
| 3 | Production benchmark suite | P2 | Medium | Medium | Real-world scenario benchmarking |
| 4 | Computed properties | P2 | High | Medium | `[ForgeComputed]` for derived members |
| 5 | Conditional/predicate mapping | P2 | High | Medium | `IgnoreIfDefault` + custom predicates |
| 6 | Multi-level deep flattening | P2 | Medium | Medium | Support 2+ levels of nesting |
| 7 | Cross-class nested forge | P2 | High | Medium-High | Discover methods in other classes |
| 8 | Tri-state ShareReference | P2 | Low | Low | Better per-member overrides |
| 9 | Polymorphic mapping | P3 | Medium | Medium | EF Core TPH inheritance |
| 10 | Dictionary mapping | P3 | Medium | Medium | Dict ↔ typed object conversion |
| 11 | Mapping profiles/inheritance | P3 | Medium | Medium-High | Cross-class reuse via `[ForgeIncludes]` |
| 12 | Reverse mapping | P3 | Medium | Medium | Auto-generate bidirectional mappings |
| 13 | Generic forge methods | P3 | High | High | Type parameter support |

---

## Remaining Fixes & Features (P2+)

### 1. Expression Assignment Mutability — `P2`

**Goal:** Refactor `ExpressionAssignment` field from mutable to immutable pattern.

**Issue:** `MemberAssignmentModel.cs:23` has mutable field:
```csharp
public string? ExpressionAssignment { get; set; } // Can be mutated!
```
Mutated in `ForgeGenerator.cs:1338, 1372`:
```csharp
assignment.ExpressionAssignment = inlined; // Late mutation
```

This violates immutability principle and makes caching risky.

**Fix Options:**

1. **Immutable reconstruction** — Create new `MemberAssignmentModel` with updated expression in `ResolveExpressionInlining()`
2. **Separate builder class** — Keep extraction immutable, use mutable builder for inlining
3. **Builder pattern** — Construct assignments with inline expressions already resolved

**Recommended:** Option 1 (immutable reconstruction). Add helper method:
```csharp
public MemberAssignmentModel WithExpressionAssignment(string expr) =>
    new(DestMemberName, SourceExpression, ..., expressionAssignment: expr);
```

**Files to modify:**
- `MemberAssignmentModel.cs` — Change setter to init-only, add `WithExpressionAssignment()` method
- `ForgeGenerator.cs:ResolveExpressionInlining()` — Replace mutations with reconstructions

**Test impact:** No test changes needed (existing tests still pass)

---

### 2. Missing Edge Case Tests — `P2`

**Goal:** Add parameterized tests for extreme scenarios to catch performance cliffs and edge cases.

**Missing scenarios:**

1. **Large member counts** — Methods with 50, 100, 200+ members
   - Why: Risk of O(n²) member matching or string concatenation
   - Test: Benchmark time/memory as member count increases

2. **Deeply nested generics** — `Generic<Generic<Generic<T>>>`
   - Why: Type resolution could fail or produce invalid generated code
   - Test: Verify code generates and compiles correctly

3. **ForgeMap name conflicts** — Two members mapping to same name
   - Why: Should emit FKF105 (duplicate target), not silent overwrite
   - Test: Verify diagnostic is emitted

4. **Expression property suppression** — All members excluded from expression
   - Why: Should skip property emission entirely
   - Test: Verify only imperative method generated, no expression property

5. **NullFallback string boundary case** — Collection with complex nested ternary
   - Why: The string replacement bug (issue #1) could corrupt nested expressions
   - Test: Verify ` : null` at end only, nested ternaries preserved

6. **Circular collection element forge** — Collections with circular element converters
   - Why: FKF301 should catch this
   - Test: Multiple nested collection levels with cycle

**Status:**
- ✅ `tests/FreakyKit.Forge.Generator.Tests/EdgeCaseGeneratorTests.cs` — Created with 3 tests:
  - `LargeMemberCount_GeneratesValidCode` — 50-property classes for O(n²) validation
  - `DeeplyNestedGenerics_GeneratesValidCode` — `List<List<List<int>>>` for type resolution depth
  - `NullFallback_CollectionFallback_UsesVersionCompatibleSyntax` — Verifies fallback uses `Enumerable.Empty<object>()` not C# 12 `[]`

**Remaining scenarios** (can be added as follow-ups):
- ForgeMap name conflicts (FKF105 emission)
- Expression property suppression (all members excluded)
- Circular collection element forge (FKF301 on collections)

---

## Features (P2+)

### 3. Production-Grade Benchmark Suite — `P2`

**Goal:** Expand the benchmark suite beyond synthetic micro-benchmarks to include real-world mapping scenarios sourced from open-source production codebases, giving a more honest picture of performance under realistic conditions.

### Why

The current benchmarks cover well-defined scenarios — simple flat mappings, fixed property counts, controlled object graphs. These are useful for measuring overhead but they don't reflect the messiness of production code: deeply nested graphs, mixed nullable and non-nullable members, enums, collections of varying sizes, partial updates, and mappings that change shape over time. Numbers from synthetic scenarios are easy to dismiss. Numbers from code that actually ships are harder to argue with.

### Design

- Source mapping scenarios from permissively licensed open-source .NET projects (e-commerce, CRM, ERP, API layers) where object mapping is a core concern
- Reproduce the source and destination types as faithfully as possible without pulling in unrelated dependencies
- Run the same scenarios through Forge and any competing library that supports the shape
- Document exactly where each scenario comes from so the benchmark is reproducible and auditable
- Cover a range of real-world shapes: large flat DTOs, deeply nested domain models, collections with hundreds of items, update methods on existing objects, nullable-heavy database entities

### Suggested Approach

1. Identify 5-10 open-source projects with realistic, representative mapping scenarios
2. Extract and reproduce the relevant types in a new `benchmarks/FreakyKit.Forge.Benchmarks.RealWorld/` project
3. Run against the same library versions used in the existing suite
4. Document each scenario with a link to the source project and a description of what it represents
5. Add to CI so results are regenerated on each release

---

## 7. Derived Type / Polymorphic Mapping — `P3`

**Goal:** Map a base type to different destination DTOs based on a discriminator property, supporting EF Core / TPH inheritance hierarchies.

### Why

Applications using Entity Framework with Table-Per-Hierarchy (TPH) inheritance produce query results typed as the base entity. Mapping these to the correct derived DTO requires a runtime type check or discriminator switch. Currently, users must hand-write this dispatch logic.

### Design

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
    // Generates:
    // return source switch
    // {
    //     Dog __dog => MapDog(__dog),
    //     Cat __cat => MapCat(__cat),
    //     _ => MapBase(source)
    // };
}
```

### Variants

- **Type-test dispatch** (above) — pattern match on `source is DerivedType`
- **Discriminator dispatch** — switch on a property value: `[ForgePolymorphic(typeof(DogDto), DiscriminatorValue = "dog")]`
- **Fallback behavior** — configurable: throw, return null, or map as base type

### Complexity

**Medium.** The core challenge is generating a switch expression with type patterns:

- Need to verify that each derived type is actually assignable from the source parameter type
- Need to verify that the referenced forge method exists and has the correct signature
- Ordering matters: more-derived types must come before less-derived types
- The fallback (default arm) needs a clear strategy
- Must work with both create and update method shapes

### Files to Modify

- New attribute: `ForgePolymorphicAttribute.cs` in `FreakyKit.Forge/Attributes/` (with `AllowMultiple = true`)
- `ForgeGenerator.cs` — detect `[ForgePolymorphic]` on a method and generate switch expression instead of normal body
- `ForgeMethodModel.cs` — add `IReadOnlyList<PolymorphicMapping> PolymorphicMappings`
- New analyzer rules: validate derived types are assignable, validate referenced methods exist

### Suggested Approach

1. Start with type-test dispatch (pattern matching) as it's the most common case
2. Generate a switch expression with type patterns
3. Default arm calls the base mapping method or throws `InvalidOperationException`
4. Add discriminator-based dispatch as a later enhancement
5. Add analyzer diagnostics for: unreachable patterns, missing derived types, invalid method references

---

## 8. Computed Properties via `[ForgeComputed]` — `P2`

**Goal:** Allow users to define computed destination properties using type-safe methods on the forge class, rather than string-based expressions.

### Why

Some destination properties don't map 1:1 from a source member — they're derived from multiple source members (e.g., `FullName = FirstName + " " + LastName`). Currently, users must use after-hooks or manually assign these after the forge call.

### Design (Type-Safe Method Approach)

```csharp
[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);

    [ForgeComputed(nameof(PersonDto.FullName))]
    private static string ComputeFullName(Person source)
        => source.FirstName + " " + source.LastName;
}
```

The generator discovers `[ForgeComputed]` methods via Roslyn symbol analysis at compile time and emits a direct method call — no reflection, no string interpolation. The generated code becomes:

```csharp
__result.FullName = ComputeFullName(source);
```

### Why Not String Expressions

A string-based approach like `[ForgeMap(Compute = "source.FirstName + ...")]` was considered but rejected because:
- No IntelliSense or compile-time type checking on the expression
- String escaping issues in attributes
- Source parameter name coupling (dest attribute doesn't know the method's parameter name)
- Facet uses string expressions because its `[Facet(typeof(Source))]` is on the dest type — Forge's architecture (separate forge class) doesn't have that context

### Open Design Questions

- Should the method parameter be the source type, or `(source, dest)` for post-assignment compute?
- Should computed properties participate in constructor mapping?
- How to handle computed properties in update methods?
- Convention-based discovery (e.g., `Compute{PropertyName}`) vs attribute-based?

### Suggested Approach

1. New attribute: `[ForgeComputed]` with `string DestinationMember` constructor parameter
2. Generator validates: return type matches dest property type, parameter is the source type
3. Emit direct call in generated code, after construction but before return
4. Analyzer diagnostic if dest property name doesn't exist or types mismatch

---

## 9. Dictionary Mapping — `P3`

**Goal:** Map between `Dictionary<string, T>` and typed objects by matching dictionary keys to member names.

### Why

Many APIs return data as dictionaries (JSON deserialization, configuration, dynamic data). Being able to forge a typed object from a dictionary (and vice versa) bridges the gap between dynamic and static typing at compile time.

### Design

**Dictionary to object:**

```csharp
[Forge]
public static partial class MyForges
{
    public static partial PersonDto FromDict(Dictionary<string, object> source);
    // Generates:
    // __result.Name = (string)source["Name"];
    // __result.Age = (int)source["Age"];
}
```

**Object to dictionary:**

```csharp
[Forge]
public static partial class MyForges
{
    public static partial Dictionary<string, object> ToDict(PersonDto source);
    // Generates:
    // var __result = new Dictionary<string, object>();
    // __result["Name"] = source.Name;
    // __result["Age"] = source.Age;
    // return __result;
}
```

### Variants

- `Dictionary<string, object>` — requires casting on read, boxing on write
- `Dictionary<string, string>` — requires parsing (`int.Parse`, `bool.Parse`, etc.)
- `IReadOnlyDictionary<string, T>` — read-only source support
- Case-insensitive key matching (opt-in)

### Complexity

**Medium.** The member discovery logic is fundamentally different:

- For dict-to-object: "source members" are the destination type's members (keys are inferred from dest)
- For object-to-dict: "dest members" are the source type's members (keys are inferred from source)
- Type conversion: need cast expressions for `object` values, parse expressions for `string` values
- Construction: dict source uses `new Dictionary<K,V>()` + `Add()` or indexer
- Nested types: `source["Address"]` could be another dictionary or a typed object — need a strategy

### Files to Modify

- `ForgeGenerator.cs` — new detection in `ExtractMethod` for dictionary parameter/return types
- New code path in `GenerateMethodBody` for dictionary construction and indexer access
- `ForgeMethodModel.cs` — add `bool IsDictionaryMapping` or a `MappingMode` enum
- Possibly a new `[ForgeDictionary]` attribute for configuration (key casing, missing key behavior)

### Suggested Approach

1. Start with `Dictionary<string, object>` to typed object (most common case)
2. Generate `(TargetType)source["Key"]` with `ContainsKey` checks
3. Add typed object to `Dictionary<string, object>` (reverse direction)
4. Add `Dictionary<string, string>` with `Parse` calls
5. Add `TryGetValue` option for graceful missing-key handling
6. Later, consider nested dictionary support

---

## 10. Mapping Profiles / Inheritance — `P3`

**Goal:** Allow a forge class to reuse mappings defined in another forge class via an `[ForgeIncludes]` attribute.

### Why

Large projects often have shared base types (e.g., `BaseEntity` with `Id`, `CreatedAt`, `UpdatedAt`) mapped across many forge classes. Currently, each class must redeclare the base mapping or let the generator match by name independently. This leads to duplication and inconsistency.

### Design

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
    // Person : BaseEntity, PersonDto : BaseDto
    // The generator can call BaseForges.ToBaseDto or inline base mappings
}
```

**Option A — Delegate to included class:** Generate a call to the included forge method for the base type members. Simple but creates a runtime dependency between forge classes.

**Option B — Inline included mappings:** Copy the base member assignments into the derived forge method. No runtime dependency but more generated code and harder to implement.

### Complexity

**Medium-high.** The main challenge is cross-class symbol resolution during incremental generation:

- The incremental pipeline currently processes each `[Forge]` class independently
- Including another class requires the pipeline to aggregate data across multiple forge classes
- Need to handle: circular includes, diamond includes, version skew between classes
- Must resolve the included class's forge methods during extraction, which means the included class must be processed first or discovered in the same pass

### Files to Modify

- New attribute: `ForgeIncludesAttribute.cs` in `FreakyKit.Forge/Attributes/`
- `ForgeGenerator.cs` — modify `ExtractForgeClass` to look up included forge classes
- `ForgeClassModel.cs` — add `IReadOnlyList<ForgeMethodModel> IncludedMethods`
- Pipeline may need a `Collect()` + `SelectMany()` step to gather all forge classes before processing

### Suggested Approach

1. Start with Option A (delegate) as it's simpler
2. Only support one level of includes (no recursive includes in v1)
3. Emit a diagnostic if circular includes are detected
4. Later, add Option B as an opt-in for performance-sensitive scenarios

---

## 11. Reverse Mapping — `P3`

**Goal:** Automatically generate a reverse mapping method (Dest → Source) from an existing forward mapping (Source → Dest).

### Why

Many applications need bidirectional mapping — e.g., mapping an entity to a DTO for API responses and mapping the DTO back to an entity for writes. Currently, users must write both methods manually.

### Open Design Questions

- **`[ForgeMap]` renames**: If source `FirstName` is mapped to dest `FullName` via `[ForgeMap("FullName")]`, the reverse must know to map `FullName` back to `FirstName`. Should rename metadata be carried through to the reverse, or should the reverse run its own independent member discovery?
- **One-way properties**: Some properties are intentionally mapped in only one direction (e.g., `CreatedAt` is set on creation but never written back). How to declare one-way exclusions?
- **Scope**: Should reverse be per-method (`[ForgeMethod(GenerateReverse = true)]`) or per-class (`[Forge(GenerateReverses = true)]`)? Per-method is more explicit; per-class reduces boilerplate.
- **Naming convention**: Auto-generate name (e.g., `ToDto` → `FromDto` or `ReverseToDto`) or require explicit `ReverseMethodName`?
- **Partial declaration**: The reverse method has no user-written `partial` declaration. The generator must emit both the declaration and implementation, which is unusual for Roslyn generators.
- **Update reverse**: Should reverse also support update methods (void return, 2 params)?
- **Nested reverse**: If forward uses `AllowNestedForging`, should reverse auto-discover reverse nested forge methods?

### Suggested Approach

1. Decide on scope (per-method recommended for v1)
2. Implement `[ForgeMap]` rename reversal by tracking bidirectional name mappings
3. Add `[ForgeIgnoreReverse]` or similar for one-way properties
4. Start with create methods only (no update reverse in v1)
5. Generate both partial declaration + implementation in the same source file

---

## 7. Circular Forge Detection — `P2`

**Goal:** Emit a build-time error when two forge methods form a recursive cycle (A→B with AllowNestedForging calls B→A with AllowNestedForging, which calls A→B, and so on).

### Why

With `AllowNestedForging = true`, the generator inlines calls to other forge methods for nested member types. If two types mutually reference each other and both directions have forge methods, the generated code will call itself recursively and stack-overflow at runtime. Nothing currently detects this at compile time.

### Design

- Build a directed graph of forge methods: a directed edge from method M to method N exists if N handles a type conversion that M depends on (i.e., a nested member of M's source→dest pair matches N's signature)
- Run DFS cycle detection over the graph
- Emit a new diagnostic (e.g., FKF301) on each method involved in the cycle, listing the cycle path
- Only trigger when `AllowNestedForging = true` on the methods involved — disabled nested forging cannot create cycles

### Suggested Approach

1. In the analyzer, after all forge methods are collected for a class, build the dependency graph
2. Run Tarjan's or a simple DFS-based cycle detection algorithm
3. Report the cycle with a message like: `"Circular nested forge detected: ToDto → ToAddressDto → ToDto. This will stack-overflow at runtime."`
4. Add a diagnostic descriptor FKF301 (Error) in the Nested category

---

## 12. Tri-State `ShareReference` / `IgnoreIfNull` on `[ForgeMap]` — `P2`

**Goal:** Allow per-member `[ForgeMap(ShareReference = false)]` to override a method-level `[ForgeMethod(ShareReference = true)]`.

### Why

Currently `ShareReference` and `IgnoreIfNull` on `[ForgeMap]` are `bool` properties defaulting to `false`. C# compilers omit named attribute arguments that match the default value, so `ShareReference = false` is indistinguishable from "not set" in the attribute metadata. This means explicitly writing `false` on a member cannot override a method-level `true` — the generator falls back to the method-level value in both cases.

The reverse (method=false, member=true) works fine because `true` is non-default and always emitted.

### Impact

Low. Only affects users who set `ShareReference = true` at the method level and try to opt a specific member back out. The default behavior (copy) is the safe one.

### Design

Replace the `bool` properties with a tri-state enum:

```csharp
public enum ForgeOptionalBool
{
    Inherit = 0,  // default — inherit from method/class level
    True = 1,
    False = 2
}

public sealed class ForgeMapAttribute : Attribute
{
    public ForgeOptionalBool ShareReference { get; set; }  // was bool
    public ForgeOptionalBool IgnoreIfNull { get; set; }    // was bool
}
```

### Complexity

**Low** code change, **breaking** API change. Existing user code writing `ShareReference = true` or `IgnoreIfNull = true` would need to change to `ShareReference = ForgeOptionalBool.True`.

### Workaround

Don't set `ShareReference = true` at the method level if you need per-member control. Instead, set `ShareReference = true` individually on each member that should share.

---

## 13. Generic / Open-Generic Forge Methods — `P3`

**Goal:** Support forge methods with type parameters for mapping generic wrapper types like `Result<T>`, `ApiResponse<T>`, or `PagedList<T>`.

### Why

The generator currently rejects methods with type parameters entirely. This forces users to write a separate forge method for every concrete instantiation of a generic wrapper — `MapResultOfPerson`, `MapResultOfOrder`, etc. — which is pure boilerplate when the wrapper mapping logic is identical.

### Design

```csharp
[Forge]
public static partial class MyForges
{
    public static partial PersonDto ToDto(Person source);

    public static partial ApiResponse<TDto> MapResponse<TEntity, TDto>(
        ApiResponse<TEntity> source) where TDto : class;
    // Generates:
    // __result.Data = ToDto(source.Data);  // discovered via nested forge for TEntity → TDto
    // __result.StatusCode = source.StatusCode;
}
```

### Complexity

**High.** The generator must resolve type parameters against concrete nested forge methods, handle constraints, and generate code that remains valid across all instantiations. A reasonable v1 scope would be to support a single type parameter with a known mapping method, rejecting unconstrained or ambiguous cases.

---

## 14. Conditional / Predicate-Based Mapping — `P2`

**Goal:** Extend `IgnoreIfNull` with `IgnoreIfDefault` and custom predicate support for partial-update scenarios.

### Why

`IgnoreIfNull` handles nullable reference types, but PATCH/partial-update APIs often need to skip `0`, `false`, `Guid.Empty`, and other default values that indicate "not provided" rather than "set to zero." Currently there's no way to express this without after-hooks.

### Design

```csharp
[Forge]
public static partial class PatchForges
{
    [ForgeMethod]
    public static partial void ApplyPatch(PatchDto source, ref Entity dest);
}

public class PatchDto
{
    [ForgeMap("Name", IgnoreIfDefault = true)]
    public string? NewName { get; set; }  // skip if null

    [ForgeMap("Priority", IgnoreIfDefault = true)]
    public int NewPriority { get; set; }  // skip if 0

    [ForgeMap("Status", Condition = nameof(ShouldMapStatus))]
    public Status NewStatus { get; set; }  // custom predicate
}
```

`IgnoreIfDefault` wraps the assignment in `if (!EqualityComparer<T>.Default.Equals(source.X, default))`. `Condition` references a static method on the forge class.

### Complexity

**Medium.** `IgnoreIfDefault` is straightforward — similar to `IgnoreIfNull` but with `EqualityComparer<T>.Default`. Custom predicates require method resolution and signature validation similar to `[ForgeConverter]`.

---

## 15. Multi-Level Deep Flattening — `P2`

**Goal:** Extend `AllowFlattening` to support 2+ levels of nesting (e.g., `source.Customer.BillingAddress.PostalCode` → `dest.CustomerBillingAddressPostalCode`).

### Why

Current flattening is limited to one level of nesting. Real-world domain models in ERP, CRM, and e-commerce frequently have deeper hierarchies where three or four levels of nesting must be flattened into a single DTO property.

### Design

Extend `TryResolveFlattenedMapping` to recursively walk prefix matches. For `dest.CustomerBillingAddressPostalCode`:
1. Find source member `Customer` → type has `BillingAddress`
2. `BillingAddress` type has `PostalCode` → match

Each intermediate step adds null-safety: `source.Customer?.BillingAddress?.PostalCode`. For expression trees, convert to chained ternaries.

### Complexity

**Medium.** The prefix-matching algorithm needs to handle ambiguity (what if `CustomerBilling` is also a member?) and performance (avoid exponential prefix splits). A reasonable approach is greedy longest-prefix matching with a configurable depth limit.

---

## 16. Cross-Class Nested Forge Method Discovery — `P2`

**Goal:** Allow `AllowNestedForging` to discover forge methods in other `[Forge]`-decorated classes, not just the current one.

### Why

Nested forging currently only searches the containing forge class. If `AddressForges.ToDto(Address)` is in a separate class from `PersonForges`, the nested forge for the `Address` member won't be found. This forces users to either duplicate methods or consolidate everything into one large class, both of which scale poorly.

### Design

```csharp
[Forge]
[ForgeUses(typeof(AddressForges))]
public static partial class PersonForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonDto ToDto(Person source);
    // Discovers AddressForges.ToDto(Address) for source.Home / source.Work
}
```

Alternatively, the generator could automatically scan all `[Forge]` classes in the compilation — but this creates implicit coupling and makes the incremental pipeline harder to scope.

### Complexity

**Medium-high.** The incremental pipeline currently processes each forge class independently. Cross-class discovery requires either a `Collect()` + `Combine()` step in the pipeline, or explicit `[ForgeUses]` references that can be resolved during single-class extraction. The explicit approach is simpler and more predictable.

---

## 13. Null-Object Fallback for Nested Forging — `P1`

**Goal:** Allow configuring what happens when a source member is `null` during nested forging — construct a default destination object instead of returning `null`.

### Why

When `AllowNestedForging` is on and a source member is null, the generator emits `source.Home != null ? ToAddressDto(source.Home) : null`. But many API contracts require non-null sub-objects in responses. Users currently need after-hooks or manual null-coalescing at every call site.

### Design

```csharp
public class Source
{
    [ForgeMap("Address", NullFallback = NullFallback.DefaultConstruct)]
    public Address? Home { get; set; }
}
// Generates: source.Home != null ? ToAddressDto(source.Home) : new AddressDto()
```

`NullFallback` enum: `Null` (default, current behavior), `DefaultConstruct` (new), `Throw` (emit `?? throw new`).

### Complexity

**Low.** The ternary is already generated — only the fallback arm changes. The main validation is ensuring the destination type has a parameterless constructor when `DefaultConstruct` is selected.
