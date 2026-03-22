using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ExtendedCollectionGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Collection_ListToImmutableArray()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest   { public ImmutableArray<int> Values { get; set; } }

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
        Assert.Contains(".ToImmutableArray()", generated);
    }

    [Fact]
    public void Collection_ListToImmutableList()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest   { public ImmutableList<string> Tags { get; set; } = ImmutableList<string>.Empty; }

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
        Assert.Contains(".ToImmutableList()", generated);
    }

    [Fact]
    public void Collection_ListToImmutableHashSet()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest   { public ImmutableHashSet<string> Tags { get; set; } = ImmutableHashSet<string>.Empty; }

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
        Assert.Contains(".ToImmutableHashSet()", generated);
    }

    [Fact]
    public void Collection_ListToReadOnlyCollection()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest   { public ReadOnlyCollection<int> Values { get; set; } = new List<int>().AsReadOnly(); }

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
        Assert.Contains(".ToList().AsReadOnly()", generated);
    }

    [Fact]
    public void Collection_ArrayToImmutableArray_WithNestedForge()
    {
        const string source = """
            using System.Collections.Immutable;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }
                public class Source  { public Item[] Items { get; set; } = System.Array.Empty<Item>(); }
                public class Dest    { public ImmutableArray<ItemDto> Items { get; set; } }

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
        Assert.Contains("Select(x => ToItemDto(x)).ToImmutableArray()", generated);
    }

    [Fact]
    public void Collection_ImmutableArray_CompilesWithoutErrors()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest   { public ImmutableArray<int> Values { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains(".ToImmutableArray()", generated);
    }
}
