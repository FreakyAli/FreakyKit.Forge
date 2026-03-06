# Analyzer

The FreakyKit.Forge analyzer (`FreakyKit.Forge.Analyzers`) is a Roslyn diagnostic analyzer that validates forge declarations and reports warnings and errors at build time. It runs alongside the source generator but produces only diagnostics, not source code.

## What It Checks

The analyzer validates every `[Forge]`-decorated static partial class and its forge methods.

### Class-Level Checks

| Check | Diagnostic |
|-------|-----------|
| Explicit mode is active | FKF001 (Info) |
| Private method inclusion is enabled | FKF011 (Info) |

### Method-Level Checks

| Check | Diagnostic |
|-------|-----------|
| Method has forge shape but no `[ForgeMethod]` in explicit mode | FKF002 (Warning) |
| Private forge method without `ShouldIncludePrivate` | FKF010 (Warning) |
| Method has an implementation body | FKF020 (Error) |
| Duplicate forge method names (overloading) | FKF030 (Error) |
| Update method shape detected | FKF040 (Info) |
| Update destination has no settable members | FKF041 (Error) |
| Before hook detected | FKF050 (Info) |
| After hook detected | FKF051 (Info) |

### Member-Level Checks

| Check | Diagnostic |
|-------|-----------|
| Public field excluded (ShouldIncludeFields = false) | FKF400 (Warning) |
| Fields enabled on method | FKF401 (Info) |
| Member excluded via `[ForgeIgnore]` | FKF102 (Info) — reserved, not currently emitted |
| Custom mapping via `[ForgeMap]` | FKF103 (Info) — reserved, not currently emitted |
| `[ForgeMap]` target not found | FKF104 (Error) |
| Duplicate `[ForgeMap]` target | FKF105 (Warning) |
| Flattened mapping applied | FKF106 (Info) — emitted by generator only |
| Destination member has no source match | FKF100 (Warning) |
| Source member has no destination match | FKF101 (Warning) |
| Name match but incompatible types, no forge method | FKF200 (Error) |
| Nullable value type to non-nullable mapping | FKF201 (Warning) |
| Nullable mapping applied | FKF202 (Info) |
| Enum cast mapping | FKF210 (Info) |
| Enum name-based mapping | FKF211 (Info) |
| Enum member missing in destination | FKF212 (Warning) |
| Type converter used | FKF220 (Info) |
| Nested forge exists but AllowNestedForging is false | FKF300 (Warning) |
| Collection mapping applied | FKF310 (Info) |

### Construction Checks

| Check | Diagnostic |
|-------|-----------|
| Multiple equally viable constructors | FKF500 (Error) |
| Constructor parameter can't be satisfied | FKF501 (Error) |
| No viable constructor | FKF502 (Error) |

## Type Mismatch Resolution

The analyzer mirrors the generator's resolution chain. When a name-matched pair has different types, the analyzer checks (in order):

1. Nullable compatibility (`T` ↔ `Nullable<T>`)
2. Enum-to-enum mapping
3. Collection type mapping (both sides are collection types)
4. Type converter (`[ForgeConverter]` method exists)
5. Nested forge method exists (reports `FKF300` if `AllowNestedForging` is false)
6. Incompatible types (`FKF200` error)

This ensures the analyzer diagnostics stay in sync with what the generator would actually produce.

## Generated Code Analysis

The analyzer is configured with `GeneratedCodeAnalysisFlags.None`, meaning it does not analyze generated code. This prevents false positives on the source generator's output.

## Orphan Body Detection

The analyzer detects a specific edge case: a user writes a partial method with a body that looks like a forge method but has no corresponding bodyless declaration. This is detected via syntax analysis (checking for the `partial` keyword modifier combined with a block or expression body) and reports `FKF020`.

Methods originating from generated files (`*.g.cs`) are excluded from this check.

## Concurrent Execution

The analyzer supports concurrent execution (`EnableConcurrentExecution`) for performance in large solutions.
