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
| 10 | Generic forge methods | P3 | High | High | Type parameter support |
| 19 | dotnet new template | P3 | Medium | Medium | `dotnet new forge-mapper` scaffold |
| 20 | EF Core integration sample | P3 | Medium | Medium | Full API → EF Core → DTO pipeline sample project |
| 22 | Eliminate silent skips | P2 | High | Medium | Add diagnostics for every silent skip in the generator |

> **Completed (removed from backlog):** #6 Polymorphic mapping, #8 Mapping profiles/inheritance (`[ForgeIncludes]`, FKF533–538), #11 CHANGELOG.md, #12 NuGet discoverability, #13 GitHub issue templates, #14 Code coverage CI, #15 Roslyn code fix providers (FKF003, FKF004, FKF002, FKF300), #17 Migration guides (AutoMapper, Mapperly, Mapster, Facet), #18 Project config files (global.json + .editorconfig), #15a Code fix providers (FKF109, FKF112, FKF525, FKF526), #16 Expand samples project (6 new samples: dictionary, projection, strict, ForgeUses, conditional, ShareReference), Orphaned attributes validation (FKF527/FKF528 now inspect forge method signatures), #21 .NET 10 benchmarks (core + 8 real-world scenarios, multi-TFM)

---

## P2 Fixes — Eliminate Silent Skips

### 22. Eliminate Silent Skips — `P2`

**Type:** Fix — Diagnostic Coverage

**Why**

The generator silently skips members and methods in several places without emitting diagnostics. When a user's mapping doesn't produce the output they expect, the absence of any diagnostic makes debugging extremely difficult. Every observable decision the generator makes should be communicated to the user via a diagnostic at the appropriate level (Error, Warning, or Info).

**Rule:** The generator must NEVER silently skip anything that affects user-observable output. Every skip needs a diagnostic. This applies to the **generator** specifically — some diagnostics (FKF002, FKF010, FKF100, FKF300) are intentionally deferred to the **analyzer**, which is co-deployed in the same NuGet package. The guarantee is: between the generator and the analyzer combined, the user always sees feedback for every skipped member or method. No silent drops across the full toolchain.

**Audit Results**

Full audit of ForgeGenerator.cs (5589 lines) identified the following silent skips, grouped by priority:

**HIGH PRIORITY (user-surprising, should fix first):**

| Location | Issue | Level |
|----------|-------|-------|
| Line ~289 | `[ForgeMethod]` on wrong-shape method silently ignored | Warning |
| Line ~593 | Non-`INamedTypeSymbol` source/dest type silently drops entire method | Error |
| Line ~1891 | Malformed `[ForgePolymorphic]` attributes silently skipped | Error |
| Line ~3490 | Flattening name match with type mismatch silently skipped | Warning |
| Line ~3205 | Expression property silently suppressed due to non-translatable ctor arg | Warning |
| Line ~5398 | Profile method extraction errors silently suppress inheritance | Warning |

**MEDIUM PRIORITY (less common, but user-observable):**

| Location | Issue | Level |
|----------|-------|-------|
| Line ~687 | Read-only destination members silently skipped (no FKF107 from generator) | Info |
| Line ~693 | Init-only members silently skipped in update methods (no diagnostic) | Info |
| Line ~3293 | Inaccessible members silently excluded from member collection | Info |
| Line ~3299 | Dest properties with inaccessible setter silently excluded | Info |
| Line ~2425 | Nested method not in lookup silently drops expression member | Info |
| Line ~2428 | Non-create nested method silently drops expression member | Info |
| Line ~2444 | Nested ctor arg not translatable silently drops expression member | Info |

**LOW PRIORITY (defensive guards, edge cases):**

| Location | Issue | Level |
|----------|-------|-------|
| Line ~5347 | Profile class resolution failure (defensive, unlikely) | Warning |
| Line ~1876 | Included class resolution failure during polymorphic collection | Warning |
| Line ~3230 | All expression members excluded, property suppressed entirely | Info |
| Line ~683 | Constructor-consumed members not reported | Info |

**Notes:**
- Some skips are intentionally deferred to the analyzer (FKF002, FKF010, FKF100, FKF300). These are acceptable IF the analyzer is always co-deployed with the generator. Consider whether the generator should also emit these for standalone usage.
- Line numbers are approximate — they shift as the file is edited. Search for the code patterns instead.
- Medium-priority items on read-only (FKF107) and init-only in update context may already be partially covered by the analyzer but not by the generator.

**Files to Modify**

- `src/FreakyKit.Forge.Diagnostics/ForgeDiagnostics.cs` — Add ~13 new diagnostic descriptors
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Replace each `continue`/`return` with diagnostic + skip
- `src/FreakyKit.Forge.Analyzers/ForgeAnalyzer.cs` — Register new diagnostics in `SupportedDiagnostics`
- Tests for every new diagnostic (positive + negative cases)

---

## P3 Features — Backlog

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



