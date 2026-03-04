using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for enum-to-enum mapping behavior in the analyzer.
/// Verifies that FKF200 is not emitted for enum-to-enum pairs,
/// and is still emitted for enum-to-non-enum and non-enum-to-enum.
/// </summary>
public sealed class EnumMappingTests : AnalyzerTestBase
{
    [Fact]
    public void EnumToEnum_NoFKF200()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public SourceStatus Status { get; set; } }
                public class Dest   { public DestStatus   Status { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void EnumToNonEnum_StillFKF200()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public class Source { public SourceStatus Status { get; set; } }
                public class Dest   { public string       Status { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void NonEnumToEnum_StillFKF200()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum DestStatus { Active, Inactive }
                public class Source { public int        Status { get; set; } }
                public class Dest   { public DestStatus Status { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF200");
    }
}
