using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class CollectionGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Collection_SameType_DirectAssignment()
    {
        // Same List<string> on both sides = exact type match = direct assignment
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest   { public List<string> Tags { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Tags = source.Tags", generated);
        Assert.DoesNotContain(".ToList()", generated);
        Assert.DoesNotContain(".ToArray()", generated);
    }

    [Fact]
    public void Collection_ListToArray_ToArray()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest   { public int[] Values { get; set; } = System.Array.Empty<int>(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Values = source.Values != null ? source.Values.ToArray() : null", generated);
    }

    [Fact]
    public void Collection_DifferentElementType_WithNestedForge()
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

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Items = source.Items != null ? source.Items.Select(x => ToItemDto(x)).ToList() : null", generated);
    }

    [Fact]
    public void Collection_MixedWithRegularProps()
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

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Values = source.Values != null ? source.Values.ToArray() : null", generated);
    }
}
