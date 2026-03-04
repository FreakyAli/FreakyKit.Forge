using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ConverterGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Converter_DateTimeToString_UsesConverter()
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

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Birthday = ConvertDateTime(source.Birthday)", generated);
    }

    [Fact]
    public void Converter_IntToString_UsesConverter()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Code { get; set; } }
                public class Dest   { public string Code { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string IntToString(int value) => value.ToString();
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Code = IntToString(source.Code)", generated);
    }

    [Fact]
    public void Converter_MixedWithDirectMatch()
    {
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public DateTime Created { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public string Created { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string DateToStr(DateTime value) => value.ToString();
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Created = DateToStr(source.Created)", generated);
    }

    [Fact]
    public void NoConverter_StillEmitsFKF200()
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

        var result = RunGenerator(source);
        AssertHasError(result, "FKF200");
    }
}
