# Future Plans

Features under consideration for future versions of FreakyKit.Forge. Each section includes enough detail to serve as a starting point for implementation.

---

## 1. Projection Expressions

**Goal:** Generate `Expression<Func<TSource, TDest>>` methods for use with EF Core / IQueryable LINQ providers.

### Why

Currently, Forge generates imperative mapping code (variable assignments). This works for in-memory mapping but cannot be translated to SQL by EF Core. A projection expression would allow:

```csharp
var dtos = dbContext.People
    .Select(PersonForges.ToDtoExpression)
    .ToListAsync();
```

### Design

- New attribute property: `[ForgeMethod(GenerateExpression = true)]`
- Generates an additional static field or property of type `Expression<Func<TSource, TDest>>`
- Uses `MemberInitExpression` with `MemberBinding` nodes instead of imperative assignments
- Constructor mapping becomes `Expression.New(ctor, args)` + `Expression.MemberInit(...)`
- Naming convention: if method is `ToDto`, expression is `ToDtoExpression`

### Complexity

**High.** This is a completely separate code generation path. Every mapping scenario (direct, nullable, enum, nested, collection, converter, flattening) needs an expression-tree equivalent. Key challenges:

- `Expression.MemberInit` requires all assignments upfront (no sequential statements)
- Nested forging becomes `Expression.Invoke` or inlining the nested expression
- Collection mapping with `.Select()` needs `Expression.Call` on `Queryable.Select`
- Type converters need to be expressed as `Expression<Func<TSrc, TDest>>` themselves
- `IgnoreIfNull` becomes `Expression.Condition` with `Expression.Constant(null)` checks
- Before/after hooks are not compatible with expression trees

### Files to Modify

- `ForgeGenerator.cs` — new `GenerateExpressionBody` method parallel to `GenerateMethodBody`
- `ForgeMethodModel.cs` — add `bool GenerateExpression` property
- `ForgeMethodAttribute.cs` — add `GenerateExpression` property
- New tests for every mapping scenario in expression mode

### Suggested Approach

1. Start with the simplest case: parameterless constructor + direct property assignments
2. Add nullable handling
3. Add enum cast mapping
4. Add constructor mapping
5. Add nested/collection (hardest part)
6. Skip hooks in expression mode (emit a diagnostic if hooks exist + expression mode)

---

## 2. Mapping Profiles / Inheritance

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

## 3. Dictionary Mapping

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

## 4. Derived Type / Polymorphic Mapping

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

## 5. Dictionary Element Conversion

**Goal:** Map between `Dictionary<TKey, TSource>` and `Dictionary<TKey, TDest>` with automatic element conversion using existing forge methods.

### Why

Common in API responses, configuration systems, and caching layers where data is stored as keyed collections. For example, mapping `Dictionary<string, OrderEntity>` to `Dictionary<string, OrderDto>`.

### Design

```csharp
[Forge]
public static partial class MyForges
{
    public static partial OrderDto MapOrder(OrderEntity source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial Dictionary<string, OrderDto> MapOrderDict(Dictionary<string, OrderEntity> source);
    // Generates:
    // var __result = new Dictionary<string, OrderDto>(source.Count);
    // foreach (var __kvp in source)
    //     __result[__kvp.Key] = MapOrder(__kvp.Value);
    // return __result;
}
```

### Complexity

**Low-medium.** Extends the existing collection mapping infrastructure:

- Detection: both source and dest are `Dictionary<TKey, TValue>` or `IDictionary<TKey, TValue>`
- Key types must match (or have a forge method / implicit conversion)
- Value mapping reuses the existing nested forging resolution
- Capacity hint: `new Dictionary<K,V>(source.Count)` for performance

### Files to Modify

- `ForgeGenerator.cs` — extend collection detection to recognize dictionary types
- Generate `foreach` loop with key-value pair iteration instead of `.Select().ToList()`
- Reuse existing nested forge method resolution for value conversion

---

## 6. Snapshot / Approval Testing for Generated Code

**Goal:** Add snapshot testing to the generator test suite so that any change in generated output is immediately caught, preventing tests from silently passing when behavior changes.

### Why

Negative-only assertions (`DoesNotContain`) can pass both before and after a behavioral change. This was observed with the init-only property feature — old tests used only negative assertions and continued to pass even though the generated code changed from skipping init-only properties entirely to placing them in object initializers. Snapshot tests compare the full generated output against a golden file, making any change visible.

### Design

- Each generator test scenario gets a corresponding `.verified.cs` golden file
- Use a library like `Verify` (https://github.com/VerifyTests/Verify) or a simple custom approach:
  1. Generate code via `RunGenerator`
  2. Compare against stored golden file
  3. On mismatch, fail with a diff
  4. Developer reviews and accepts new output to update the golden file

### Suggested Approach

1. Add `Verify.Xunit` NuGet package to `FreakyKit.Forge.Generator.Tests`
2. Create a `Snapshots/` folder for `.verified.cs` golden files
3. Convert key test scenarios (one per mapping feature) to snapshot tests
4. Keep existing assertion-based tests for targeted checks — snapshots complement, not replace
5. Add a CI step that fails if any `.received.cs` files are generated (unapproved changes)

---

## 7. Reverse Mapping

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

## 8. Computed Properties via `[ForgeComputed]`

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
