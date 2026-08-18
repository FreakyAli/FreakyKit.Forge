using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Member Discovery diagnostics (FKF400–FKF401).
/// Tests field inclusion and discovery options.
/// </summary>
public sealed class MemberDiscoveryDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF400_FieldIgnored_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public string Value = ""; }
                public class Dest { public string Value = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF400");
        AssertDiagnosticWithSeverity(source, "FKF400", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF400_FieldIncluded_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public string Value = ""; }
                public class Dest { public string Value = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF400");
    }

    [Fact]
    public void FKF401_FieldsEnabled_EmitsInfo()
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
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF401");
        AssertDiagnosticWithSeverity(source, "FKF401", DiagnosticSeverity.Info);
    }
}
