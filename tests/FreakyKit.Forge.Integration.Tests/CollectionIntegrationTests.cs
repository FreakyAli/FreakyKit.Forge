using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class CollectionIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_Collection_ListToArray_GeneratesSuccessfully()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public List<int> Values { get; set; } = new(); }
                public class Dest   { public string Name { get; set; } = ""; public int[] Values { get; set; } = System.Array.Empty<int>(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("__result.Values = source.Values.ToArray()", generated);
    }

    [Fact]
    public void E2E_Collection_WithNestedForge_GeneratesSuccessfully()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }
                public class Source  { public List<Item> Items { get; set; } = new(); }
                public class Dest    { public List<ItemDto> Items { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                    public static partial ItemDto ToItemDto(Item source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("source.Items.Select(x => ToItemDto(x)).ToList()", generated);
    }
}
