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
}
