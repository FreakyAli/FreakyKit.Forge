using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for update mapping (void return, 2 parameters) diagnostics.
/// </summary>
public sealed class UpdateMappingTests : AnalyzerTestBase
{
    [Fact]
    public void UpdateShape_Valid_NoErrors()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        // Should have FKF040 (info) but no errors
        AssertNotContainsDiagnostic(source, "FKF041");
        AssertContainsDiagnostic(source, "FKF040");
        AssertDiagnosticSeverity(source, "FKF040", DiagnosticSeverity.Info);
    }

    [Fact]
    public void UpdateShape_FKF101_StillEmits()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Extra { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        // FKF101: Extra is unused
        AssertContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void UpdateShape_TypeMismatch_EmitsFKF200()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int    Value { get; set; } }
                public class Dest   { public string Value { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF200");
    }
}
