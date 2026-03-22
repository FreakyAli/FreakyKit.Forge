using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class InvalidConverterTests : AnalyzerTestBase
{
    [Fact]
    public void ValidConverter_NoFKF221()
    {
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public DateTime Birthday { get; set; } }
                public class Dest   { public string Birthday { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate(DateTime value) => value.ToString("yyyy-MM-dd");
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF221");
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void Converter_MultipleParams_EmitsFKF221()
    {
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public DateTime Birthday { get; set; } }
                public class Dest   { public string Birthday { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate(DateTime value, string format) => value.ToString(format);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF221");
        AssertDiagnosticSeverity(source, "FKF221", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Converter_VoidReturn_EmitsFKF221()
    {
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public DateTime Birthday { get; set; } }
                public class Dest   { public string Birthday { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static void ConvertDate(DateTime value) { }
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF221");
        AssertDiagnosticSeverity(source, "FKF221", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Converter_NoParams_EmitsFKF221()
    {
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest   { public string Value { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertValue() => "42";
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF221");
        AssertDiagnosticSeverity(source, "FKF221", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Converter_Generic_EmitsFKF221()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest   { public string Value { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string Convert<T>(T value) => value?.ToString() ?? "";
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF221");
        AssertDiagnosticSeverity(source, "FKF221", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void Converter_InvalidSignature_StillEmitsFKF200_ForMissingConversion()
    {
        // Invalid converter should be ignored → type mismatch still produces FKF200
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public DateTime Birthday { get; set; } }
                public class Dest   { public string Birthday { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate(DateTime value, string format) => value.ToString(format);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF221");
        AssertContainsDiagnostic(source, "FKF200");
    }
}
