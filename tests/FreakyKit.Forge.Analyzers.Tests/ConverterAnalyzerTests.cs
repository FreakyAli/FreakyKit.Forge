using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class ConverterAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public void Converter_SuppressesFKF200()
    {
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public DateTime Birthday { get; set; } }
                public class Dest   { public string Birthday { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDateTime(DateTime value) => value.ToString("yyyy-MM-dd");
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void NoConverter_EmitsFKF200()
    {
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public DateTime Birthday { get; set; } }
                public class Dest   { public string Birthday { get; set; } = ""; }

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
