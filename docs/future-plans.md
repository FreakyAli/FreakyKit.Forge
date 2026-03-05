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
