using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF042, FKF107, FKF108, FKF109.
/// </summary>
public sealed class NewMemberDiagnosticsTests : AnalyzerTestBase
{
    // ─── FKF042: Zero members mapped ─────────────────────────────────────────

    [Fact]
    public void FKF042_NoMatchingMembers_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Foo { get; set; } = ""; }
                public class Dest   { public string Bar { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF042");
    }

    [Fact]
    public void FKF042_HasMatchingMembers_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF042");
    }

    [Fact]
    public void FKF042_CollectionProjection_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDto(Source source);
                    public static partial List<Dest> ToDtos(List<Source> sources);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF042");
    }

    [Fact]
    public void FKF042_FlattenedMatchOnly_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCity { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF042");
    }

    [Fact]
    public void FKF101_FlattenedSourceNavProperty_NoWarning()
    {
        // Source.Address is consumed via flattening to AddressCity —
        // it must NOT appear as an unused source member (FKF101).
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCity { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF101");
    }

    // ─── FKF107: Read-only destination member skipped ────────────────────────

    [Fact]
    public void FKF107_ReadOnlyDestMemberHasSourceMatch_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF107");
    }

    [Fact]
    public void FKF107_ReadOnlyDestMemberNoSourceMatch_NoInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Other { get; set; } = ""; }
                public class Dest   { public string Name { get; } = ""; public string Other { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF107");
    }

    [Fact]
    public void FKF107_SettableDestMember_NoInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF107");
    }

    [Fact]
    public void FKF107_ReadOnlyMemberBoundViaConstructor_NoInfo()
    {
        // Dest.Name is get-only but is satisfied by the parameterized constructor —
        // it is NOT skipped; the generator will produce new Dest(source.Name).
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; }
                    public Dest(string name) { Name = name; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF107");
    }

    [Fact]
    public void FKF042_ConstructorOnlyDest_NoWarning()
    {
        // All members are get-only and bound via the constructor — zero property
        // assignments does NOT mean zero members mapped.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest
                {
                    public string Name { get; }
                    public int Age { get; }
                    public Dest(string name, int age) { Name = name; Age = age; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF042");
    }

    // ─── FKF108: Write-only source member skipped ────────────────────────────

    [Fact]
    public void FKF108_WriteOnlySourceMember_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public string WriteOnly { set { } }
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF108");
    }

    [Fact]
    public void FKF108_PrivateGetterSourceMember_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public string Hidden { private get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF108");
    }

    [Fact]
    public void FKF108_NormalSourceMember_NoInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF108");
    }

    // ─── FKF109: Member both ignored and explicitly mapped ───────────────────

    [Fact]
    public void FKF109_MemberHasBothIgnoreAndMap_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeIgnore]
                    [ForgeMap("Name")]
                    public string FirstName { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF109");
    }

    [Fact]
    public void FKF109_OnlyIgnore_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore] public string Internal { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF109");
    }

    [Fact]
    public void FKF109_OnlyMap_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("Name")]
                    public string FirstName { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF109");
    }

    [Fact]
    public void FKF109_FieldHasBothIgnoreAndMap_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeIgnore]
                    [ForgeMap("Name")]
                    public string firstName = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF109");
    }

    [Fact]
    public void FKF109_FieldOnlyIgnore_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore] public string internalId = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF109");
    }

    // ─── FKF107 + init-only ───────────────────────────────────────────────────

    [Fact]
    public void FKF107_InitOnlyDestMember_CreateMethod_NoInfo()
    {
        // Init-only properties are writable via object initializer in create methods —
        // the generator uses object-initializer syntax, so they are NOT skipped. No FKF107.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; init; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF107");
    }

    [Fact]
    public void FKF107_InitOnlyDestMember_UpdateMethod_EmitsInfo()
    {
        // Init-only properties cannot be assigned in update methods (no object initializer).
        // They ARE considered read-only in update context, so FKF107 fires.
        // Dest has one settable member (Other) to prevent FKF041 (no settable members at all)
        // from swallowing the FKF107 on the init-only Name.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public string Other { get; set; } = ""; }
                public class Dest   { public string Name { get; init; } = ""; public string Other { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest dest);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF107");
    }

    // ─── FKF108 + init-only source ───────────────────────────────────────────

    [Fact]
    public void FKF108_InitOnlySourceMember_NoInfo()
    {
        // Init-only source properties have a public getter — they are readable.
        // FKF108 must NOT fire.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public string Code { get; init; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; public string Code { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF108");
    }
}
