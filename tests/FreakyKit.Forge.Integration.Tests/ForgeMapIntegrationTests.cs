using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class ForgeMapIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_ForgeMap_SourceSide_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name")] public string FirstName { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("__result.Name = source.FirstName", generated);
    }

    [Fact]
    public void E2E_ForgeMap_BothSides_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Key")] public string SrcProp { get; set; } = ""; }
                public class Dest   { [ForgeMap("Key")] public string DstProp { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("__result.DstProp = source.SrcProp", generated);
    }

    [Fact]
    public void E2E_ForgeMap_MixedWithRegular_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public int Id { get; set; }
                    [ForgeMap("DisplayName")] public string Name { get; set; } = "";
                }
                public class Dest
                {
                    public int Id { get; set; }
                    public string DisplayName { get; set; } = "";
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("__result.Id = source.Id", generated);
        Assert.Contains("__result.DisplayName = source.Name", generated);
    }
}
