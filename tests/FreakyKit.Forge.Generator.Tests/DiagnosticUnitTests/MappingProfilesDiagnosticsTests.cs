using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Mapping Profiles / Inheritance diagnostics (FKF533–FKF542).
/// Tests [ForgeIncludes] validation and base-type mapping inheritance.
/// </summary>
public sealed class MappingProfilesDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF533_IncludedProfileClassNotFound_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                [ForgeIncludes(typeof(NonExistentProfile))]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF533");
        AssertDiagnosticWithSeverity(source, "FKF533", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF534_IncludedProfileNotForge_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                public static partial class NotAProfile { }

                [Forge]
                [ForgeIncludes(typeof(NotAProfile))]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF534");
        AssertDiagnosticWithSeverity(source, "FKF534", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF535_CircularForgeIncludes_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                [ForgeIncludes(typeof(ForgesB))]
                public static partial class ForgesA { }

                [Forge]
                [ForgeIncludes(typeof(ForgesA))]
                public static partial class ForgesB { }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF535");
        AssertDiagnosticWithSeverity(source, "FKF535", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF538_ForgeIncludesWithoutForge_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial class BaseProfile { }

                [ForgeIncludes(typeof(BaseProfile))]
                public static partial class DerivedForges { }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF538");
        AssertDiagnosticWithSeverity(source, "FKF538", DiagnosticSeverity.Error);
    }
}
