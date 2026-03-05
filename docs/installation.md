# Installation

FreakyKit.Forge is split into independent NuGet packages — install only what you need. Both `FreakyKit.Forge.Generator` and `FreakyKit.Forge.Analyzers` automatically pull in `FreakyKit.Forge` (core attributes), so you never need to add it separately.

## Recommended: Code Generation + Build-Time Validation

Install both the generator and the analyzer. The generator writes your mapping method bodies at compile time. The analyzer validates your declarations and reports 31 diagnostics (errors, warnings, and info messages) to catch mistakes before you run.

```xml
<ItemGroup>
    <PackageReference Include="FreakyKit.Forge.Generator" Version="1.0.0" />
    <PackageReference Include="FreakyKit.Forge.Analyzers" Version="1.0.0" />
</ItemGroup>
```

## Lightweight: Code Generation Only

Install just the generator if you want the mapping implementations without the analyzer diagnostics. You still get compile errors for invalid code, but you won't see Forge-specific warnings like "destination member has no source match" (`FKF100`) or "nested forging disabled" (`FKF300`).

```xml
<PackageReference Include="FreakyKit.Forge.Generator" Version="1.0.0" />
```

## Optional: Naming Conventions

Install the conventions package if you want advisory helpers for organizing forge classes and methods (e.g., `ForgeConventions.ForgeClassName("Person")` returns `"PersonForges"`). See [conventions.md](conventions.md) for details.

```xml
<PackageReference Include="FreakyKit.Forge.Conventions" Version="1.0.0" />
```

## Advanced: Custom Tooling

Install the diagnostics package directly only if you are building your own Roslyn analyzers or tools that need to reference Forge diagnostic IDs. Most users do not need this — it is already bundled inside the Generator and Analyzers packages.

```xml
<PackageReference Include="FreakyKit.Forge.Diagnostics" Version="1.0.0" />
```

## Local Development (Without NuGet)

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

The `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` flags tell MSBuild to treat these as build-time tools rather than runtime dependencies.
