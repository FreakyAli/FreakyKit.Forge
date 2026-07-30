using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for the Projection Expressions feature (Phase 1).
/// Scope: parameterless constructor + same-type direct property assignments only.
/// </summary>
public sealed class ExpressionGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void GenerateExpression_HappyPath_EmitsExpressionProperty()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }
                [Forge]
                public static partial class PersonForges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        Assert.Contains("public static Expression<Func<Person, PersonDto>> ToDtoExpression", generated);
        Assert.Contains("source => new PersonDto", generated);
        Assert.Contains("Name = source.Name", generated);
        Assert.Contains("Age = source.Age", generated);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
    }

    [Fact]
    public void GenerateExpression_AddsLinqExpressionsUsing()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("using System.Linq.Expressions;", generated);
    }

    [Fact]
    public void GenerateExpression_NotSet_OmitsLinqExpressionsUsing()
    {
        // Sanity: the new using must NOT appear unless a method opted in.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.DoesNotContain("using System.Linq.Expressions;", generated);
        Assert.DoesNotContain("Expression<Func<", generated);
    }

    [Fact]
    public void GenerateExpression_PropertyNameMatchesConvention()
    {
        // ToDto → ToDtoExpression; FooBar → FooBarExpression
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B FooBar(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("FooBarExpression", generated);
    }

    [Fact]
    public void GenerateExpression_LambdaParameterMatchesSourceParameterName()
    {
        // The user declared the source param as `person`, not `source`. The lambda must use it.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public int Age { get; set; } }
                public class PersonDto { public int Age { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial PersonDto ToDto(Person person);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("person => new PersonDto", generated);
        Assert.Contains("Age = person.Age", generated);
    }

    [Fact]
    public void GenerateExpression_OnUpdateMethod_EmitsFKF504()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial void Update(A source, B existing);
                }
            }
            """;

        AssertHasError(RunGenerator(source), "FKF504");
    }

    [Fact]
    public void GenerateExpression_OnUpdateMethod_SuppressesExpressionProperty()
    {
        // Even though FKF504 is emitted, the imperative update body must still generate.
        // The expression property must NOT be emitted (it's invalid for update shape).
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial void Update(A source, B existing);
                }
            }
            """;

        var result = RunGenerator(source);
        // Generator emits no source when there's an error diagnostic, per the existing policy.
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void GenerateExpression_WithBeforeHook_EmitsFKF505_Warning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                    static partial void OnBeforeMap(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF505");
        // Expression property still emitted — the warning is non-blocking
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("MapExpression", generated);
    }

    [Fact]
    public void GenerateExpression_WithAfterHook_EmitsFKF505_Warning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                    static partial void OnAfterMap(A source, B result);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF505");
    }

    [Fact]
    public void GenerateExpression_WithoutHooks_DoesNotEmitFKF505()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF505");
    }

    [Fact]
    public void GenerateExpression_UntranslatableMembers_ExcludedFromExpressionOnly()
    {
        // Per-member IgnoreIfNull has no expression-tree equivalent. The imperative body still
        // wraps the assignment in `if`; the expression property excludes that member but keeps
        // the others.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A
                {
                    public string Name { get; set; } = "";
                    [ForgeMap("Tag", IgnoreIfNull = ForgePolicy.True)] public string? Tag { get; set; }
                }
                public class B { public string Name { get; set; } = ""; public string? Tag { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);

        // Imperative body still wraps Tag in if
        Assert.Contains("if (source.Tag != null)", generated);

        // Expression property emitted with Name only — Tag is excluded
        Assert.Contains("MapExpression", generated);
        Assert.Contains("Name = source.Name", generated);

        var exprStart = generated.IndexOf("source => new B");
        Assert.True(exprStart >= 0, "Expression property body not found");
        var exprEnd = generated.IndexOf("};", exprStart);
        var exprBody = generated.Substring(exprStart, exprEnd - exprStart);
        Assert.DoesNotContain("Tag", exprBody);
    }

    [Fact]
    public void GenerateExpression_ImperativeBodyStillEmitted()
    {
        // The expression property is additive — the imperative method body must still be there.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("public static partial B Map(A source)", generated);
        Assert.Contains("var __result = new B();", generated);
        Assert.Contains("__result.X = source.X;", generated);
        Assert.Contains("return __result;", generated);
    }

    [Fact]
    public void GenerateExpression_UpdateMethod_NoExpressionProperty()
    {
        // Update methods cannot have expressions (no return value). FKF504 fires and the entire
        // class is suppressed (existing error-blocks-generation policy).
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; }
                public class B { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial void Update(A source, B existing);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF504");
        AssertNoGeneratedSource(result);
    }

    // ─── Phase 2: Nullable + ForgeMap + ForgeIgnore + IgnoreIfNull + Converter ───

    [Fact]
    public void Phase2_NullableValueToNonNullable_UsesGetValueOrDefault()
    {
        // Imperative uses .Value (and emits FKF201). Expression mode prefers GetValueOrDefault()
        // so the same .Compile()'d expression doesn't throw on null input.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int? Age { get; set; } }
                public class B { public int Age { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));

        // Imperative uses .Value
        Assert.Contains("__result.Age = source.Age.Value;", generated);

        // Expression uses GetValueOrDefault()
        Assert.Contains("Age = source.Age.GetValueOrDefault()", generated);
    }

    [Fact]
    public void Phase2_NullableWithDefaultValue_UsesNullCoalescing()
    {
        // [ForgeMap(DefaultValue = ...)] on either side: the imperative path uses `?? defaultLiteral`
        // (no FKF201 emitted). Expression mode mirrors that exactly.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { [ForgeMap("Age", DefaultValue = -1)] public int? Age { get; set; } }
                public class B { public int Age { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("Age = source.Age ?? -1", generated);
    }

    [Fact]
    public void Phase2_NonNullableToNullable_DirectAssignment()
    {
        // T → Nullable<T> is implicit; same expression on both paths.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int Age { get; set; } }
                public class B { public int? Age { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Both paths use direct assignment
        Assert.Contains("Age = source.Age", generated);
        // Should NOT contain GetValueOrDefault — only Nullable→Non-nullable triggers that path
        Assert.DoesNotContain("GetValueOrDefault", generated);
    }

    [Fact]
    public void Phase2_ForgeMapRename_AppliesToExpression()
    {
        // [ForgeMap("Name")] redirects FirstName → Name. The dest member 'Name' is the LHS
        // in the expression; the RHS still reads from source.FirstName.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { [ForgeMap("Name")] public string FirstName { get; set; } = ""; }
                public class B { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("Name = source.FirstName", generated);
    }

    [Fact]
    public void Phase2_ForgeIgnore_ExcludesFromBothImperativeAndExpression()
    {
        // [ForgeIgnore] removes the member from both paths — never reaches the resolution chain.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore] public string Internal { get; set; } = "";
                }
                public class B
                {
                    public string Name { get; set; } = "";
                    public string Internal { get; set; } = "";
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Internal absent from both bodies; Name present in both.
        Assert.Contains("Name = source.Name", generated);
        // Make sure Internal is fully absent — including in the expression body
        var exprStart = generated.IndexOf("source => new B");
        Assert.True(exprStart >= 0);
        var exprEnd = generated.IndexOf("};", exprStart);
        var exprBody = generated.Substring(exprStart, exprEnd - exprStart);
        Assert.DoesNotContain("Internal", exprBody);
    }

    [Fact]
    public void Phase2_IgnoreIfNull_OnMember_ExcludedFromExpression_FKF506()
    {
        // IgnoreIfNull = "skip assignment when source is null" has no expression-tree equivalent.
        // The imperative method wraps the assignment in `if`; the expression omits the member.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)] public string? Name { get; set; } }
                public class B { public string? Name { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF506");
        var generated = AssertSingleGeneratedFile(result);
        // Imperative still has the conditional assignment
        Assert.Contains("if (source.Name != null)", generated);
        // Expression omits Name entirely — since it was the only member, the property is suppressed
        Assert.DoesNotContain("MapExpression", generated);
    }

    [Fact]
    public void Phase2_IgnoreIfNull_MethodLevel_AllMembersExcluded_FKF506_PerMember()
    {
        // [ForgeMethod(IgnoreIfNull = ForgePolicy.True)] applies to every assignment.
        // Expression mode excludes every member with one FKF506 per member.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string? Name { get; set; } public int? Age { get; set; } }
                public class B { public string? Name { get; set; } public int? Age { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true, IgnoreIfNull = ForgePolicy.True)]
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        var fkf506Count = result.Diagnostics.Count(d => d.Id == "FKF506");
        Assert.Equal(2, fkf506Count);
    }

    [Fact]
    public void Phase2_CustomConverter_ExcludedFromExpression_FKF506()
    {
        // User-defined static methods are not translatable to SQL. Expression omits with FKF506.
        const string source = """
            using FreakyKit.Forge;
            using System;
            namespace TestNs
            {
                public class A { public DateTime CreatedAt { get; set; } }
                public class B { public string CreatedAt { get; set; } = ""; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);

                    [ForgeConverter]
                    public static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd");
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF506");
        var generated = AssertSingleGeneratedFile(result);
        // Imperative still calls the converter
        Assert.Contains("__result.CreatedAt = FormatDate(source.CreatedAt);", generated);
        // CreatedAt was the only member — expression property is suppressed
        Assert.DoesNotContain("MapExpression", generated);
    }

    [Fact]
    public void Phase2_FKF506_NotEmittedWhenGenerateExpressionFalse()
    {
        // FKF506 is only meaningful when GenerateExpression is set — otherwise there's no
        // expression to be excluded from. Don't emit FKF506 noise on plain forge methods.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)] public string? Name { get; set; } }
                public class B { public string? Name { get; set; } }
                [Forge]
                public static partial class F
                {
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF506");
    }

    // ─── Phase 7: Flattening ─────────────────────────────────────────────────

    [Fact]
    public void Phase7_Flattening_ReferenceTypeIntermediate_UsesNullGuardTernary()
    {
        // Imperative: __result.AddressCity = source.Address?.City
        // Expression: AddressCity = source.Address == null ? null : source.Address.City
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class A { public Address Home { get; set; } = new(); }
                public class B { public string HomeCity { get; set; } = ""; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true, AllowFlattening = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Imperative
        Assert.Contains("__result.HomeCity = source.Home?.City;", generated);
        // Expression — null-conditional converted to ternary
        Assert.Contains("HomeCity = source.Home == null ? null : source.Home.City", generated);
    }

    [Fact]
    public void Phase7_Flattening_ValueTypeIntermediate_NoNullGuard()
    {
        // Value-type intermediate (struct) — no `?.` in imperative, no ternary in expression.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public struct Coord { public double Lat { get; set; } }
                public class A { public Coord Position { get; set; } }
                public class B { public double PositionLat { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true, AllowFlattening = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Imperative: direct chain (no ?.)
        Assert.Contains("__result.PositionLat = source.Position.Lat;", generated);
        // Expression: same chain, no ternary
        Assert.Contains("PositionLat = source.Position.Lat", generated);
        Assert.DoesNotContain("source.Position == null", generated);
    }

    // ─── Phase 6: Collection mapping ─────────────────────────────────────────

    [Fact]
    public void Phase6_SameElementCollection_DifferentContainers_List()
    {
        // Source: List<int>, Dest: IList<int>. Same element type, different container — hits the
        // collection-mapping branch with .ToList() materializer.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class A { public List<int> Tags { get; set; } = new(); }
                public class B { public IList<int> Tags { get; set; } = new List<int>(); }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("Tags = source.Tags != null ? source.Tags.ToList() : null", generated);
    }

    [Fact]
    public void Phase6_SameElementCollection_ListToArray_Translatable()
    {
        // Source: List<int>, Dest: int[]. Hits the collection-mapping branch with .ToArray().
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class A { public List<int> Tags { get; set; } = new(); }
                public class B { public int[] Tags { get; set; } = new int[0]; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("Tags = source.Tags != null ? source.Tags.ToArray() : null", generated);
    }

    [Fact]
    public void Phase6_DifferentElement_List_InlinesElementBody()
    {
        // List<Item> -> List<ItemDto>, with ToItemDto forge method. Expression must inline.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Item { public string Name { get; set; } = ""; public int Stock { get; set; } }
                public class ItemDto { public string Name { get; set; } = ""; public int Stock { get; set; } }
                public class Source { public List<Item> Items { get; set; } = new(); }
                public class Dest { public List<ItemDto> Items { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    public static partial ItemDto ToItemDto(Item source);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dest Map(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Imperative path: Select with call to ToItemDto
        Assert.Contains(".Select(x => ToItemDto(x)).ToList()", generated);
        // Expression path: Select with inlined element body — no method call, no Invoke
        Assert.Contains(".Select(x => new ItemDto", generated);
        Assert.Contains("Name = x.Name", generated);
        Assert.Contains("Stock = x.Stock", generated);
        // Outer null guard on the collection itself
        Assert.Contains("source.Items == null ? null :", generated);
        // No Invoke anywhere — EF can't translate Invoke
        Assert.DoesNotContain("Invoke", generated);
    }

    [Fact]
    public void Phase6_ListToHashSet_NotTranslatable_EmitsFKF506()
    {
        // List<int> → HashSet<int>: hits collection branch with .ToHashSet() materializer.
        // EF doesn't translate ToHashSet → FKF506.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public List<int> Tags { get; set; } = new(); }
                public class B { public string Name { get; set; } = ""; public HashSet<int> Tags { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF506");
        var generated = AssertSingleGeneratedFile(result);

        // Imperative still emits .ToHashSet()
        Assert.Contains(".ToHashSet()", generated);

        // Expression body has only Name — Tags is excluded
        var exprStart = generated.IndexOf("source => new B");
        Assert.True(exprStart >= 0);
        var exprEnd = generated.IndexOf("};", exprStart);
        var exprBody = generated.Substring(exprStart, exprEnd - exprStart);
        Assert.Contains("Name = source.Name", exprBody);
        Assert.DoesNotContain("Tags", exprBody);
    }

    [Fact]
    public void Phase6_ListToImmutableArray_NotTranslatable_EmitsFKF506()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            using System.Collections.Immutable;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public List<int> Codes { get; set; } = new(); }
                public class B { public string Name { get; set; } = ""; public ImmutableArray<int> Codes { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF506");
    }

    // ─── Phase 5: Nested forging (string-level inlining) ─────────────────────

    [Fact]
    public void Phase5_NestedForge_InlinedIntoExpression()
    {
        // Imperative: __result.Home = source.Home != null ? ToAddressDto(source.Home) : null;
        // Expression: Home = source.Home == null ? null : new AddressDto { City = source.Home.City, ... }
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; public string Zip { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; public string Zip { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Imperative path: nested method call
        Assert.Contains("source.Home != null ? ToAddressDto(source.Home) : null", generated);

        // Expression path: inlined with null-guard ternary
        Assert.Contains("Home = source.Home == null ? null : new AddressDto", generated);
        Assert.Contains("City = source.Home.City", generated);
        Assert.Contains("Zip = source.Home.Zip", generated);
    }

    [Fact]
    public void Phase5_NestedForge_OuterMethodAlsoEmitsExpressionForChild()
    {
        // When the nested method also has GenerateExpression = true, BOTH expression properties exist.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("public static Expression<Func<Address, AddressDto>> ToAddressDtoExpression", generated);
        Assert.Contains("public static Expression<Func<Person, PersonDto>> ToDtoExpression", generated);
        // Outer still INLINES (not Invoke) the inner expression body — EF can't translate Invoke
        Assert.Contains("Home = source.Home == null ? null : new AddressDto", generated);
        Assert.DoesNotContain("Invoke", generated);
    }

    [Fact]
    public void Phase5_NestedForge_TwoLevelsDeep_InlinedRecursively()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Coord { public double Lat { get; set; } public double Lng { get; set; } }
                public class CoordDto { public double Lat { get; set; } public double Lng { get; set; } }
                public class Address { public string City { get; set; } = ""; public Coord Position { get; set; } = new(); }
                public class AddressDto { public string City { get; set; } = ""; public CoordDto Position { get; set; } = new(); }
                public class Person { public Address Home { get; set; } = new(); }
                public class PersonDto { public AddressDto Home { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    public static partial CoordDto ToCoordDto(Coord source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Two-deep inlining: Person → Address → Coord
        Assert.Contains("Home = source.Home == null ? null : new AddressDto", generated);
        Assert.Contains("Position = source.Home.Position == null ? null : new CoordDto", generated);
        Assert.Contains("Lat = source.Home.Position.Lat", generated);
        Assert.Contains("Lng = source.Home.Position.Lng", generated);
    }

    [Fact]
    public void Phase5_NestedForge_CycleEmitsFKF301_BlocksGeneration()
    {
        // Address.Parent references Address → ToAddressDto inlining loops forever.
        // Now detected as FKF301 (circular nested forge) instead of FKF507 (expression inlining cycle).
        const string source = """
            using FreakyKit.Forge;
            #nullable enable
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; public Address Parent { get; set; } = null!; }
                public class AddressDto { public string City { get; set; } = ""; public AddressDto Parent { get; set; } = null!; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF301");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void Phase5_NestedForge_ValueTypeSource_NoNullGuard()
    {
        // Source.Position is a struct — no null guard ternary needed in the inlined expression.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public struct Coord { public double Lat { get; set; } public double Lng { get; set; } }
                public struct CoordDto { public double Lat { get; set; } public double Lng { get; set; } }
                public class A { public Coord Position { get; set; } }
                public class B { public CoordDto Position { get; set; } }
                [Forge]
                public static partial class F
                {
                    public static partial CoordDto ToCoordDto(Coord source);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // No null guard for value-type sources
        Assert.Contains("Position = new CoordDto", generated);
        Assert.Contains("Lat = source.Position.Lat", generated);
        Assert.Contains("Lng = source.Position.Lng", generated);
        Assert.DoesNotContain("source.Position == null", generated);
    }

    // ─── Phase 4: Constructor mapping ────────────────────────────────────────

    [Fact]
    public void Phase4_ParameterizedConstructor_EmittedInExpression()
    {
        // `new Dest(args)` is valid in expression-tree lambdas (compiles to Expression.New).
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class B
                {
                    public string Name { get; }
                    public int Age { get; }
                    public B(string name, int age) { Name = name; Age = age; }
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("MapExpression", generated);
        Assert.Contains("source => new B(source.Name, source.Age);", generated);
    }

    [Fact]
    public void Phase4_ParameterizedCtorWithExtraProps_EmittedAsInitializer()
    {
        // Ctor satisfies some members, others are settable properties — expression uses `new X(args) { Y = ... }`.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public int Age { get; set; } public string Email { get; set; } = ""; }
                public class B
                {
                    public string Name { get; }
                    public int Age { get; }
                    public string Email { get; set; } = "";
                    public B(string name, int age) { Name = name; Age = age; }
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("source => new B(source.Name, source.Age)", generated);
        Assert.Contains("Email = source.Email", generated);
    }

    [Fact]
    public void Phase4_ParameterizedCtor_WithNullable_UsesGetValueOrDefault()
    {
        // Nullable args in ctor must use GetValueOrDefault() in expression mode, same as property assignments.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public int? Age { get; set; } }
                public class B
                {
                    public string Name { get; }
                    public int Age { get; }
                    public B(string name, int age) { Name = name; Age = age; }
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Imperative uses .Value
        Assert.Contains("var __result = new B(source.Name, source.Age.Value);", generated);
        // Expression uses GetValueOrDefault()
        Assert.Contains("source => new B(source.Name, source.Age.GetValueOrDefault());", generated);
    }

    [Fact]
    public void Phase4_InitOnlyProperties_EmittedAsObjectInitializer()
    {
        // Init-only properties use object-initializer syntax in both imperative and expression modes.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class B { public string Name { get; init; } = ""; public int Age { get; init; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("MapExpression", generated);
        // Expression must use object-initializer syntax (init-only props)
        Assert.Contains("source => new B", generated);
        Assert.Contains("Name = source.Name", generated);
        Assert.Contains("Age = source.Age", generated);
    }

    [Fact]
    public void Phase4_Record_PositionalParams_EmittedInExpression()
    {
        // Records with positional parameters → parameterized ctor in both paths.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public int Age { get; set; } }
                public record B(string Name, int Age);
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("MapExpression", generated);
        Assert.Contains("source => new B(source.Name, source.Age)", generated);
    }

    // ─── Phase 3: Enum mapping ───────────────────────────────────────────────

    [Fact]
    public void Phase3_EnumCast_DefaultStrategy_EmittedInExpression()
    {
        // Cast strategy is the default. Same `(DestEnum)source.X` in both paths.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SrcKind { Free, Pro, Enterprise }
                public enum DestKind { Free, Pro, Enterprise }
                public class A { public SrcKind Kind { get; set; } }
                public class B { public DestKind Kind { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("MapExpression", generated);
        Assert.Contains("Kind = (DestKind)source.Kind", generated);
    }

    [Fact]
    public void Phase3_EnumByName_EmitsChainedTernary()
    {
        // ByName must emit a chained conditional in the expression (switch expressions are
        // not allowed inside Expression<Func<,>>). Imperative still uses the switch.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SrcStatus { Active, Inactive, Banned }
                public enum DestStatus { Active, Inactive, Banned }
                public class A { public SrcStatus Status { get; set; } }
                public class B { public DestStatus Status { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true, MappingStrategy = ForgeMapping.ByName)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));

        // Imperative uses the switch (unchanged)
        Assert.Contains("source.Status switch", generated);

        // Expression uses chained ternary
        Assert.Contains("source.Status == SrcStatus.Active ? DestStatus.Active", generated);
        Assert.Contains("source.Status == SrcStatus.Inactive ? DestStatus.Inactive", generated);
        Assert.Contains("source.Status == SrcStatus.Banned ? DestStatus.Banned", generated);
        Assert.Contains(": default(DestStatus)", generated);
    }

    [Fact]
    public void Phase3_EnumByName_DestMissingMember_FallsToDefault_NoThrow()
    {
        // Source has a value the destination doesn't. Imperative emits a throw arm in the switch
        // (and FKF212). Expression mode cannot throw — falls to the default arm instead. The
        // imperative behavior is preserved by the imperative method; the expression is a best-effort
        // SQL-translatable approximation.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SrcStatus { Active, Inactive, Legacy }
                public enum DestStatus { Active, Inactive }
                public class A { public SrcStatus Status { get; set; } }
                public class B { public DestStatus Status { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true, MappingStrategy = ForgeMapping.ByName)]
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF212"); // Legacy missing in dest
        var generated = AssertSingleGeneratedFile(result);

        // Imperative throw is still there
        Assert.Contains("throw new InvalidOperationException", generated);

        // Expression body has only the two mapped arms + default — no throw
        var exprStart = generated.IndexOf("source => new B");
        Assert.True(exprStart >= 0);
        var exprEnd = generated.IndexOf("};", exprStart);
        var exprBody = generated.Substring(exprStart, exprEnd - exprStart);
        Assert.Contains("source.Status == SrcStatus.Active ? DestStatus.Active", exprBody);
        Assert.Contains("source.Status == SrcStatus.Inactive ? DestStatus.Inactive", exprBody);
        Assert.DoesNotContain("Legacy", exprBody);
        Assert.DoesNotContain("throw", exprBody);
        Assert.Contains("default(DestStatus)", exprBody);
    }

    [Fact]
    public void Phase2_NullableExpressionProperty_StillEmitted_WhenAtLeastOneMemberTranslates()
    {
        // Mixed scenario: one nullable, one same-type. Both must appear in the expression.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public string Name { get; set; } = ""; public int? Age { get; set; } }
                public class B { public string Name { get; set; } = ""; public int Age { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("MapExpression", generated);
        Assert.Contains("Name = source.Name", generated);
        Assert.Contains("Age = source.Age.GetValueOrDefault()", generated);
    }
}
