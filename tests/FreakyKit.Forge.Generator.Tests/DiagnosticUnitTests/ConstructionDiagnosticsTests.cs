using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Construction diagnostics (FKF500–FKF509).
/// Tests constructor selection, validation, and expression generation.
/// </summary>
public sealed class ConstructionDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF500_ConstructorAmbiguity_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int X { get; set; } public int Y { get; set; } }

                public class Dest
                {
                    public int X { get; }
                    public int Y { get; }
                    public Dest(int x, int y) { X = x; Y = y; }
                    public Dest(int x) { X = x; Y = 0; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF500");
        AssertDiagnosticWithSeverity(source, "FKF500", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF501_MissingConstructorParameter_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int X { get; set; } }

                public class Dest
                {
                    public int X { get; }
                    public int Y { get; }
                    public Dest(int x, int y) { X = x; Y = y; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF501");
        AssertDiagnosticWithSeverity(source, "FKF501", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF502_NoViableConstructor_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int X { get; set; } }

                public class Dest
                {
                    private Dest() { }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF502");
        AssertDiagnosticWithSeverity(source, "FKF502", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF503_AbstractClassNotInstantiable_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public abstract class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF503");
        AssertDiagnosticWithSeverity(source, "FKF503", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF504_GenerateExpressionOnUpdateMethod_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial void Update(Source s, Dest d);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF504");
        AssertDiagnosticWithSeverity(source, "FKF504", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF505_HooksIgnoredInExpression_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial Dest Map(Source s);

                    static partial void OnBeforeMap(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF505");
        AssertDiagnosticWithSeverity(source, "FKF505", DiagnosticSeverity.Warning);
    }
}
