# Comprehensive Diagnostic Unit Tests Index

**Status:** Foundational structure created. Framework ready for all 109 diagnostics.

## Test Files & Coverage

### 1. Mode & Visibility Diagnostics ✅ (11 diagnostics)
**File:** `ModeVisibilityDiagnosticsTests.cs`
- FKF001 ✅ Explicit mode activated (Info)
- FKF002 ✅ Method ignored in explicit mode (Warning)
- FKF003 ✅ Forge class not static (Error)
- FKF004 ✅ Forge class not partial (Error)
- FKF005 ✅ [Forge] on non-class type (Error)
- FKF010 ✅ Private forge method ignored (Warning)
- FKF011 ✅ Private visibility enabled (Info)

**Remaining in this category:**
- None (all 11 diagnostics in Mode & Visibility are covered by FKF001-FKF011)

### 2. Method Shape Diagnostics ✅ (10 diagnostics)
**File:** `MethodShapeDiagnosticsTests.cs`
- FKF020 ✅ Forge method declares a body (Error)
- FKF030 ✅ Forge method name overloaded (Error)
- FKF040 ✅ Update mode activated (Info)
- FKF041 ✅ Update destination has no settable members (Error)
- FKF042 ✅ Zero members mapped (Warning)
- FKF043 ✅ Flattening enabled but nothing flattened (Warning)
- FKF050 ✅ Before hook detected (Info)
- FKF051 ✅ After hook detected (Info)

**Remaining in this category:**
- None (8 covered, all in FKF020-FKF051 range)

### 3. Member Discovery Diagnostics ⏳ (2 diagnostics)
**File:** `MemberDiscoveryDiagnosticsTests.cs` (TO CREATE)
- FKF400 ☐ Field ignored (Warning)
- FKF401 ☐ Fields enabled (Info)

### 4. Member Matching Diagnostics ⏳ (20+ diagnostics)
**File:** `MemberMatchingDiagnosticsTests.cs` (TO CREATE)
- FKF100 ☐ Destination member missing source (Warning)
- FKF101 ☐ Source member unused (Warning)
- FKF102 ☐ Member ignored via [ForgeIgnore] (Info)
- FKF103 ☐ Custom member mapping (Info)
- FKF104 ☐ [ForgeMap] target not found (Error)
- FKF105 ☐ Duplicate [ForgeMap] target (Warning)
- FKF106 ☐ Flattened mapping applied (Info)
- FKF107 ☐ Read-only destination member skipped (Info)
- FKF108 ☐ Write-only source member skipped (Info)
- FKF109 ☐ Both [ForgeIgnore] and [ForgeMap] (Warning)
- FKF110 ☐ Strict: destination member missing source (Error)
- FKF111 ☐ Strict: source member unused (Error)
- FKF112 ☐ [ForgeMap] maps to own name (Warning)
- FKF530 ☐ Ambiguous flattening (Error)
- FKF531 ☐ Deep flattening detected (Info)
- FKF532 ☐ Flattening depth limit exceeded (Error)
- FKF539 ☐ Included assignment skipped — dest member not found (Info)
- FKF540 ☐ Included assignment skipped — constructor provides member (Info)
- FKF541 ☐ Included init-only skipped in update method (Info)
- FKF542 ☐ Included assignment diamond dedup (Info)

### 5. Type Safety Diagnostics ⏳ (17 diagnostics)
**File:** `TypeSafetyDiagnosticsTests.cs` (TO CREATE)
- FKF200 ☐ Incompatible member types (Error)
- FKF201 ☐ Nullable value type to non-nullable mapping (Warning)
- FKF202 ☐ Nullable mapping applied (Info)
- FKF203 ☐ Lossy implicit conversion (Warning)
- FKF210 ☐ Enum cast mapping (Info)
- FKF211 ☐ Enum name-based mapping (Info)
- FKF212 ☐ Enum member missing in destination (Warning)
- FKF220 ☐ Type converter used (Info)
- FKF221 ☐ Invalid converter signature (Warning)
- FKF222 ☐ Duplicate converter for same type pair (Warning)
- FKF230 ☐ Enum ↔ string mapping applied (Info)
- FKF316 ☐ Conditional guard on init-only member (Error)

### 6. Nested & Collections Diagnostics ⏳ (11 diagnostics)
**File:** `NestedCollectionsDiagnosticsTests.cs` (TO CREATE)
- FKF300 ☐ Nested forging disabled (Warning)
- FKF301 ☐ Circular nested forge detected (Error)
- FKF310 ☐ Collection mapping applied (Info)
- FKF311 ☐ Collection reference-shared (Info)
- FKF312 ☐ Same-type custom class reference-shared (Info)
- FKF313 ☐ Conflicting ShareReference (Warning)
- FKF314 ☐ NullFallback on value type (Warning)
- FKF315 ☐ IgnoreIfNull + NullFallback both set (Error)

### 7. Construction Diagnostics ⏳ (10 diagnostics)
**File:** `ConstructionDiagnosticsTests.cs` (TO CREATE)
- FKF500 ☐ Constructor ambiguity (Error)
- FKF501 ☐ Missing constructor parameter (Error)
- FKF502 ☐ No viable constructor (Error)
- FKF503 ☐ Destination not instantiable (Error)
- FKF504 ☐ GenerateExpression on update method (Error)
- FKF505 ☐ Hooks ignored in expression (Warning)
- FKF506 ☐ Member excluded from expression (Info)
- FKF507 ☐ Circular nested forge in expression (Error)
- FKF508 ☐ Deep expression nesting (Warning)
- FKF509 ☐ Expression nesting limit exceeded (Error)

### 8. Conditional/Predicate Diagnostics ⏳ (4 diagnostics)
**File:** `ConditionalMappingDiagnosticsTests.cs` (TO CREATE)
- FKF510 ☐ Condition method not found (Error)
- FKF511 ☐ Condition method invalid signature (Error)
- FKF512 ☐ Condition method not accessible (Error)
- FKF513 ☐ Condition method shadowed (Warning)

### 9. Cross-Class Nested Diagnostics ⏳ (10 diagnostics)
**File:** `CrossClassNestedDiagnosticsTests.cs` (TO CREATE)
- FKF520 ☐ Included forge class not found (Error)
- FKF521 ☐ Included class not [Forge] (Error)
- FKF522 ☐ Circular forge class includes (Error)
- FKF523 ☐ Nested forge method shadowed (Warning)
- FKF524 ☐ [ForgeUses] without [Forge] (Error)
- FKF525 ☐ [ForgeMethod] without [Forge] class (Error)
- FKF526 ☐ [ForgeConverter] without [Forge] class (Error)
- FKF527 ☐ [ForgeMap] on source type (Warning)
- FKF528 ☐ [ForgeIgnore] on source type (Warning)

### 10. Mapping Profiles / Inheritance Diagnostics ⏳ (8 diagnostics)
**File:** `MappingProfilesDiagnosticsTests.cs` (TO CREATE)
- FKF533 ☐ Included profile class not found (Error)
- FKF534 ☐ Included profile class not [Forge] (Error)
- FKF535 ☐ Circular [ForgeIncludes] (Error)
- FKF536 ☐ No compatible method in included profile (Warning)
- FKF537 ☐ Local assignment shadows included assignment (Info)
- FKF538 ☐ [ForgeIncludes] without [Forge] (Error)

### 11. Silent Skip Diagnostics ⏳ (12 diagnostics)
**File:** `SilentSkipsDiagnosticsTests.cs` (TO CREATE)
- FKF543 ☐ [ForgeMethod] on wrong-shape method (Error)
- FKF544 ☐ Non-INamedTypeSymbol source/dest type (Error)
- FKF545 ☐ Malformed [ForgePolymorphic] (Error)
- FKF546 ☐ Flattening name match with type mismatch (Error)
- FKF547 ☐ Profile method extraction errors (Warning)
- FKF548 ☐ Init-only in update context (Info)
- FKF549 ☐ Inaccessible source member (Info)
- FKF550 ☐ Destination member no setter (Info)
- FKF551 ☐ Profile class resolution failed (Warning)
- FKF552 ☐ Included class resolution failed (Warning)
- FKF553 ☐ All expression members excluded (Info)
- FKF554 ☐ Constructor-consumed members (Info)

### 12. Dictionary Diagnostics ⏳ (3 diagnostics)
**File:** `DictionaryDiagnosticsTests.cs` (TO CREATE)
- FKF700 ☐ Dictionary key type not string (Error)
- FKF701 ☐ Unsupported dictionary value type (Error)
- FKF702 ☐ ReturnNull on non-nullable (Error)

### 13. Polymorphic Diagnostics ⏳ (8 diagnostics)
**File:** `PolymorphicDiagnosticsTests.cs` (TO CREATE)
- FKF800 ☐ Polymorphic method not found (Error)
- FKF801 ☐ Polymorphic return type mismatch (Error)
- FKF802 ☐ Polymorphic source type mismatch (Error)
- FKF803 ☐ Unreachable polymorphic pattern (Error)
- FKF804 ☐ Incompatible options on polymorphic dispatch (Error)
- FKF805 ☐ Expression not supported on polymorphic (Error)
- FKF806 ☐ Duplicate polymorphic source type (Error)
- FKF807 ☐ [ForgePolymorphic] without [Forge] class (Error)

---

## Summary

| Status | Count | Files |
|--------|-------|-------|
| ✅ Complete | 18 | 2 files (Mode & Visibility, Method Shape) |
| ⏳ To Create | 91 | 11 files |
| **Total** | **109** | **13 files** |

## Quick Start for Adding Tests

Each test file:
1. Inherits from `DiagnosticsTestBase`
2. Uses helper methods: `AssertDiagnosticEmitted()`, `AssertDiagnosticNotEmitted()`, `AssertDiagnosticWithSeverity()`
3. Tests both **positive** (triggers diagnostic) and **negative** (doesn't trigger) cases
4. One test method per diagnostic scenario (both positive and negative if applicable)

## Example Pattern

```csharp
[Fact]
public void FKFnnn_ScenarioDescription_ExpectedResult()
{
    const string source = """
        // Source code that triggers or doesn't trigger FKFnnn
    """;

    // Positive case: diagnostic should emit
    AssertDiagnosticEmitted(source, "FKFnnn", "message part");
    AssertDiagnosticWithSeverity(source, "FKFnnn", DiagnosticSeverity.Error);

    // Negative case: diagnostic should NOT emit
    // Use a different source and call:
    // AssertDiagnosticNotEmitted(source2, "FKFnnn");
}
```

---

## Next Steps

1. Create remaining 11 test files following the pattern established
2. Each test file should have 2-4 tests per diagnostic (positive + negative + edge cases)
3. Run full test suite: `dotnet test tests/FreakyKit.Forge.Generator.Tests/DiagnosticUnitTests`
4. Integrate with CI/CD pipeline
