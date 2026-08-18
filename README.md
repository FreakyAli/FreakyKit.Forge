<div align="center">

<img src="forge.png" alt="Forge" width="600" />

# Forge

**A compile-time object mapping library for C# powered by Roslyn source generators**

<a href="https://github.com/FreakyAli/FreakyKit.Forge/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/FreakyAli/FreakyKit.Forge/ci.yml?label=CI&style=for-the-badge" alt="CI"></a>
<a href="https://github.com/FreakyAli/FreakyKit.Forge/actions/workflows/test.yml"><img src="https://img.shields.io/github/actions/workflow/status/FreakyAli/FreakyKit.Forge/test.yml?label=Tests&style=for-the-badge" alt="Tests"></a>
<a href="https://www.nuget.org/packages/FreakyKit.Forge"><img src="https://img.shields.io/nuget/v/FreakyKit.Forge?color=blue&logo=nuget&style=for-the-badge" alt="NuGet"></a>
<a href="https://www.nuget.org/packages/FreakyKit.Forge"><img src="https://img.shields.io/nuget/dt/FreakyKit.Forge?style=for-the-badge" alt="Downloads"></a>
<a href="./LICENSE"><img src="https://img.shields.io/github/license/FreakyAli/FreakyKit.Forge?style=for-the-badge" alt="License"></a>
<a href="https://codecov.io/gh/FreakyAli/FreakyKit.Forge"><img src="https://img.shields.io/codecov/c/github/FreakyAli/FreakyKit.Forge?style=for-the-badge&logo=codecov" alt="Coverage"></a>

<br/>

</div>

## Quick Start

```csharp
using FreakyKit.Forge;

public class Person    { public string Name { get; set; } public int Age { get; set; } }
public class PersonDto { public string Name { get; set; } public int Age { get; set; } }

[Forge]
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source);
}
```

At compile time, Forge generates the implementation:

```csharp
public static partial class PersonForges
{
    public static partial PersonDto ToDto(Person source)
    {
        var __result = new PersonDto();
        __result.Name = source.Name;
        __result.Age = source.Age;
        return __result;
    }
}
```

Then just call it:

```csharp
var dto = PersonForges.ToDto(person);
```

## Why Forge?

Reflection-based mappers like AutoMapper and Mapster are convenient but blind — wrong type, missing member, nullable mismatch and you find out at runtime. At some point that gets old enough that writing mappers by hand starts looking reasonable.

Source generators solve the runtime problem, but control is still coarse. Forge's differentiator is how explicitly you can express intent: **implicit mode** means zero ceremony for simple cases — declare the method signature, the generator fills in the body. **Explicit mode** locks that down for critical paths where nothing should be generated without intent. `ForgeIgnoreSide` lets you exclude a member from one side without hiding it on the other. These are the gaps that come up in real codebases, not just toy examples.

See the [Comparison](#comparison) section below for feature-by-feature breakdown vs AutoMapper, Mapperly, Mapster, and Facet.

## Migration Guides

Switching from another mapping library?

- [Migrate from AutoMapper](docs/migrate-from-automapper.md)
- [Migrate from Mapperly](docs/migrate-from-mapperly.md)
- [Migrate from Mapster](docs/migrate-from-mapster.md)
- [Migrate from Facet](docs/migrate-from-facet.md)

## Installation

For most projects, add two packages:

```xml
<ItemGroup>
    <PackageReference Include="FreakyKit.Forge.Generator" Version="1.5.0" />
    <PackageReference Include="FreakyKit.Forge.Analyzers" Version="1.5.0" />
</ItemGroup>
```

`Generator` writes your mapping bodies at compile time. `Analyzers` gives you 97 build-time diagnostics. Both automatically pull in the core `FreakyKit.Forge` attributes package — you never need to add it separately.

See the [full installation guide](docs/installation.md) for lightweight setups, the optional conventions package, local development without NuGet, and custom Roslyn tooling.

## Terminology

```text
Source (input)  ──────►  Destination (output)
   Person                   PersonDto
```

| Term | Meaning |
|------|---------|
| **Source** | The type you map **from** — the method's first parameter (`Person source`) |
| **Destination** | The type you map **to** — the return type (`PersonDto`) or second parameter in update methods |
| **Source member** | A property/field on the source type the generator reads from |
| **Destination member** | A property/field on the destination type the generator writes to |
| **Flattening** | Mapping nested source properties into flat destination members by name convention (`AddressCity` matches `Address.City`) |
| **Nested forging** | Calling another forge method to convert a type-mismatched member (requires `AllowNestedForging = true`) |
| **Drift detection** | `StrictMapping = true` — unmapped members become errors instead of warnings |

For the complete Forge glossary with examples, see the [Key Terms section in attributes.md](docs/attributes.md#key-terms).

## Features

- **Zero reflection** — all mapping code is generated at compile time
- **Zero runtime dependencies** — the generated code is plain C#
- **Parameterized constructor support** — automatically selects the best constructor
- **Init-only & record support** — init-only properties and records use object initializer syntax
- **Nested forging** — compose mappings for complex object graphs with null-safe access
- **Collection mapping** — automatic `List<T>`, `T[]`, `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>`, `ReadOnlyCollection<T>`, `HashSet<T>` conversion with LINQ
- **Dictionary mapping** — automatic `Dictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>` element conversion with nested forging for value types
- **Null-safe nested access** — null guards on nested forge calls, flattened properties, and collection mappings
- **Flattening** — map nested properties like `Address.City` to flat members like `AddressCity`
- **Custom member mapping** — rename members with `[ForgeMap]` on properties, fields, or constructor parameters
- **Ignore members** — exclude members with `[ForgeIgnore]`; use `Side` to restrict exclusion to source or destination only
- **Type converters** — bridge incompatible types with `[ForgeConverter]`; invalid converter signatures are caught by FKF221
- **Nullable handling** — automatic `Nullable<T>` ↔ `T` conversion with optional default values
- **Enum mapping** — cast or name-based enum-to-enum conversion
- **Update mapping** — modify existing objects in place (void return, 2 parameters)
- **Before/after hooks** — run custom logic before or after mapping via partial methods
- **Implicit and explicit modes** — control which methods get generated
- **Strict mapping (drift detection)** — opt-in error-level diagnostics when source/destination types drift apart
- **Rich diagnostics** — 97 diagnostics across 9 categories guide you at build time
- **Circular forge detection** — detects and reports circular dependencies in nested forge methods at compile time
- **Top-level collection projection** — declare a `List<Dest> ToList(List<Source> source)` method and the generator produces the LINQ projection automatically
- **Top-level dictionary projection** — declare a `Dictionary<string, Dest> ToDict(Dictionary<string, Source> source)` method and the generator produces an efficient `foreach`-based conversion
- **EF Core / IQueryable projection expressions** — add `GenerateExpression = true` and the generator emits a static `Expression<Func<TSource, TDest>>` property alongside the partial method, usable directly in `IQueryable.Select(...)`. Requires EF Core 8+. See [docs/projections.md](docs/projections.md).
- **Reference semantics control** — same-type mutable collections are deep-copied by default (so the DTO owns an independent list/dictionary instance). Opt out with `[ForgeMethod(ShareReference = true)]` for hot paths, or override per-member with `[ForgeMap(ShareReference = ...)]`. See [Reference semantics](docs/attributes.md#reference-semantics-for-same-type-collections).
- **Field support** — opt-in to include fields in member discovery
- **Private method support** — opt-in to include private forge methods
- **Conditional mapping** — skip assignments when source is null with `IgnoreIfNull`
- **Mapping profiles / inheritance** — reuse base-type mappings with `[ForgeIncludes]` — include another forge class and its assignments are inlined into compatible derived methods
- **Debugging friendly** — generated code includes `[GeneratedCode]`, `[DebuggerStepThrough]`, `#line` directives, `#pragma warning disable`, and XML doc comments

## Comparison

<details>
<summary><strong>Forge vs AutoMapper, Mapperly, Mapster, Facet — full feature breakdown</strong></summary>

<br>

> **Note:** This comparison is based on publicly available documentation at the time of writing. If you spot an inaccuracy, please [open an issue](https://github.com/FreakyAli/FreakyKit.Forge/issues) and we'll correct it.

| Feature | Forge | AutoMapper | Mapperly | Mapster | Facet |
|---------|:-----:|:----------:|:--------:|:-------:|:-----:|
| Source generator (compile-time) | ✅ | ❌ | ✅ | ✅ | ✅ |
| Zero runtime dependencies | ✅ | ❌ | ✅ | ❌ | ✅ |
| Constructor mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Nested object mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Collection mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Flattening | ✅ | ✅ | ✅ | ✅ | ✅ |
| Custom member renaming | ✅ | ✅ | ✅ | ✅ | ✅ |
| Ignore members | ✅ | ✅ | ✅ | ✅ | ✅ |
| Type converters | ✅ | ✅ | ✅ | ✅ | ~ |
| Nullable handling | ✅ | ✅ | ✅ | ✅ | ✅ |
| Enum mapping | ✅ | ✅ | ✅ | ✅ | ✅ |
| Update existing objects | ✅ | ✅ | ✅ | ✅ | ✅ |
| Before/after hooks | ✅ | ✅ | ✅ | ✅ | ✅ |
| Rich diagnostics | ✅ | ❌ | ✅ | ~ | ~ |
| Field support | ✅ | ✅ | ✅ | ✅ | ❌ |
| Init-only / record support | ✅ | ✅ | ✅ | ✅ | ✅ |
| Null-safe nested access | ✅ | ✅ | ✅ | ~ | ~ |
| Immutable collection types | ✅ | ✅ | ✅ | ✅ | ~ |
| Strict mapping (drift detection) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Conditional mapping (ignore if null) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Debugging friendly output | ✅ | N/A | ✅ | ~ | ✅ |
| Implicit and explicit mapping modes | ✅ | ❌ | ❌ | ❌ | ❌ |
| Custom constructor parameter mapping | ✅ | ✅ | ✅ | ~ | ❌ |
| Dedicated collection projection methods | ✅ | ✅ | ✅ | ✅ | ~ |
| Side-specific member exclusion | ✅ | ~ | ❌ | ❌ | ❌ |
| Type converter validation | ✅ | N/A | ✅ | N/A | N/A |
| EF Core / IQueryable projection expressions | ✅ | ✅ | ✅ | ✅ | ❌ |

</details>

## Performance Benchmarks

> Benchmarked on .NET 8 using BenchmarkDotNet v0.15.8. The same benchmarks were also run against AutoMapper 16.1.1, Mapperly 4.3.1, Mapster 7.4.0, and Facet 5.8.2 — full per-library breakdown in [docs/benchmarks.md](docs/benchmarks.md).
> Benchmark run: 2026-03-23 — commit: `6132259`

Forge generates plain C# assignments — the same code you'd write by hand. It compiles to identical IL, so the JIT sees no difference. The numbers below confirm that: any variation from hand-written is measurement noise, not a real difference.

| Scenario | Forge | Hand-written |
|----------|------:|-------------:|
| Simple mapping (4 props) | 6.46 ns | 6.37 ns |
| Medium mapping (10 props) | 12.43 ns | 14.49 ns |
| Nested object | 21.92 ns | 23.57 ns |
| Property flattening | 10.97 ns | 11.72 ns |
| Deep object graph | 208.5 ns | 204.5 ns |
| Collection (1,000 items) | 5,270 ns | 5,261 ns |
| Throughput (10,000 objects) | 152.0 μs | 155.2 μs |
| Real-world e-commerce order | 161.5 ns | 161.9 ns |
| Nullable DB entity (populated) | 11.45 ns | 11.71 ns |

The meaningful comparison is against reflection-based mappers — see [docs/benchmarks.md](docs/benchmarks.md) for the full breakdown.

## The Forge Ecosystem

| Package | Install? | Downloads | What it does |
|---------|:--------:|:---------:|--------------|
| [**FreakyKit.Forge.Generator**](https://www.nuget.org/packages/FreakyKit.Forge.Generator) | ✅ Always | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Generator?style=flat-square) | Roslyn source generator — writes your mapping method bodies at compile time |
| [**FreakyKit.Forge.Analyzers**](https://www.nuget.org/packages/FreakyKit.Forge.Analyzers) | ✅ Always | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Analyzers?style=flat-square) | Roslyn analyzer — 87 build-time diagnostics to catch mistakes before you run |
| [**FreakyKit.Forge**](https://www.nuget.org/packages/FreakyKit.Forge) | ⛔ Never directly | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge?style=flat-square) | Core attributes and enums — pulled in automatically by Generator and Analyzers |
| [**FreakyKit.Forge.Conventions**](https://www.nuget.org/packages/FreakyKit.Forge.Conventions) | 🔧 Optional | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Conventions?style=flat-square) | Naming helpers — `ForgeConventions.ForgeClassName("Person")` → `"PersonForges"` |
| [**FreakyKit.Forge.Diagnostics**](https://www.nuget.org/packages/FreakyKit.Forge.Diagnostics) | 🔧 Advanced | ![NuGet Downloads](https://img.shields.io/nuget/dt/FreakyKit.Forge.Diagnostics?style=flat-square) | Shared diagnostic descriptors — only if you're building custom Roslyn tooling on top of Forge |

## Feature Documentation

**See [docs/features.md](docs/features.md)** for comprehensive documentation on every Forge feature with code examples:

- **Method Shape** — Forge method declaration patterns (create, update)
- **Constructor Selection** — How the best constructor is chosen
- **Nested Forging** — Composing mappings for complex object graphs
- **Collection Mapping** — Automatic `List<T>`, arrays, immutable types, etc.
- **Dictionary Mapping** — `Dictionary<K, V>` with nested forging support
- **Flattening** — Map nested properties to flat members
- **Custom Member Mapping** — Rename members with `[ForgeMap]`
- **Ignore Members** — Exclude members with `[ForgeIgnore]`
- **Type Converters** — Bridge incompatible types with `[ForgeConverter]`
- **Nullable Handling** — Automatic `Nullable<T>` conversion with defaults
- **Conditional Mapping** — Skip assignments when source is null
- **Init-Only & Records** — Object initializer syntax for readonly members
- **Null-Safe Access** — Automatic null guards on nested access
- **Strict Mapping** — Detect type drift at compile time
- **Enum Mapping** — Cast or name-based enum-to-enum conversion
- **Before/After Hooks** — Custom logic via partial methods
- **Implicit vs Explicit Mode** — Control which methods get generated
- **Attribute Reference** — Complete `[Forge]`, `[ForgeMethod]`, `[ForgeMap]`, `[ForgeConverter]`, `[ForgeIgnore]` guide

## Diagnostics

**Nothing happens silently.** When something doesn't work as expected, when a member is skipped, when an edge case is hit — you get a compile-time diagnostic with documentation on how to fix it.

Errors block code generation. Warnings flag potential issues before they become runtime bugs. Info diagnostics confirm intentional behavior. No surprises, no hidden skips, no guessing why a mapping didn't work.

**See [docs/diagnostics.md](docs/diagnostics.md)** for the complete reference of 100+ diagnostics organized by category:

- **Mode & Visibility** (FKF001–FKF011) — class shape, method visibility
- **Method Shape** (FKF020–FKF051) — signature validation, hooks
- **Member Discovery & Matching** (FKF100–FKF112, FKF530–FKF542) — member mapping, flattening, includes
- **Type Safety** (FKF200–FKF230) — type compatibility, converters, enums
- **Nested Forging** (FKF300–FKF316) — nested calls, conditionals, expressions
- **Collections & Dictionaries** (FKF310–FKF316, FKF700–FKF702) — mapping strategies, key policies
- **Construction** (FKF500–FKF509) — constructor selection, expression generation
- **Mapping Profiles** (FKF520–FKF542) — includes, inheritance, diamond dedup
- **Polymorphic Mapping** (FKF800–FKF807) — dispatch validation

## Project Structure

```text
src/
  FreakyKit.Forge/              # Core attributes and enums (NuGet: FreakyKit.Forge)
  FreakyKit.Forge.Generator/    # Roslyn source generator (NuGet: FreakyKit.Forge.Generator)
  FreakyKit.Forge.Analyzers/    # Roslyn analyzer (NuGet: FreakyKit.Forge.Analyzers)
  FreakyKit.Forge.CodeFixes/    # Code fix providers (packed into Analyzers NuGet)
  FreakyKit.Forge.Diagnostics/  # Shared diagnostic descriptors (NuGet: FreakyKit.Forge.Diagnostics)
  FreakyKit.Forge.Conventions/  # Optional naming conventions (NuGet: FreakyKit.Forge.Conventions)
tests/
  FreakyKit.Forge.Analyzers.Tests/   # 190 tests
  FreakyKit.Forge.Generator.Tests/   # 407 tests
  FreakyKit.Forge.Integration.Tests/ # 51 tests
  FreakyKit.Forge.EFCore.Tests/      # 8 tests — projection expressions verified against real EF Core 8 + Sqlite
```

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a complete history of releases and what changed in each version.

## Roadmap

Features planned for future versions — production-grade real-world benchmarks, reverse mapping, computed properties, generic forge methods, and more. See [docs/future-plans.md](docs/future-plans.md) for the full breakdown with design notes.

## Troubleshooting & FAQ

**See [docs/troubleshooting.md](docs/troubleshooting.md)** for detailed solutions to common issues:

- Generator not running
- Diagnostics not appearing
- Generated code looks wrong
- Members not being mapped
- Type mismatches
- Circular references
- Expression generation issues
- Test failures

**Quick FAQ:**

<details>
<summary><strong>Q: My partial method has no body generated. Why?</strong></summary>

**A:** Check three things:
1. Class has `[Forge]` attribute
2. Class is declared `static partial`
3. Method signature is valid (non-void + 1 param for create, or void + 2 params for update)

If all three are correct, rebuild the solution and check for compile-time diagnostics.
</details>

<details>
<summary><strong>Q: Is Forge suitable for large object graphs?</strong></summary>

**A:** Yes. Forge generates plain C# assignments, so performance is identical to hand-written mappers. Deep nesting and complex hierarchies work, though you may want to use `[ForgeMethod(AllowFlattening = true)]` or `[ForgeIncludes]` to keep methods maintainable.
</details>

<details>
<summary><strong>Q: Can I use Forge in a shared library (NuGet package)?</strong></summary>

**A:** Yes. Install `FreakyKit.Forge.Generator` and `FreakyKit.Forge.Analyzers` in your library project. Generated mappers are included in the compiled DLL as public methods.
</details>

<details>
<summary><strong>Q: Does Forge work with EF Core?</strong></summary>

**A:** Yes. Use `[ForgeMethod(GenerateExpression = true)]` to generate an `Expression<Func<TSource, TDest>>` property suitable for `IQueryable.Select(...)`. Requires EF Core 8+. See [docs/features.md](docs/features.md) for examples.
</details>

<details>
<summary><strong>Q: What if source and destination types have different constructors?</strong></summary>

**A:** Forge automatically selects the best constructor. If multiple constructors are equally viable, you get FKF500 (ambiguity). Use `[ForgeMap]` on constructor parameters to guide the selection, or add a parameterless constructor.
</details>

<details>
<summary><strong>Q: Can I map nullable to non-nullable types?</strong></summary>

**A:** Yes, but you get a warning (FKF201) because `.Value` can throw. Better: use `[ForgeMap("Property", DefaultValue = 0)]` to provide a fallback instead.
</details>

<details>
<summary><strong>Q: Does Forge support circular references?</strong></summary>

**A:** No — circular nested forging emits FKF301 (error). Break the cycle by using a converter, conditional mapping, or one-way mapping.
</details>

## Contributing

Want to contribute to Forge? We'd love your help! Whether it's bug reports, feature requests, documentation improvements, or code contributions, every contribution helps make Forge better.

**Start here:** [CONTRIBUTING.md](CONTRIBUTING.md) — comprehensive guide covering:
- Getting started with local development
- Project structure and architecture
- Development workflow and testing
- Code style and key patterns
- Submitting pull requests
- Documentation standards

## Support the Project

If you find Forge useful, consider supporting its development:

[![](https://miro.medium.com/max/600/0*wrBJU05A3BULKcWA.gif)](https://www.buymeacoffee.com/FreakyAli)

## License

Apache-2.0 — see [LICENSE](LICENSE) for details.

## Activity

[![Star History Chart](https://api.star-history.com/svg?repos=FreakyAli/FreakyKit.Forge&type=Date)](https://star-history.com/#FreakyAli/FreakyKit.Forge&Date)

![Alt](https://repobeats.axiom.co/api/embed/4e1dad54a0d67502121ef9d06efa2b3fba64c7a3.svg "Repobeats analytics image")
