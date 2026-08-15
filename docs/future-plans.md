# Future Plans

Features and fixes under consideration for future versions of Forge. Each section includes enough detail to serve as a starting point for implementation.

## Type Classification

Every item in this document is classified by **Type** to help new contributors understand the nature and priority of each item:

| Type | Meaning | Impact | Examples |
|------|---------|--------|----------|
| **Feature** | New capability or attribute enhancement that extends Forge's functionality beyond current scope | Additive — users gain new mapping options or workflow capabilities | Tri-State ShareReference, Generic Methods |
| **Fix** | Correctness issue, missing validation, behavior bug, or limit enforcement affecting existing features | Blocking/Correctness — existing code may behave incorrectly; users may hit unvalidated edge cases | Circular ForgeUses Detection, Expression Nesting Depth Enforcement, Per-Member Accessibility Validation |
| **Documentation** | Gaps in docs, inaccurate examples, or unclear explanations that confuse users or new contributors | Clarity — improves developer experience and onboarding | Flattening Ambiguity Examples, Attribute Feature Interaction Matrix, Diagnostic Code Reference |
| **Test** | Test coverage gaps, missing edge-case validation, or test infrastructure improvements | Regression Prevention — ensures features stay correct as codebase evolves | ConditionalMappingTests Coverage, Null Handling Edge Cases, Compilation Error Checking |
| **Infrastructure** | CI/CD, NuGet packaging, project configuration, and tooling improvements that affect discoverability or contributor experience | Adoption — removes friction for new users and contributors | CHANGELOG, Code Coverage CI, PackageTags, global.json |

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
| 8 | Mapping profiles/inheritance | P3 | Medium | Medium-High | Cross-class reuse via `[ForgeIncludes]` |
| 10 | Generic forge methods | P3 | High | High | Type parameter support |
| 19 | dotnet new template | P3 | Medium | Medium | `dotnet new forge-mapper` scaffold |
| 20 | EF Core integration sample | P3 | Medium | Medium | Full API → EF Core → DTO pipeline sample project |
| 21 | .NET 10 benchmarks | P3 | Low | Medium | Fill TODO placeholders in benchmarks.md |

> **Completed (removed from backlog):** #6 Polymorphic mapping, #11 CHANGELOG.md, #12 NuGet discoverability, #13 GitHub issue templates, #14 Code coverage CI, #15 Roslyn code fix providers (FKF003, FKF004, FKF002, FKF300), #17 Migration guides (AutoMapper, Mapperly, Mapster, Facet), #18 Project config files (global.json + .editorconfig), #15a Code fix providers (FKF109, FKF112, FKF525, FKF526), #16 Expand samples project (6 new samples: dictionary, projection, strict, ForgeUses, conditional, ShareReference)

---

## P3 Features — Backlog

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

Correctness bugs and usability improvements identified through code audit. Organized by subsystem.

### Exhaustive Flattening Candidate Collection

**Type:** Fix — Algorithm Enhancement

**Why**

`TryResolveFlattenedMappingRecursive` detects ambiguity (FKF530 fires when multiple prefix matches exist), but resolution is still greedy first-match: the `foreach` loop returns immediately on the first successful recursive resolution without comparing against other valid paths. If two paths both resolve successfully, only the first (longest prefix) is used and the second is never attempted.

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

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — TryResolveFlattenedMappingRecursive, TryResolveFlattenedMapping

**Suggested Approach**

1. Change `TryResolveFlattenedMappingRecursive` to return a list of `FlatteningResult` candidates instead of the first match
2. Accumulate all matches at all depths up to `MaxFlatteningDepth`
3. In `TryResolveFlattenedMapping`, post-process the list to select longest matches
4. If `depth == MaxFlatteningDepth + 1`, include a sentinel `result.IsDepthOverflow = true`
5. Invoke existing FKF530 logic when multiple candidates remain; invoke FKF532 logic when depth overflow is detected
6. Add tests covering ambiguous flattening and depth-limit scenarios

---

### Orphaned Attributes Validation Redesign

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

## Adoption & Project Health

Items focused on discoverability, trust signals, and developer experience — the things that determine whether a new user evaluates Forge or closes the tab.

### 19. dotnet new Template — `P3`

**Type:** Feature — Tooling

**Why**

`dotnet new forge-mapper` would scaffold a forge class with the right usings and attributes. This is a low-barrier entry point that shows up in `dotnet new list` searches, improving discoverability.

**Complexity**

Medium. Requires a template package with `.template.config/template.json`, testing, and a separate NuGet package.

**Impact**

Medium. Nice to have for discoverability; not a blocker for adoption.

**Files to Modify**

- `templates/FreakyKit.Forge.Templates/` — New template project
- NuGet packaging for the template

---

### 20. EF Core Integration Sample — `P3`

**Type:** Documentation

**Why**

Expression projections are one of Forge's strongest features but are undersold. A dedicated sample showing a full API controller → EF Core → DTO pipeline with `IQueryable` projections that avoid N+1 queries would demonstrate real-world value that toy examples can't.

**Complexity**

Medium. Requires a small ASP.NET Core project with EF Core, Sqlite, a couple of entities, and API endpoints.

**Impact**

Medium. Could be the basis for a blog post or conference talk demo.

**Files to Modify**

- `samples/FreakyKit.Forge.Samples.EFCore/` — New sample project (ASP.NET Core Minimal API + EF Core + Sqlite)

---

### 21. .NET 10 Benchmarks — `P3`

**Type:** Documentation

**Why**

The benchmarks doc has a ".NET 10" section that is entirely TODO placeholders. Now that .NET 10 has shipped, running and publishing these benchmarks shows the project is actively maintained and keeps the performance claims current.

**Complexity**

Medium. Requires running BenchmarkDotNet on .NET 10 and formatting results.

**Impact**

Low. Nice to have for completeness; the .NET 8 benchmarks are still valid.

**Files to Modify**

- `docs/benchmarks.md` — Fill in .NET 10 results
- `benchmarks/FreakyKit.Forge.Benchmarks/` — May need TFM updates
- `benchmarks/FreakyKit.Forge.Benchmarks.RealWorld/` — May need TFM updates

