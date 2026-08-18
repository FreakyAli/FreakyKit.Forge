# Troubleshooting Guide

## Generator Not Running

**Problem**: Partial methods declared but no code generated.

**Checklist**:
- ✅ Class is marked `[Forge]`
- ✅ Class is declared `static partial`
- ✅ Method signatures are correct:
  - **Create**: non-void return, 1 parameter → `public static partial Dest ToDto(Source source)`
  - **Update**: void return, 2 parameters → `public static partial void Update(Source source, Dest dest)`
- ✅ Rebuild solution: `dotnet clean && dotnet build` (generator is incremental; clean rebuild if stuck)
- ✅ Both `FreakyKit.Forge.Generator` and `FreakyKit.Forge.Analyzers` NuGet packages installed

**Still stuck?** Check [docs/diagnostics.md](diagnostics.md) for any compile-time diagnostics that would block generation.

---

## Analyzer Diagnostics Not Appearing

**Problem**: Expected warnings/errors not shown at compile time.

**Checklist**:
- ✅ `FreakyKit.Forge.Analyzers` NuGet package is installed
- ✅ Rebuild solution to re-run analyzer
- ✅ Check Error List window (not just build output)
- ✅ Verify diagnostic codes exist in [docs/diagnostics.md](diagnostics.md)

**Note**: Analyzer runs on your source code, not generated code. Generated code produces different diagnostics.

---

## Generated Code Looks Wrong

**Problem**: Method body doesn't match expected mapping logic.

**Solutions**:
- Check for FKF-series diagnostic warnings (compile errors block generation)
- Review `[ForgeMethod]` attributes — all configuration options in [docs/features.md](features.md)
- Look for `[ForgeMap]` on source/destination members
- Verify constructor is public and matches parameter types
- Check if `AllowNestedForging`, `AllowFlattening`, or other flags are needed

---

## Member Not Being Mapped

**Problem**: Expected property/field missing from generated assignments.

**Causes & Fixes**:
- `[ForgeIgnore]` attribute on the member? → Remove it or adjust `Side`
- Destination member has no matching source member? → Add `[ForgeMap("SourceName")]` or create source member
- Names differ? → Use `[ForgeMap("SourceName")]` to rename
- Member is private? → Private ignored by default; use `[Forge(ShouldIncludePrivate = true)]` to include
- Member is a field? → Enable `[ForgeMethod(ShouldIncludeFields = true)]` to include fields

**Check diagnostics**: FKF100 (destination unmatched) or FKF101 (source unused) point to specific members.

---

## "Incompatible Member Types" Error (FKF200)

**Problem**: Source and destination member types don't match.

**Solutions**:
- Add a `[ForgeConverter]` method to handle the conversion
- Use `[ForgeMap("DifferentMember")]` to map to a different destination member
- For enums: verify mapping strategy: `MappingStrategy = ForgeMapping.ByName` or `.Cast`
- For nested types: enable `[ForgeMethod(AllowNestedForging = true)]` if a forge method exists for the type
- Check if source type can implicitly convert to destination type

---

## Circular Reference Detected (FKF301)

**Problem**: Forge methods reference each other in a cycle.

**Example**:
```csharp
public static partial ADto ToA(A source);  // calls ToB
public static partial BDto ToB(B source);  // calls ToA
```

**Solutions**:
- Break the cycle by using a different approach: `[ForgeConverter]`, conditional mapping, or one-way mapping
- Disable nested forging: `[ForgeMethod(AllowNestedForging = false)]` on one direction
- Restructure the type hierarchy to avoid circular dependencies

---

## Expression Property Not Generated (FKF504/FKF505)

**Problem**: `[ForgeMethod(GenerateExpression = true)]` not producing the expression property.

**Causes**:
- Used on update method (void return) — expressions only work on create methods (non-void, 1 parameter)
- Before/after hooks present — FKF505 warns; hooks can't be called in expressions
- Member has incompatible mapping logic that can't be expressed as LINQ

**Fix**: Remove `GenerateExpression` from update methods, remove hooks, or simplify member mappings.

---

## Tests Failing After Generator Changes

**Problem**: Modified generator code breaks existing tests.

**Checklist**:
- ✅ Rebuild before testing: `dotnet build && dotnet test`
- ✅ Check snapshot `.verified.cs` files — if generator output changed, approve the new snapshot
- ✅ Run test in debug mode to inspect actual vs. expected
- ✅ Verify changes don't affect member discovery logic

**Snapshot testing**: See [CONTRIBUTING.md](../CONTRIBUTING.md) for testing guidelines.

---

## "ForgeMap Target Not Found" Error (FKF104)

**Problem**: `[ForgeMap("Name")]` references a member that doesn't exist.

**Fix**: Check that the target member name is spelled correctly and exists on the counterpart type.

```csharp
// ❌ Wrong — "Name" doesn't exist on Source
public class Source { public string FullName { get; set; } }
public class Dest { [ForgeMap("Name")] public string Name { get; set; } }

// ✅ Correct
public class Dest { [ForgeMap("FullName")] public string Name { get; set; } }
```

---

## "Duplicate ForgeMap Target" Warning (FKF105)

**Problem**: Multiple destination members map to the same source member.

**Fix**: Each source member can be read by at most one destination member. Remove the duplicate or use different source members.

```csharp
// ❌ Wrong — both map to "Name"
public class Dest
{
    [ForgeMap("Name")] public string First { get; set; }
    [ForgeMap("Name")] public string Second { get; set; }
}
```

---

## Strict Mapping Errors (FKF110/FKF111)

**Problem**: `[ForgeMethod(StrictMapping = true)]` reports unmapped members as errors.

**Causes**:
- FKF110: Destination member has no matching source member
- FKF111: Source member is unused (not mapped to any destination)

**Fix**: 
- Add missing members to align source and destination
- Use `[ForgeMap]` to manually map misnamed members
- Use `[ForgeIgnore]` to explicitly exclude intentional differences

Strict mode is useful for critical mappings where silent drift would cause data loss.

---

## Can't Find a Diagnostic Code?

Every FKF diagnostic is documented in [docs/diagnostics.md](diagnostics.md). Use Ctrl+F to search for the diagnostic ID.

If a diagnostic isn't listed, it may be new in a recent version. Check [CHANGELOG.md](../CHANGELOG.md).

---

## Still Stuck?

- Check the full [Feature Documentation](features.md) for attribute options and examples
- Read [docs/diagnostics.md](diagnostics.md) for the diagnostic reference
- Review your types in the debugger — member discovery is case-insensitive but not fuzzy
- Open an issue on [GitHub](https://github.com/FreakyAli/FreakyKit.Forge/issues)
