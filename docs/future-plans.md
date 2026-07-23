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
| 7 | Dictionary mapping | P3 | Medium | Medium | Dict ↔ typed object conversion |
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
4. Generate switch expression: order patterns by inheritance depth (derived first)
5. Default arm calls base mapping or throws `InvalidOperationException`
6. Add analyzer diagnostics: unreachable patterns, missing methods, type mismatches

---

### 7. Dictionary Mapping — `P3`

**Type:** Feature — Mapping Type Support

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

2. **Dictionary<string, string>**: Parse or convert with proper error handling
   - Missing key: guarded by ContainsKey; use MissingKeyBehavior (Throw, UseDefault)
   - Parse failures: when value is present but malformed, throws `FormatException`; not caught by MissingKeyBehavior
   - Primitive types: `int.Parse()`, `bool.Parse()`, `decimal.Parse()`, etc.
   - Enum types: `Enum.Parse<EnumType>(value)`
   - DateTime: requires explicit culture and styles, e.g. `DateTime.Parse(value, CultureInfo.InvariantCulture)` or `DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)`
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

#### Flattening Members Silently Excluded From Expression Properties

**Type:** Fix — Correctness

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


### Low-Priority Issues

#### Expression Nesting Depth Limit Not Enforced

**Type:** Fix — Limit Enforcement

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

#### Diagnostic Aggregation Masks Primary Errors

**Type:** Fix — Error Reporting

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

#### Missing Validator: [ForgeConverter] Discoverability

**Type:** Fix — Validation

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

#### Correctness: Qualified Method Names for Cross-Class Converters

**Why**

When `FindNestedForgeMethod` or `FindConverterMethod` discovers a method in an `[ForgeUses]` included class, it returns a bare method name (e.g., `ConvertAddress`). However, `GenerateSource` does not emit `using static` imports for included classes, so the generated code compiles only if the method is in the same namespace. In different-namespace scenarios, generated code references an unqualified method that doesn't exist in scope, causing runtime compilation failures.

**Design**

Modify `FindNestedForgeMethod` and `FindConverterMethod` to return class-qualified method names for methods found in included classes (e.g., `AddressForges.ConvertAddress`). Keep bare names for methods found in the current forge class (which is always in scope).

**Complexity**

Medium. Requires:
1. Tracking which class each discovered method came from
2. Returning qualified names for included-class methods
3. Updating all ~10 call sites to handle qualified names in expression generation

**Impact**

Medium. Fixes a correctness bug that causes generated code to fail in cross-namespace cross-class converter scenarios.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — FindNestedForgeMethod (lines 2583-2665), FindConverterMethod (lines 2709-2760), and all call sites using the returned method names

**Suggested Approach**

1. Return a tuple `(methodName: string, includeClassName: string?)` from both lookup methods
2. When method is from included class, set `includeClassName` to the simple class name (e.g., `AddressForges`)
3. At call sites, use `$"{includeClassName}.{methodName}"` when `includeClassName` is not null
4. Add test: verify generated code uses `AddressForges.ConvertAddress(...)` when converter is in included class

---

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

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — TryResolveFlattenedMappingRecursive (lines 2444-2537) and TryResolveFlattenedMapping (lines 2404-2437)

**Suggested Approach**

1. Change `TryResolveFlattenedMappingRecursive` to return a list of `FlatteningResult` candidates instead of the first match
2. Accumulate all matches at all depths up to `MaxFlatteningDepth`
3. In `TryResolveFlattenedMapping`, post-process the list to select longest matches
4. If `depth == MaxFlatteningDepth + 1`, include a sentinel `result.IsDepthOverflow = true`
5. Invoke existing FKF530 logic when multiple candidates remain; invoke FKF532 logic when depth overflow is detected
6. Add tests covering ambiguous flattening and depth-limit scenarios

---

#### Documentation Gap: Attribute Feature Interaction Matrix

**Type:** Documentation — Guidance

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

1. List all collection types checked by `IsMutableSameTypeCollection()`: List<T>, HashSet<T>, Dictionary<K,V>, etc.
2. Document which types trigger `ShareReference` behavior
3. Clarify which types are always copied (IEnumerable, IList, etc.)

---

## Implementation Issues & Code Quality Refinements

Fine-grained fixes, algorithmic improvements, test coverage gaps, and consistency issues identified during code audit. Organized by subsystem.

### Generator Correctness & Behavior

#### Qualified Method Names for Cross-Class Converters

**Type:** Fix — Correctness

**Why**

When `FindNestedForgeMethod` or `FindConverterMethod` discovers a method in an `[ForgeUses]` included class, it returns a bare method name. However, `GenerateSource` does not emit `using static` imports for included classes, so generated code in different-namespace scenarios fails at compile time.

**Complexity**

Medium. Update both lookup methods to return class-qualified names for included-class methods; update all call sites.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — FindNestedForgeMethod, FindConverterMethod, and ~10 call sites

---

#### Exhaustive Flattening Candidate Collection

**Type:** Fix — Algorithm Enhancement

**Why**

`TryResolveFlattenedMappingRecursive` uses greedy-first-match and returns before evaluating all valid paths. FKF530 (ambiguous flattening) never triggers; depth overflow silently returns "not found" instead of emitting FKF532.

**Complexity**

High. Restructure recursion to accumulate all candidates, post-process for longest-match selection.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — TryResolveFlattenedMappingRecursive, TryResolveFlattenedMapping

---

#### Expression Nesting Depth Enforcement

**Type:** Fix — Limit Enforcement

**Why**

FKF508 (deep nesting) warns at depth 5+ but allows arbitrary nesting. No hard limit; very deep expressions generate megabytes of code causing unclear compiler errors.

**Complexity**

Medium. Add depth check before inlining; emit FKF509 error when exceeded.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Expression inlining loops

---

#### Circular ForgeUses Detection with Cycle Reporting

**Type:** Fix — Validation

**Why**

Current self-include check doesn't detect transitive cycles (A → B → A). Cycles cause infinite recursion in lookup logic.

**Complexity**

Medium. Track recursion stack during validation; report full cycle chain in diagnostic.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — ExtractAndValidateForgeUses

---

#### Conditional Metadata Refactoring

**Type:** Fix — Code Organization

**Why**

Conditional handling (IgnoreIfNull, IgnoreIfDefault, ConditionMethodName) is built inline in regular assignments only. Update, init-only, and expression assignments lack this logic, causing inconsistent code generation.

**Complexity**

High. Extract conditional wrapping into shared method; apply to all assignment types.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Member assignment generation

---

#### Per-Member Accessibility Validation

**Type:** Fix — Correctness

**Why**

Constructor parameters are validated for type-compatibility but not for accessibility. Private/internal parameters in external types cause runtime compilation failures.

**Complexity**

Low. Check `DeclaredAccessibility` before using constructor parameter.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — DetermineConstruction

---

#### Analyzer-Generator Accessibility Consistency

**Type:** Fix — Consistency

**Why**

ForgeAnalyzer validates converter accessibility (public/internal only) but ForgeGenerator.FindConverterMethod doesn't apply the same filter, causing analyzer and generator to disagree.

**Complexity**

Low. Apply same accessibility predicate to both analyzer validation and generator lookup.

**Files to Modify**

- `src/FreakyKit.Forge.Analyzers/ForgeAnalyzer.cs` — validation logic
- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — FindConverterMethod

---

#### Condition Resolution from Included Classes

**Type:** Fix — Feature Completeness

**Why**

Condition method lookup only searches the current forge class. Methods in `[ForgeUses]` included classes are not discoverable, even though nested forge methods from included classes are supported.

**Complexity**

Medium. Extend condition lookup to search included classes; apply accessibility filter.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Condition resolution logic

---

#### Per-Member IgnoreIfNull Precedence

**Type:** Fix — Correctness

**Why**

When a `[ForgeMap]` attribute explicitly specifies IgnoreIfNull, the code OR-combines it with method-level setting instead of respecting explicit configuration. Explicit member values should take precedence.

**Complexity**

Low. Track whether each member's IgnoreIfNull was explicitly set; prefer explicit over inherited.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Member IgnoreIfNull calculation

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

#### Null-Conditional Operator Lowering in Flattening

**Type:** Fix — Expression Trees

**Why**

When flattening paths use expression properties, only the outermost null-conditional is lowered to ternaries. Intermediate `?.` operators remain, causing expression-tree compilation failures.

**Complexity**

Medium. Lower all `?.` in flattening paths to nested ternaries before expression compilation.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Flattening expression lowering

---

#### Flattening Depth Check Accounting

**Type:** Fix — Correctness

**Why**

Depth checks compare absolute depth to thresholds without accounting for zero-based indexing. Paths at threshold level silently fail instead of emitting FKF531 diagnostic.

**Complexity**

Low. Compare effective component count (depth + 1) against thresholds.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — Depth threshold checks

---

#### Receiver Type for Null-Propagating Access

**Type:** Fix — Correctness

**Why**

`nextAccess` construction uses `prop.Type` to determine null-propagation, but should use the receiver type `currentType`. Incorrect operator selection for intermediate properties in chains.

**Complexity**

Low. Check `currentType.IsReferenceType` instead of `prop.Type.IsReferenceType`.

**Files to Modify**

- `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` — nextAccess construction

---

### Test Coverage Gaps

#### ConditionalMappingTests Coverage Expansion

**Type:** Test — Coverage

**Why**

ConditionalMappingTests only asserts "no errors". Should assert that generated conditions are correct and guard assignments as expected, including update methods and expression output.

**Complexity**

Low. Add assertions verifying conditional guard structure and combined conditions.

**Files to Modify**

- `tests/FreakyKit.Forge.Generator.Tests/ConditionalMappingTests.cs`

---

#### CrossClassNestedForgeTests Qualified Names Verification

**Type:** Test — Coverage

**Why**

Test asserts "ConvertAddress" appears in generated code but doesn't verify it's qualified (AddressForges.ConvertAddress). Doesn't detect the correctness issue when namespace differs.

**Complexity**

Low. Assert qualified method name appears in generated output.

**Files to Modify**

- `tests/FreakyKit.Forge.Generator.Tests/CrossClassNestedForgeTests.cs`

---

#### Compilation Error Checking in Test Base

**Type:** Test — Infrastructure

**Why**

`AssertNoErrors` only checks diagnostics, not compilation errors. Generated code could have syntax errors that tests don't catch.

**Complexity**

Low. Assert `result.CompilationDiagnostics` contains no Error-severity entries.

**Files to Modify**

- `tests/FreakyKit.Forge.Generator.Tests/GeneratorTestBase.cs`

---

#### FlatteningGeneratorTests Coordinate Mapping Assertion

**Type:** Test — Coverage

**Why**

Test asserts flattened City but not the proper null-propagation for intermediate properties (Address?.Coords should use ?., Latitude should use .).

**Complexity**

Low. Add assertion verifying correct operator usage in generated path.

**Files to Modify**

- `tests/FreakyKit.Forge.Generator.Tests/FlatteningGeneratorTests.cs`

---

#### NullFallbackAdvancedTests Nullable Value Type Fix

**Type:** Test — Coverage

**Why**

Test uses non-nullable Source.Home but intends to verify nullable-to-non-nullable mapping behavior. Should use nullable value type.

**Complexity**

Low. Change Source.Home to nullable (Address?); assert FKF314 diagnostic.

**Files to Modify**

- `tests/FreakyKit.Forge.Generator.Tests/NullFallbackAdvancedTests.cs`

---

#### CollectionMismatchEdgeCasesTests Assertion Unconditional

**Type:** Test — Coverage

**Why**

Test has conditional assertion based on hasErrors flag. Should unconditionally assert FKF200 diagnostic, ensuring generator properly detects incompatibilities.

**Complexity**

Low. Remove conditional; assert FKF200 always.

**Files to Modify**

- `tests/FreakyKit.Forge.Generator.Tests/CollectionMismatchEdgeCasesTests.cs`

---

### Documentation Accuracy

#### Flattening Ambiguity Examples Correction

**Type:** Documentation — Accuracy

**Why**

Current examples present direct matches as flattening conflicts. Real FKF530 scenarios require multiple valid prefixes at same depth that both map valid nested paths.

**Complexity**

Low. Replace examples with genuine ambiguity cases.

**Files to Modify**

- `docs/attributes.md` — Flattening examples section

---

#### Flattening Example Attributes Completion

**Type:** Documentation — Accuracy

**Why**

Example shows flattening but lacks AllowFlattening = true on [ForgeMethod]; cross-class example lacks [ForgeUses] required for cross-class discovery.

**Complexity**

Low. Add missing attributes to examples.

**Files to Modify**

- `docs/attributes.md` — Flattening examples
