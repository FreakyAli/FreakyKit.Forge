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
| 11 | CHANGELOG.md | P1 | High | Low | Backfill from git tags v1.0.0–v1.5.0 |
| 12 | NuGet discoverability | P1 | High | Low | PackageTags on all packages + PackageReleaseNotes |
| 13 | Customize GitHub issue templates | P1 | Medium | Low | Replace generic browser/smartphone template with .NET-specific fields |
| 14 | Code coverage CI | P2 | High | Medium | Coverlet + codecov badge |
| 15 | Roslyn code fix providers | P2 | High | High | Lightbulb suggestions for common diagnostics |
| 16 | Expand samples project | P2 | Medium | Low-Medium | Dictionary, EF Core, conditional mapping, ForgeUses, ShareReference |
| 17 | AutoMapper migration guide | P2 | High | Medium | Side-by-side migration doc targeting AutoMapper users |
| 18 | Project config files | P2 | Low | Low | global.json + .editorconfig |
| 19 | dotnet new template | P3 | Medium | Medium | `dotnet new forge-mapper` scaffold |
| 20 | EF Core integration sample | P3 | Medium | Medium | Full API → EF Core → DTO pipeline sample project |
| 21 | .NET 10 benchmarks | P3 | Low | Medium | Fill TODO placeholders in benchmarks.md |

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

---

## Adoption & Project Health

Items focused on discoverability, trust signals, and developer experience — the things that determine whether a new user evaluates Forge or closes the tab.

### 11. CHANGELOG.md — `P1`

**Type:** Infrastructure

**Why**

Seven stable releases (v1.0.0, v1.0.1, v1.3.0, v1.3.1, v1.4.0, v1.4.1, v1.5.0) and two pre-releases (v1.2.0-pre, v1.3.1-pre) exist with no release notes in the repository. Users evaluating Forge for production use need to know what changed between versions, whether upgrading is safe, and what the release cadence looks like. A missing changelog is a red flag for cautious teams.

**Design**

Use [Keep a Changelog](https://keepachangelog.com/) format. Backfill from git history for all existing tags. Going forward, update the changelog as part of every release.

**Complexity**

Low. Run `git log --oneline v1.0.0..v1.1.0` (etc.) for each tag range and categorize changes into Added/Changed/Fixed/Removed.

**Impact**

High. Table-stakes for any library asking teams to take a production dependency. Also enables `PackageReleaseNotes` in NuGet (item 12).

**Suggested Approach**

1. Create `CHANGELOG.md` at repo root
2. For each tag pair, extract commits and categorize
3. Add a link from README.md to the changelog
4. Update CI release workflow to remind/enforce changelog entry before tagging

---

### 12. NuGet Discoverability — `P1`

**Type:** Infrastructure

**Why**

The Generator and Analyzers packages (the ones people actually install) have zero NuGet `PackageTags`, making them hard to find on nuget.org via search. No package has `PackageReleaseNotes`, which leaves the NuGet page's release notes section blank.

**Design**

Add `PackageTags` to `Directory.Build.props` (shared across all packages) and `PackageReleaseNotes` pointing to the changelog.

**Complexity**

Low. A few XML lines in `Directory.Build.props`.

**Impact**

High. NuGet search is the primary discovery channel for .NET libraries.

**Files to Modify**

- `src/Directory.Build.props` — Add `PackageTags` and `PackageReleaseNotes`

**Suggested Approach**

1. Add to `Directory.Build.props`:
   - `<PackageTags>mapping;source-generator;roslyn;codegen;object-mapper;dto;compile-time</PackageTags>`
   - `<PackageReleaseNotes>See https://github.com/FreakyAli/FreakyKit.Forge/blob/master/CHANGELOG.md</PackageReleaseNotes>`
2. Remove the duplicate `PackageTags` from `src/FreakyKit.Forge/FreakyKit.Forge.csproj` (it currently has its own tag set)

---

### 13. Customize GitHub Issue Templates — `P1`

**Type:** Infrastructure

**Why**

The current bug report template is the default GitHub template — it asks for "Browser", "Smartphone", "iOS" which are irrelevant for a .NET library. This signals to contributors that the project isn't actively maintained or curated.

**Complexity**

Low. Replace the template YAML/markdown files.

**Impact**

Medium. First-touch experience for bug reporters. Signals project maturity.

**Files to Modify**

- `.github/ISSUE_TEMPLATE/bug_report.md`
- `.github/ISSUE_TEMPLATE/feature_request.md`

**Suggested Approach**

Replace the bug report template with fields relevant to Forge:
- Forge version
- .NET SDK version (`dotnet --version`)
- Target framework
- Minimal reproduction code (source types + forge class + expected vs actual behavior)
- Diagnostic output (if applicable)
- IDE/build tool (VS, Rider, `dotnet build`)

---

### 14. Code Coverage CI — `P2`

**Type:** Infrastructure

**Why**

616 tests exist but there's no visibility into what percentage of the codebase they cover. A 90%+ coverage badge is a strong trust signal for potential adopters. It also catches coverage regressions early.

**Design**

Add Coverlet to test projects, configure CI to collect coverage and upload to codecov (or similar), add badge to README.

**Complexity**

Medium. Coverlet integration is straightforward; the main work is configuring the CI workflow and handling the coverage merge across 4 test projects.

**Impact**

High. Coverage badge is one of the first things experienced developers look for.

**Files to Modify**

- `tests/*/\*.csproj` — Add Coverlet package reference
- `.github/workflows/test.yml` — Add coverage collection and upload steps
- `README.md` — Add coverage badge

**Suggested Approach**

1. Add `coverlet.collector` to each test project
2. Run `dotnet test` with `--collect:"XPlat Code Coverage"` and `--results-directory`
3. Use `reportgenerator` to merge coverage from all 4 test projects
4. Upload merged report to codecov via `codecov/codecov-action`
5. Add `![codecov](https://codecov.io/gh/FreakyAli/FreakyKit.Forge/branch/master/graph/badge.svg)` to README

---

### 15. Roslyn Code Fix Providers — `P2`

**Type:** Feature — Developer Experience

**Why**

Forge has 77 diagnostics that tell you what's wrong, but none that offer to fix it. Roslyn code fix providers (lightbulb suggestions) are what developers remember and recommend. Even 5-10 code fixes for the most common diagnostics would be a major DX win and a differentiator over Mapperly.

**Design**

Create a `CodeFixProvider` class per diagnostic (or grouped by related diagnostics). Register via `[ExportCodeFixProvider]`. Ship alongside the analyzer in the same NuGet package.

**Complexity**

High. Each code fix needs its own `CodeAction` implementation with syntax rewriting. The infrastructure (base class, test helpers) is medium effort; each individual fix is low-medium.

**Impact**

High. This is the kind of feature that generates word-of-mouth. "It not only tells you what's wrong, it fixes it for you."

**Files to Modify**

- `src/FreakyKit.Forge.Analyzers/CodeFixes/` — New directory for code fix providers
- `tests/FreakyKit.Forge.Analyzers.Tests/CodeFixes/` — Tests for each code fix

**Suggested Approach**

Start with the highest-value, lowest-complexity fixes:
1. **FKF001** (class not static) → Add `static` modifier
2. **FKF002** (class not partial) → Add `partial` modifier
3. **FKF020** (method not partial) → Add `partial` modifier
4. **FKF100** (unmapped member) → Add `[ForgeIgnore]` to the member, or offer to add `[ForgeMap("CorrectName")]`
5. **FKF300** (nested forging disabled) → Add `AllowNestedForging = true` to `[ForgeMethod]`

Each fix follows the pattern: detect diagnostic → compute syntax edit → register `CodeAction`.

---

### 16. Expand Samples Project — `P2`

**Type:** Documentation

**Why**

The samples project covers 15 features but is missing examples for several advanced capabilities: dictionary mapping, EF Core projections, strict mapping, ForgeUses cross-class sharing, conditional mapping (Condition/IgnoreIfNull/IgnoreIfDefault), and ShareReference. These are features that have docs coverage but no runnable sample code.

**Complexity**

Low-Medium. Each sample is a self-contained file following the existing pattern.

**Impact**

Medium. Samples are the fastest way for a new user to understand a feature. Copy-paste beats reading docs.

**Files to Modify**

- `samples/FreakyKit.Forge.Samples/Forges/` — New sample files
- `samples/FreakyKit.Forge.Samples/Models/` — Additional model types as needed
- `samples/FreakyKit.Forge.Samples/Program.cs` — Wire up new demos

**Suggested Approach**

Add these sample files:
1. `DictionaryForges.cs` — Dictionary-to-object mapping with key casing policies
2. `ProjectionForges.cs` — EF Core expression projection demo
3. `StrictMappingForges.cs` — `StrictMapping = true` with drift detection
4. `ForgeUsesForges.cs` — Cross-class method sharing via `[ForgeUses]`
5. `ConditionalForges.cs` — `IgnoreIfNull`, `IgnoreIfDefault`, `Condition`
6. `ShareReferenceForges.cs` — Reference semantics for same-type collections

---

### 17. AutoMapper Migration Guide — `P2`

**Type:** Documentation

**Why**

AutoMapper is the most widely used .NET mapping library. Many teams are looking to migrate away from reflection-based mappers. A side-by-side migration guide is high-value content that captures developers at the moment they're actively looking for alternatives. This is how Mapperly grew its user base.

**Design**

A standalone doc (`docs/migrate-from-automapper.md`) with before/after code for the 10-15 most common AutoMapper patterns, showing the Forge equivalent. Link from README.

**Complexity**

Medium. Requires understanding AutoMapper's API surface well enough to map patterns accurately.

**Impact**

High. Targets the largest pool of potential adopters at their moment of highest intent.

**Files to Modify**

- `docs/migrate-from-automapper.md` — New file
- `README.md` — Add link in the "Why Forge?" section
- `llms.txt` — Add migration reference
- `docs/patterns.md` — Cross-reference where applicable

**Suggested Approach**

Cover these AutoMapper patterns:
1. `CreateMap<TSource, TDest>()` → `[Forge]` + method signature
2. `.ForMember(dest => dest.X, opt => opt.MapFrom(src => src.Y))` → `[ForgeMap("Y")]`
3. `.Ignore()` → `[ForgeIgnore]`
4. `.ReverseMap()` → Two separate forge methods (explicit is safer)
5. `mapper.Map<TDest>(source)` → `ForgeClass.ToDto(source)` or `source.ToDto()`
6. Nested object mapping → `AllowNestedForging = true`
7. Collection mapping → Forge handles this automatically
8. Constructor mapping → Forge selects constructors automatically
9. Null substitution → `DefaultValue` on `[ForgeMap]`
10. Conditional mapping → `IgnoreIfNull`, `Condition`
11. Custom value resolvers → `[ForgeConverter]`
12. Profile inheritance → `[ForgeUses]`

---

### 18. Project Config Files — `P2`

**Type:** Infrastructure

**Why**

No `global.json` means contributors can build with any SDK version, leading to "works on my machine" issues. CONTRIBUTING.md says ".NET 8.0 SDK" but CI uses .NET 9. No `.editorconfig` means no enforced code style despite the CONTRIBUTING.md mentioning style guidelines.

**Complexity**

Low. Two small config files.

**Impact**

Low. Primarily affects contributors, not end users.

**Files to Modify**

- `global.json` — New file at repo root
- `.editorconfig` — New file at repo root

**Suggested Approach**

1. `global.json`: Pin to the SDK version CI uses (currently 9.0.x) with `rollForward: latestFeature`
2. `.editorconfig`: Use the standard .NET/C# conventions (`dotnet_style_*`, `csharp_style_*`), matching the project's existing style (4-space indent, no `this.` qualifier, `var` where type is apparent)

---

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

