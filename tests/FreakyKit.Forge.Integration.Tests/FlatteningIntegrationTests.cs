using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class FlatteningIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_Flattening_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; public string State { get; set; } = ""; }
                public class Source  { public string Name { get; set; } = ""; public Address Address { get; set; } = new(); }
                public class Dest    { public string Name { get; set; } = ""; public string AddressCity { get; set; } = ""; public string AddressState { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.AddressCity = source.Address.City", generated);
        Assert.Contains("__result.AddressState = source.Address.State", generated);
    }
}
