using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Type Safety diagnostics (FKF200–FKF230, FKF316).
/// Tests type compatibility, nullable handling, enum mapping, and converters.
/// </summary>
public sealed class TypeSafetyDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF200_IncompatibleTypes_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public string Value { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF200");
        AssertDiagnosticWithSeverity(source, "FKF200", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF201_NullableToNonNullable_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int? Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF201");
        AssertDiagnosticWithSeverity(source, "FKF201", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF202_NullableMapping_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int? Value { get; set; } }
                public class Dest { public int? Value { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF202");
        AssertDiagnosticWithSeverity(source, "FKF202", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF210_EnumCastMapping_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus { Active, Inactive }

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF210");
        AssertDiagnosticWithSeverity(source, "FKF210", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF211_EnumNameBasedMapping_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus { Active, Inactive }

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF211");
        AssertDiagnosticWithSeverity(source, "FKF211", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF230_EnumStringMapping_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public enum Status { Active, Inactive }

                public class Source { public Status Status { get; set; } }
                public class Dest { public string Status { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF230");
        AssertDiagnosticWithSeverity(source, "FKF230", DiagnosticSeverity.Info);
    }
}
