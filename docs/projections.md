# Projection Expressions

When `[ForgeMethod(GenerateExpression = true)]` is set, Forge emits a static
`Expression<Func<TSource, TDest>>` property alongside the regular partial method body.
This lets the same mapping work in `IQueryable.Select(...)` against EF Core or any other
LINQ provider, so the projection runs as SQL rather than fetching every column and mapping
in memory.

```csharp
[Forge]
public static partial class PersonForges
{
    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonDto ToDto(Person source);
}

// Use the imperative method as before:
var dto = PersonForges.ToDto(person);

// Or use the generated expression directly in EF Core:
var dtos = await dbContext.People
    .Where(p => p.IsActive)
    .Select(PersonForges.ToDtoExpression)
    .ToListAsync();
```

The expression property is named `{MethodName}Expression` — so `ToDto` produces `ToDtoExpression`,
`MapOrder` produces `MapOrderExpression`, and so on.

## Requirements

- **EF Core 8 or later** if you intend to use the expression against a database. EF Core 6 and 7
  are out of support and not guaranteed to translate every shape Forge emits. The C# language
  features (object initializers, `Expression.Condition`, `Expression.New`, member chains) work
  in any version that supports expression trees, but EF translation parity is the bar that matters.
- The imperative partial method is unaffected by the flag. `GenerateExpression = true` is purely
  additive — both the method body and the expression property exist after compilation.

### Verified translation

Every mapping shape in the table below is covered by an end-to-end test in
`tests/FreakyKit.Forge.EFCore.Tests/` that runs the generated expression against a real
EF Core 8 + Sqlite database. SQL translation is verified, not assumed. If you upgrade
EF Core or switch providers and want to re-verify, run `dotnet run --project tests/FreakyKit.Forge.EFCore.Tests`
locally.

## What gets translated

The generator emits an expression property for every mapping shape it can encode as a
translatable expression tree. Concretely:

| Mapping case | Imperative | Expression |
|---|---|---|
| Same-type direct assignment | `__result.X = source.X` | `X = source.X` |
| Nullable → non-nullable | `source.X.Value` | `source.X.GetValueOrDefault()` |
| Nullable with `DefaultValue` | `source.X ?? default` | `source.X ?? default` |
| `[ForgeMap]` rename | same | same |
| Enum cast | `(DestEnum)source.X` | `(DestEnum)source.X` |
| Enum by name | `switch` expression | chained ternary (`source.X == Src.A ? Dest.A : ...`) |
| Parameterized constructor | `new Dest(a, b)` | `new Dest(a, b)` |
| Init-only properties | object initializer | object initializer |
| Nested forge | `Nested(source.X)` call | nested expression body inlined |
| Collection (List, Array) | `Select(...).ToList()` | `Select(x => new ElemDto { ... }).ToList()` |
| Flattening, value-type intermediate | `source.X.Y` | `source.X.Y` |
| Flattening, reference-type intermediate | `source.X?.Y` | `source.X == null ? null : source.X.Y` |

The key constraint is that **expression-tree lambdas cannot contain switch expressions, null-conditional
operators (`?.`), pattern matching, or method calls EF can't translate**. Forge's expression mode
rewrites these patterns into translatable equivalents where possible, and omits members where no
equivalent exists.

### Why `GetValueOrDefault()` instead of `.Value`?

The imperative method uses `.Value` because it preserves the original throw-on-null semantics. The
expression form uses `.GetValueOrDefault()` so the same expression can be `.Compile()`-invoked
against a null input without throwing. EF translates both identically — the difference is purely
about in-memory safety.

### Why chained ternary for ByName enums?

C# expression trees do not allow `switch` expressions. The only EF-translatable encoding for ByName
mapping is a chain of conditionals: `source.Status == Src.A ? Dest.A : source.Status == Src.B ? Dest.B : default`.
EF Core 8+ translates this to a SQL `CASE WHEN ... THEN ... ELSE ... END`. Source enum members that
don't exist on the destination fall to the `default` arm (the imperative method preserves the
throw-on-missing behaviour via the switch's default).

### Why inline nested forge bodies?

EF Core cannot translate `Expression.Invoke`. If a member needs another forge method to map
its type (`AllowNestedForging = true`), the generator inlines that method's expression body
directly into the parent. For a circular reference (A → B → A, etc.) the generator emits
**FKF507** and the expression property is suppressed because inlining would produce infinite source.

When inlining depth exceeds five levels, **FKF508** fires as informational notice — the expression
property still emits, but generated source size grows multiplicatively at deep nesting.

## What gets excluded

Forge silently omits a member from the expression property — and emits **FKF506** explaining why —
in these cases. The imperative method still maps the member normally.

| Reason | Cause |
|---|---|
| Custom converter | `[ForgeConverter]` methods are user-defined static methods that EF can't translate. Write the conversion inline or skip projection mode for this member. |
| `IgnoreIfNull` | "Skip assignment when source is null" has no expression-tree equivalent. Expression trees always evaluate every binding. |
| Non-translatable collection materializer | `HashSet`, `ImmutableArray`, `ImmutableList`, `ImmutableHashSet`, `ReadOnlyCollection` — EF translates only `.ToList()` and `.ToArray()`. Map the collection imperatively or change the destination type to `List<T>` / `T[]`. |
| Before/after hooks | Hooks invoke arbitrary side-effectful methods which don't exist in expression trees. **FKF505** fires once per method when hooks coexist with `GenerateExpression = true`. The hooks still run when the imperative method is called. |

If every member of a method is excluded (e.g. a hooks-only method), the expression property is
suppressed entirely.

## Update methods

Update methods (`void` return, two parameters) cannot produce an expression — there's no return
value to project. Setting `GenerateExpression = true` on an update method emits **FKF504** (Error)
and blocks generation for the entire class. Drop the flag or split into a create method.

## Diagnostic reference

| ID | Severity | When |
|---|---|---|
| FKF504 | Error | `GenerateExpression = true` on an update method |
| FKF505 | Warning | `GenerateExpression = true` with before/after hooks (hooks omitted from expression) |
| FKF506 | Info | Member excluded from generated expression — reason in message |
| FKF507 | Error | Cycle in nested-forge inlining chain |
| FKF508 | Info | Inlined nesting depth exceeds five levels |

See [diagnostics.md](diagnostics.md) for the full diagnostic catalogue.

## Caveats

- **Custom value converters** configured in EF Core's `OnModelCreating` (e.g. `HasConversion<string>()`
  for an enum column) affect how the expression is translated to SQL. The expression itself is valid;
  the SQL plan depends on your model configuration.
- **Provider parity** for less common shapes (deep nested + immutable collections + custom value
  converters all at once) may need testing against your target database. The FKF506 diagnostics tell
  you at build time which members aren't going to translate — if your build is clean, the expression
  is safe.
- **Generated source size**: deeply nested mappings produce large inlined expression bodies. FKF508
  surfaces the cost at depth > 5. The runtime cost is identical to hand-written code; only the
  generated file size grows.
