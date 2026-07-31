# Installation

## The short version

Add two packages and you're done:

```xml
<ItemGroup>
    <PackageReference Include="FreakyKit.Forge.Generator" Version="1.5.0" />
    <PackageReference Include="FreakyKit.Forge.Analyzers" Version="1.5.0" />
</ItemGroup>
```

`Generator` writes your mapping method bodies at compile time. `Analyzers` gives you 77 build-time diagnostics. Both automatically pull in `FreakyKit.Forge` (core attributes) as a transitive dependency — you never need to add it separately.

---

## Package decision guide

| Package | When to install |
|---------|----------------|
| `FreakyKit.Forge.Generator` | Always — this is what generates the code |
| `FreakyKit.Forge.Analyzers` | Always — this is what tells you when something's wrong at build time |
| `FreakyKit.Forge` | Never directly — it comes for free via Generator and Analyzers |
| `FreakyKit.Forge.Conventions` | Only if you want naming convention helpers (see below) |
| `FreakyKit.Forge.Diagnostics` | Only if you're building your own Roslyn analyzers or tools on top of Forge |

---

## Dependency graph

```
FreakyKit.Forge.Generator  ──┐
                              ├──▶  FreakyKit.Forge          (core attributes)
FreakyKit.Forge.Analyzers  ──┤
                              └──▶  FreakyKit.Forge.Diagnostics  (diagnostic descriptors)
```

Both `Generator` and `Analyzers` reference `FreakyKit.Forge` and `FreakyKit.Forge.Diagnostics` directly. NuGet restores them as transitive dependencies. You never reference them in your project file.

---

## Generator only (no diagnostics)

If you want the mapping implementations but don't need build-time validation:

```xml
<PackageReference Include="FreakyKit.Forge.Generator" Version="1.5.0" />
```

You'll still get compile errors for invalid C# in the generated output, but you won't see Forge-specific guidance like "destination member has no source match" (FKF100) or "nested forging disabled" (FKF300).

---

## Optional: naming conventions

The conventions package provides advisory helpers for naming forge classes and methods:

```xml
<PackageReference Include="FreakyKit.Forge.Conventions" Version="1.5.0" />
```

```csharp
ForgeConventions.ForgeClassName("Person")      // → "PersonForges"
ForgeConventions.ForgeMethodName("PersonDto")  // → "ToPersonDto"
```

This package has no dependency on `Generator` or `Analyzers` — it's a standalone utility with no build-time behaviour.

---

## Advanced: custom Roslyn tooling

Install `FreakyKit.Forge.Diagnostics` directly only if you're writing your own Roslyn analyzer or source generator that needs to reference Forge's diagnostic IDs (FKF001–FKF702):

```xml
<PackageReference Include="FreakyKit.Forge.Diagnostics" Version="1.5.0" />
```

Most users never need this — it's already bundled inside both `Generator` and `Analyzers`.

---

## Local development (without NuGet)

If you're building from source instead of using the NuGet packages, add these project references:

```xml
<ItemGroup>
    <ProjectReference Include="path/to/FreakyKit.Forge" />
    <ProjectReference Include="path/to/FreakyKit.Forge.Analyzers"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <ProjectReference Include="path/to/FreakyKit.Forge.Generator"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```

`OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` tell MSBuild to treat these as build-time tools rather than runtime dependencies. You do not need to reference `FreakyKit.Forge.Diagnostics` separately — it is already referenced by the Analyzers and Generator projects.
