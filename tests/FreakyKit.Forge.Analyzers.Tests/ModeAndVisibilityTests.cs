using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF001, FKF002, FKF010, FKF011 — Mode and Visibility diagnostics.
/// </summary>
public sealed class ModeAndVisibilityTests : AnalyzerTestBase
{
    // ─── FKF001: Explicit mode activated ─────────────────────────────────────

    [Fact]
    public void FKF001_ExplicitMode_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF001");
        AssertDiagnosticSeverity(source, "FKF001", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF001_ImplicitMode_NoInfo() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF001");

    // ─── FKF002: Method ignored in explicit mode ──────────────────────────────

    [Fact]
    public void FKF002_ExplicitMode_UnmarkedMethodGetsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF002");
        AssertDiagnosticSeverity(source, "FKF002", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF002_ExplicitMode_MarkedMethodNoWarning() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF002");

    [Fact]
    public void FKF002_ImplicitMode_NoWarning() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF002");

    // ─── FKF010: Private method ignored ──────────────────────────────────────

    [Fact]
    public void FKF010_PrivateMethod_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    private static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF010");
        AssertDiagnosticSeverity(source, "FKF010", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF010_PublicMethod_NoWarning() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF010");

    [Fact]
    public void FKF010_InternalMethod_NoWarning() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    internal static partial Dest ToDest(Source source);
                }
            }
            """, "FKF010");

    // ─── FKF011: Private visibility enabled ──────────────────────────────────

    [Fact]
    public void FKF011_IncludePrivateMethods_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass(IncludePrivateMethods = true)]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF011");
        AssertDiagnosticSeverity(source, "FKF011", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF011_DefaultConfig_NoInfo() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF011");
}
