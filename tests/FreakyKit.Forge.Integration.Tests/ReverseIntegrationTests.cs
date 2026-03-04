using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class ReverseIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_Reverse_GeneratesBothMethods()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(GenerateReverse = true, ReverseName = "FromDto")]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("PersonDto ToDto(Person source)", generated);
        Assert.Contains("Person FromDto(PersonDto source)", generated);
    }
}
