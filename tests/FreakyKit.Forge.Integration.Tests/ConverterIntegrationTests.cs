using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class ConverterIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_Converter_GeneratesSuccessfully()
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

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("__result.Created = DateToStr(source.Created)", generated);
    }
}
