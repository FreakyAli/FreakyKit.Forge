using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class HooksIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_BothHooks_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                    static partial void OnBeforeToDest(Source source);
                    static partial void OnAfterToDest(Source source, Dest result);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("OnBeforeToDest(source);", generated);
        Assert.Contains("OnAfterToDest(source, __result);", generated);
    }
}
