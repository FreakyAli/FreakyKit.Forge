using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Conditional/Predicate Mapping diagnostics (FKF510–FKF513).
/// Tests condition method validation and shadowing.
/// </summary>
public sealed class ConditionalMappingDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF510_ConditionMethodNotFound_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", Condition = nameof(MissingMethod))]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF510");
        AssertDiagnosticWithSeverity(source, "FKF510", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF511_ConditionMethodInvalidSignature_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", Condition = nameof(Forges.IsValid))]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);

                    internal static bool IsValid() => true;
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF511");
        AssertDiagnosticWithSeverity(source, "FKF511", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF512_ConditionMethodNotAccessible_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", Condition = nameof(Forges.IsValid))]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);

                    private static bool IsValid(Source s) => true;
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF512");
        AssertDiagnosticWithSeverity(source, "FKF512", DiagnosticSeverity.Error);
    }
}
