# Diagnostic Unit Tests — Refinement Guide

## Current State

- **Total diagnostic unit tests:** 89 test methods across 12 files
- **Passing:** 506/532 tests (95% pass rate overall)
- **Diagnostic unit tests passing:** ~63 out of 89
- **Failing unit tests:** 26 (mostly in diagnostic unit test suite)

## Why Tests Fail

### Category 1: Diagnostics Require Full Compilation Context
**Examples:** FKF001, FKF002, FKF003, FKF004, FKF005, FKF010, FKF011, FKF020

These diagnostics fire during initial validation/parsing phases and require a valid Roslyn compilation with all type references resolved. The RunGenerator() test helper only creates minimal compilations.

**Fix approach:**
```csharp
[Fact]
public void FKFxxx_Scenario_EmitsDiagnostic()
{
    // Add proper Source/Dest class definitions with matching namespaces
    // Ensure all referenced types are declared in the compilation
    // These tests often need >50 lines of setup code
    
    const string source = """
        using FreakyKit.Forge;
        using System.Collections.Generic;
        
        // Define all referenced types explicitly
        public class Source { public string Name { get; set; } = ""; }
        public class Dest { public string Name { get; set; } = ""; }
        
        // Then define the forge class
        [Forge]
        public static partial class Forges { ... }
        """;
}
```

### Category 2: Scenarios Missing Required Members
**Examples:** FKF042, FKF100, FKF101, FKF104, FKF105, FKF107, FKF109

These tests have scenarios that don't actually trigger the diagnostic because the source/dest types have mismatched structures or missing members. The diagnostic can't fire if the member relationship doesn't exist.

**Fix approach:** Ensure source and destination types actually have the member mismatches being tested:
```csharp
[Fact]
public void FKF100_DestinationMemberNoSourceMatch_EmitsWarning()
{
    // Source MUST have fewer members than Dest for FKF100 to trigger
    // Dest member must match by name (case-insensitive) but have no source equivalent
    
    const string source = """
        public class Source 
        { 
            public int X { get; set; }  // Only one member
        }
        public class Dest 
        { 
            public int X { get; set; }     // Matches X
            public int Y { get; set; }     // No source match → FKF100
        }
        """;
    
    AssertDiagnosticEmitted(source, "FKF100");
}
```

### Category 3: Diagnostics from Analyzer, Not Generator
**Examples:** FKF527, FKF528, FKF531, etc.

These diagnostics are emitted by the Roslyn analyzer running on the user's source code, not by the code generator itself. The test infrastructure primarily captures generator diagnostics.

**Workaround:** Check `AllDiagnostics()` which combines generator + compilation diagnostics:
```csharp
protected void AssertDiagnosticEmitted(string source, string diagnosticId)
{
    var result = RunGenerator(source);
    var allDiags = GetAllDiagnostics(result);  // ← includes analyzer diagnostics
    var diagnostic = allDiags.FirstOrDefault(d => d.Id == diagnosticId);
    Assert.NotNull(diagnostic);
}
```

### Category 4: Complex Nested Scenarios
**Examples:** FKF110, FKF111, FKF300, FKF314, FKF315, FKF503, FKF530

These require intricate type relationships, attribute combinations, or edge cases. The test source becomes long and must be precisely structured.

**Fix pattern:** Build realistic class hierarchies
```csharp
[Fact]
public void FKF110_StrictModeDestinationUnmapped_EmitsError()
{
    const string source = """
        using FreakyKit.Forge;
        
        public class Source 
        { 
            public int X { get; set; } 
        }
        
        public class Dest 
        { 
            public int X { get; set; }
            public int Y { get; set; }  // ← Unmapped member
        }
        
        [Forge]
        public static partial class Forges
        {
            [ForgeMethod(StrictMapping = true)]  // ← Strict mode
            public static partial Dest Map(Source s);
        }
        """;
    
    AssertDiagnosticEmitted(source, "FKF110");
    AssertDiagnosticWithSeverity(source, "FKF110", DiagnosticSeverity.Error);
}
```

## Fixing Priority Order

### Tier 1 (Easiest): Member Matching Tests
- FKF100, FKF101, FKF110, FKF111, FKF112
- **Why:** Just need proper member mismatches in source/dest
- **Effort:** 5 mins per test
- **Pattern:** Ensure types have the structural mismatch being tested

### Tier 2 (Medium): Type Safety & Collections
- FKF202, FKF300, FKF314, FKF315
- **Why:** Need proper type relationships and attributes
- **Effort:** 10 mins per test
- **Pattern:** Realistic source/dest with proper generics/nullability

### Tier 3 (Hard): Validation & Construction
- FKF003, FKF004, FKF005, FKF020, FKF503, FKF530
- **Why:** Require complete compilation context or complex class hierarchies
- **Effort:** 20+ mins per test
- **Pattern:** Often need to accept these stay skipped or require integration testing

## How to Fix a Test

### Step 1: Identify the diagnostic condition
Read the diagnostic description in `docs/diagnostics.md` to understand EXACTLY what triggers it.

### Step 2: Create minimal example
Build the smallest possible source code that creates that exact condition:
```csharp
const string source = """
    // Only include what's necessary to trigger the diagnostic
    // Strip all extraneous code
    """;
```

### Step 3: Test it
Run just that test:
```bash
dotnet test tests/FreakyKit.Forge.Generator.Tests \
  --filter "MethodName=TestNameHere" -v detailed
```

### Step 4: Debug if needed
If it still doesn't emit:
- Check if diagnostic comes from analyzer or generator
- Verify all types are properly declared
- Look at a similar PASSING test and copy its structure

## Recommended Next Steps

1. **Fix Tier 1 tests (5 tests)** — 30 mins total
   ```
   FKF100, FKF101, FKF110, FKF111, FKF112
   ```

2. **Review Tier 3 tests** — Decide if they should remain skipped with explanations, or refactored as integration tests that run the full pipeline

3. **Document patterns** — Update this guide with working examples as tests pass

4. **Run full suite** — `dotnet test tests/FreakyKit.Forge.Generator.Tests --filter "DiagnosticUnit"`

## Quick Test Template

Copy this template for new diagnostic unit tests:

```csharp
[Fact]
public void FKFxxx_Scenario_ExpectedResult()
{
    // Positive case: diagnostic SHOULD emit
    const string sourceWithDiagnostic = """
        using FreakyKit.Forge;
        
        public class Source { /* ... */ }
        public class Dest { /* ... */ }
        
        [Forge]
        public static partial class Forges
        {
            [ForgeMethod]
            public static partial Dest Map(Source s);
        }
        """;
    
    AssertDiagnosticEmitted(sourceWithDiagnostic, "FKFxxx");
    AssertDiagnosticWithSeverity(sourceWithDiagnostic, "FKFxxx", DiagnosticSeverity.Warning);
    
    // Negative case: diagnostic should NOT emit
    const string sourceWithoutDiagnostic = """
        // Fixed version that avoids the diagnostic condition
        """;
    
    AssertDiagnosticNotEmitted(sourceWithoutDiagnostic, "FKFxxx");
}
```

## Files by Status

| File | Working Tests | Failing Tests | Status |
|------|---------------|---------------|--------|
| ModeVisibilityDiagnosticsTests.cs | 7/17 | 10 | Tier 3 |
| MethodShapeDiagnosticsTests.cs | 6/18 | 12 | Mixed |
| MemberDiscoveryDiagnosticsTests.cs | 3/4 | 1 | 👍 |
| **MemberMatchingDiagnosticsTests.cs** | **3/12** | **9** | **← START HERE** |
| TypeSafetyDiagnosticsTests.cs | 6/7 | 1 | Good |
| NestedCollectionsDiagnosticsTests.cs | 3/6 | 3 | Mixed |
| ConstructionDiagnosticsTests.cs | 5/6 | 1 | Good |
| ConditionalMappingDiagnosticsTests.cs | 3/3 | 0 | ✅ |
| CrossClassNestedDiagnosticsTests.cs | 5/5 | 0 | ✅ |
| MappingProfilesDiagnosticsTests.cs | 4/4 | 0 | ✅ |
| DictionaryDiagnosticsTests.cs | 3/3 | 0 | ✅ |
| PolymorphicDiagnosticsTests.cs | 4/4 | 0 | ✅ |

**Key insight:** Tests that fire on validation (Mode, MethodShape, Construction) are hardest to test with RunGenerator() infrastructure. Tests that validate member/type compatibility are easier.
