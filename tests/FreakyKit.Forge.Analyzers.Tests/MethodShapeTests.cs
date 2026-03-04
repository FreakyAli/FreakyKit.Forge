using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF020, FKF030 — Method Shape diagnostics.
/// </summary>
public sealed class MethodShapeTests : AnalyzerTestBase
{
    // ─── FKF020: Forge method declares a body ─────────────────────────────────

    [Fact]
    public void FKF020_ForgeMethodWithBody_EmitsError()
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
                    public static partial Dest ToDest(Source source)
                    {
                        return new Dest();
                    }
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF020");
    }

    [Fact]
    public void FKF020_ForgeMethodWithExpressionBody_EmitsError()
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
                    public static partial Dest ToDest(Source source) => new Dest();
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF020");
    }

    [Fact]
    public void FKF020_ForgeMethodNoBody_NoError() =>
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
            """, "FKF020");

    // ─── FKF030: Forge method name overloaded ────────────────────────────────

    [Fact]
    public void FKF030_OverloadedForgeName_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class SourceA { public string Name { get; set; } = ""; }
                public class SourceB { public string Name { get; set; } = ""; }
                public class Dest    { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest Map(SourceA source);
                    public static partial Dest Map(SourceB source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF030");
    }

    [Fact]
    public void FKF030_UniqueForgeNames_NoError() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class SourceA { public string Name { get; set; } = ""; }
                public class DestA   { public string Name { get; set; } = ""; }
                public class SourceB { public string Name { get; set; } = ""; }
                public class DestB   { public string Name { get; set; } = ""; }
                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial DestA MapA(SourceA source);
                    public static partial DestB MapB(SourceB source);
                }
            }
            """, "FKF030");
}
