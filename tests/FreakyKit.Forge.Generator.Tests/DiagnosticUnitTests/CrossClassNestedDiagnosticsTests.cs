using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Cross-Class Nested Forge diagnostics (FKF520–FKF528).
/// Tests [ForgeUses] validation, included class checks, and attribute placement.
/// </summary>
public sealed class CrossClassNestedDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF520_IncludedForgeClassNotFound_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                [ForgeUses(typeof(NonExistentForges))]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF520");
        AssertDiagnosticWithSeverity(source, "FKF520", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF521_IncludedClassNotForge_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                public static partial class NotAForge { }

                [Forge]
                [ForgeUses(typeof(NotAForge))]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF521");
        AssertDiagnosticWithSeverity(source, "FKF521", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF524_ForgeUsesWithoutForge_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial class HelperForges { }

                [ForgeUses(typeof(HelperForges))]
                public static partial class MyForges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF524");
        AssertDiagnosticWithSeverity(source, "FKF524", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF525_ForgeMethodWithoutForgeClass_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                public static partial class NotAForge
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF525");
        AssertDiagnosticWithSeverity(source, "FKF525", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF527_ForgeMapOnSourceType_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("Name")]
                    public string Value { get; set; } = "";
                }

                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF527");
        AssertDiagnosticWithSeverity(source, "FKF527", DiagnosticSeverity.Warning);
    }
}
