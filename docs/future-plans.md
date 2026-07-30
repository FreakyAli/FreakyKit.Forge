# Future Plans

Features and fixes under consideration for future versions of FreakyKit.Forge. Each section includes enough detail to serve as a starting point for implementation.

## Type Classification

Every item in this document is classified by **Type** to help new contributors understand the nature and priority of each item:

| Type | Meaning | Impact | Examples |
|------|---------|--------|----------|
| **Feature** | New capability or attribute enhancement that extends Forge's functionality beyond current scope | Additive — users gain new mapping options or workflow capabilities | Polymorphic Mapping, Reverse Mapping, Tri-State ShareReference, Generic Methods |
| **Fix** | Correctness issue, missing validation, behavior bug, or limit enforcement affecting existing features | Blocking/Correctness — existing code may behave incorrectly; users may hit unvalidated edge cases | Circular ForgeUses Detection, Expression Nesting Depth Enforcement, Per-Member Accessibility Validation |
| **Documentation** | Gaps in docs, inaccurate examples, or unclear explanations that confuse users or new contributors | Clarity — improves developer experience and onboarding | Flattening Ambiguity Examples, Attribute Feature Interaction Matrix, Diagnostic Code Reference |
| **Test** | Test coverage gaps, missing edge-case validation, or test infrastructure improvements | Regression Prevention — ensures features stay correct as codebase evolves | ConditionalMappingTests Coverage, Null Handling Edge Cases, Compilation Error Checking |

**How to use this guide:**
- **Fixes** should be prioritized over Features if they block users or cause incorrect behavior
- **Documentation** updates should accompany Features (you document what the feature does)
- **Test** additions should follow any Feature or Fix (validate that it works and stays working)
- New contributors reading this table can quickly understand: "This item is a Feature (additive, nice-to-have)" vs "This is a Fix (we should do this soon)"

**Pending Removal (marked for deprecation):**
- FKF507 diagnostic descriptor — kept for backward compatibility in v1.x, scheduled for removal in v2.0

## Priority Matrix

Each feature is prioritized using an **Impact × Effort** matrix:

- **P1 — Do first.** Critical bugs or high-impact features with low-to-medium implementation effort. Correctness, security, and infrastructure issues belong here.
- **P2 — Do next.** Either high-impact features requiring significant effort, or moderate-impact fixes/features with low effort.
- **P3 — Backlog.** Valuable features with open design questions, high complexity, or niche use cases. Need more design work before implementation.

| # | Feature | Priority | Impact | Effort | Notes |
|---|---------|----------|--------|--------|-------|
| 6 | Polymorphic mapping | P3 | Medium | Medium | EF Core TPH inheritance |
| 8 | Mapping profiles/inheritance | P3 | Medium | Medium-High | Cross-class reuse via `[ForgeIncludes]` |
| 9 | Reverse mapping | P3 | Medium | Medium | Auto-generate bidirectional mappings |
| 10 | Generic forge methods | P3 | High | High | Type parameter support |

---

## P3 Features — Backlog

### 6. Polymorphic Mapping / Derived Type Support — `P3`

**Type:** Feature — EF Core Integration

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
    public static partial DogDto MapDog(Dog source);  // returns DogDto : AnimalDto
    public static partial CatDto MapCat(Cat source);  // returns CatDto : AnimalDto

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
6. Generate switch expression: order patterns by inheritance depth (derived first)
7. Default arm calls base mapping or throws `InvalidOperationException`
8. Add analyzer diagnostics: unreachable patterns, missing methods, type mismatches

---

### 8. Mapping Profiles / Inheritance — `P3`

**Type:** Feature — Code Reuse

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

### 9. Reverse Mapping — `P3`

**Type:** Feature — Mapping Automation

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

### 10. Generic Forge Methods — `P3`

**Type:** Feature — Type Parameterization

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
4. If nested `Data` contains a `Company`, the generator looks for a **dedicated mapper** (e.g., `MapCompany`) for `Company -> CompanyDto`
   - If no dedicated mapper exists, emits FKF diagnostic and excludes the nested member
   - The generated call becomes `MapCompany(nestedData)`, NOT a generic wrapper like `MapResponse<Company, CompanyDto>`
5. Constraint validation ensures mapped types satisfy any where clauses on the dedicated mapper

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

### Medium-Priority Bugs


### Low-Priority Issues

#### Algorithm Enhancement: Exhaustive Flattening Candidate Collection

**Why**

`TryResolveFlattenedMappingRecursive` uses a greedy algorithm: it returns the first flattened path found, even if multiple equally-valid paths exist at the same depth. This means `Customer.Address.City` matching both `AddressCity` (direct property) and `ContactAddress.City` (flattened) returns whichever is discovered first in the member iteration order, with no ambiguity detection or longest-prefix selection. FKF530 (ambiguous flattening) never triggers because the code returns before evaluating all candidates.

**Design**

Refactor to collect ALL valid flattened candidates up to `MaxFlatteningDepth`, then apply a selection strategy:
1. Filter to longest-prefix matches
2. If multiple candidates remain at the same depth, emit FKF530 (ambiguous flattening) per the existing handler
3. If `depth > MaxFlatteningDepth`, propagate an explicit depth-overflow result so the caller can emit FKF532 instead of falling through to FKF100

**Complexity**

High. Requires restructuring the recursion to accumulate rather than return-early, then post-processing the results.

**Impact**

Low-Medium. Closes a gap where ambiguities go undetected and depth limits are silently ignored. Mostly affects edge cases in deeply-nested or ambiguous source structures.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — TryResolveFlattenedMappingRecursive (line 3055) and TryResolveFlattenedMapping (line 3009)

**Suggested Approach**

1. Change `TryResolveFlattenedMappingRecursive` to return a list of `FlatteningResult` candidates instead of the first match
2. Accumulate all matches at all depths up to `MaxFlatteningDepth`
3. In `TryResolveFlattenedMapping`, post-process the list to select longest matches
4. If `depth == MaxFlatteningDepth + 1`, include a sentinel `result.IsDepthOverflow = true`
5. Invoke existing FKF530 logic when multiple candidates remain; invoke FKF532 logic when depth overflow is detected
6. Add tests covering ambiguous flattening and depth-limit scenarios

---


## Implementation Issues & Code Quality Refinements

Fine-grained fixes, algorithmic improvements, test coverage gaps, and consistency issues identified during code audit. Organized by subsystem.

### Generator Correctness & Behavior

#### Exhaustive Flattening Candidate Collection

**Type:** Fix — Algorithm Enhancement

**Why**

`TryResolveFlattenedMappingRecursive` uses greedy-first-match and returns before evaluating all valid paths. FKF530 (ambiguous flattening) never triggers; depth overflow silently returns "not found" instead of emitting FKF532.

**Complexity**

High. Restructure recursion to accumulate all candidates, post-process for longest-match selection.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — TryResolveFlattenedMappingRecursive, TryResolveFlattenedMapping

---

#### Orphaned Attributes Validation Redesign

**Type:** Fix — Diagnostic Accuracy

**Why**

FKF527 (ForgeMap on source) and FKF528 (ForgeIgnore on source) fire based on member type alone, not actual role. They should only emit when the attribute is truly ineffective for that context.

**Complexity**

High. Inspect forge method signatures to determine actual source/destination roles; emit diagnostics only when ineffective.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — DetectForgeMapOnSourceMember, DetectForgeIgnoreOnSourceMember
- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — FKF527/FKF528 descriptors
- `docs/attributes.md` — Update validation documentation

---

### Generator Expression Trees & Flattening


### Test Coverage Gaps

#### FlatteningGeneratorTests Coordinate Mapping Assertion

**Type:** Test — Coverage

**Why**

Test asserts flattened City but not the proper null-propagation for intermediate properties (Address?.Coords should use ?., Latitude should use .).

**Complexity**

Low. Add assertion verifying correct operator usage in generated path.

**Files to Modify**

- `tests/FreakyKit.Forge.Generator.Tests/FlatteningGeneratorTests.cs`

---

---

### Documentation Accuracy

