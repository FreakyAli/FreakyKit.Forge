using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF100, FKF101 — Member Matching diagnostics.
/// </summary>
public sealed class MemberMatchingTests : AnalyzerTestBase
{
    // ─── FKF100: Destination member missing source ────────────────────────────

    [Fact]
    public void FKF100_DestMemberHasNoSourceCounterpart_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name  { get; set; } = "";
                    public int    Score { get; set; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void FKF100_AllDestMembersHaveSources_NoWarning() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Score { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Score { get; set; } }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF100");

    // ─── FKF101: Source member unused ────────────────────────────────────────

    [Fact]
    public void FKF101_SourceMemberHasNoDestCounterpart_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name  { get; set; } = "";
                    public int    Score { get; set; }
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void FKF101_AllSourceMembersHaveDests_NoWarning() =>
        AssertNotContainsDiagnostic("""
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
            """, "FKF101");

    // ─── Read-only destination members should not trigger FKF100 ───────────────

    [Fact]
    public void FKF100_ReadOnlyDestProperty_NoSourceMatch_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Id { get; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void FKF100_ReadOnlyField_NoSourceMatch_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name = ""; }
                public class Dest
                {
                    public string Name = "";
                    public readonly int Id = 0;
                }
                [Forge]
                [ForgeOptions(IncludeFields = true)]
                public static partial class MyForges
                {
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void FKF100_WritableDestProperty_NoSourceMatch_StillWarns()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Score { get; set; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF100");
    }
}
