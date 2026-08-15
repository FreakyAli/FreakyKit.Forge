# Changelog

All notable changes to Forge are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `[ForgePolymorphic]` attribute for derived type dispatch via switch expressions
- FKF800–FKF807 diagnostics for polymorphic mapping validation
- Code fix providers: FKF109 (remove conflicting `[ForgeMap]`), FKF112 (remove self-referencing `[ForgeMap]`), FKF525/FKF526 (add missing `[Forge]` to class)
- Migration guides: [AutoMapper](docs/migrate-from-automapper.md), [Mapperly](docs/migrate-from-mapperly.md), [Mapster](docs/migrate-from-mapster.md), [Facet](docs/migrate-from-facet.md)
- 6 new sample demos: dictionary mapping, expression projection, strict mapping, ForgeUses cross-class sharing, conditional mapping, ShareReference
- Project config: `global.json` (SDK 9.0.x) and `.editorconfig`

## [1.5.0] - 2026-07-30

### Fixed

- Condition methods now resolved from included (`[ForgeUses]`) classes
- `Condition` and `IgnoreIfDefault` guards applied across all codegen paths: expression, update, and init-only

## [1.4.1] - 2026-07-27

### Fixed

- Documentation and test cleanup
- Minor consistency improvements

## [1.4.0] - 2026-07-26

### Added

- Conditional/Predicate Mapping on `[ForgeMap]` (`Condition`, `IgnoreIfDefault`)
- Cross-class nested forging via `[ForgeUses]`
- Multi-level deep flattening (recursive up to 10 levels)
- Dictionary mapping with `Dictionary<string, string>` parsing and key casing/missing key/null value policies
- Circular `[ForgeUses]` detection (transitive)
- Parameter type and constructor/converter accessibility validation

### Fixed

- String injection and null safety hardening in generated code
- `GetHashCode`/`Equals` contract on internal models
- NullFallback documentation and edge case handling

## [1.3.1] - 2026-07-09

### Added

- NullFallback support with `DefaultValue` on `[ForgeMap]`

### Fixed

- Circular dependency detection improvements
- String injection and null safety for generators
- Duplicate null fallback validation extracted to shared helper

## [1.3.1-pre] - 2026-07-04

### Added

- Snapshot testing infrastructure
- Default extension methods for DTOs (`GenerateExtensionMethods`)
- Enum-to-string mapping support

## [1.3.0] - 2026-07-01

### Added

- EF Core `IQueryable` projection expressions (`GenerateExpression = true`)
- Consistent analyzer and generator diagnostic checks

### Fixed

- Benchmark sorting and accuracy improvements

## [1.2.0-pre] - 2026-04-28

### Added

- Additional diagnostics for missing forge attributes
- Improved README structure and documentation

### Fixed

- Test coverage improvements

## [1.0.1] - 2026-03-24

### Added

- Benchmarks comparing Forge vs AutoMapper, Mapperly, and Mapster

## [1.0.0] - 2026-03-06

### Added

- Initial release
- `[Forge]` and `[ForgeMethod]` attributes for compile-time object mapping
- `[ForgeIgnore]` and `[ForgeMap]` for member control
- `[ForgeConverter]` for custom type conversion
- `ForgeMode.Implicit` and `ForgeMode.Explicit` mapping modes
- Nullable mapping support
- `IgnoreIfNull` conditional mapping
- `GeneratedCodeAttribute` on all generated methods
- Roslyn analyzer with build-time diagnostics
- Sample project

[Unreleased]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.5.0...HEAD
[1.5.0]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.4.1...v1.5.0
[1.4.1]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.3.1...v1.4.0
[1.3.1]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.3.1-pre...v1.3.1
[1.3.1-pre]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.3.0...v1.3.1-pre
[1.3.0]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.2.0-pre...v1.3.0
[1.2.0-pre]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.0.1...v1.2.0-pre
[1.0.1]: https://github.com/FreakyAli/FreakyKit.Forge/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/FreakyAli/FreakyKit.Forge/releases/tag/v1.0.0
