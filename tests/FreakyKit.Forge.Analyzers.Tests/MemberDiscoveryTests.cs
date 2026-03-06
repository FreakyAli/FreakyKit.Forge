using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF400, FKF401 — Member Discovery diagnostics.
/// </summary>
public sealed class MemberDiscoveryTests : AnalyzerTestBase
{
    // ─── FKF400: Field ignored ────────────────────────────────────────────────

    [Fact]
    public void FKF400_FieldOnSourceIgnored_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int score;
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF400");
        AssertDiagnosticSeverity(source, "FKF400", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF400_NoFieldsOnSource_NoWarning() =>
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
            """, "FKF400");

    // ─── FKF401: Fields enabled ───────────────────────────────────────────────

    [Fact]
    public void FKF401_ShouldIncludeFields_EmitsInfo()
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
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF401");
        AssertDiagnosticSeverity(source, "FKF401", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF401_ShouldIncludeFieldsFalse_NoInfo() =>
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
            """, "FKF401");
}
