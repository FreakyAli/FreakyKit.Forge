using System;
using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public class EdgeCaseGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void LargeMemberCount_GeneratesValidCode()
    {
        // 50 properties — tests O(n²) member matching and code generation at scale
        var props = string.Join("\n            ",
            Enumerable.Range(1, 50).Select(i => $"public int Prop{i} {{ get; set; }}"));

        var source = "using FreakyKit.Forge;\n" +
            "namespace TestNs\n" +
            "{\n" +
            "    public class Source\n" +
            "    {\n" +
            $"        {props}\n" +
            "    }\n" +
            "    public class Dest\n" +
            "    {\n" +
            $"        {props}\n" +
            "    }\n" +
            "\n" +
            "    [Forge]\n" +
            "    public static partial class MyForges\n" +
            "    {\n" +
            "        public static partial Dest ToDto(Source source);\n" +
            "    }\n" +
            "}\n";

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Spot-check: verify all 50 properties are assigned
        Assert.Contains("__result.Prop1 = source.Prop1;", generated);
        Assert.Contains("__result.Prop25 = source.Prop25;", generated);
        Assert.Contains("__result.Prop50 = source.Prop50;", generated);
    }

    [Fact]
    public void DeeplyNestedGenerics_GeneratesValidCode()
    {
        // Generic<Generic<Generic<T>>> — tests type resolution at depth
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Source
                {
                    public List<List<List<int>>> Deep { get; set; } = new();
                }

                public class Dest
                {
                    public List<List<List<int>>> Deep { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Verify code handles the nested generic correctly
        Assert.Contains("source.Deep", generated);
        Assert.Contains("List<List<int>>", generated);
    }

    [Fact]
    public void NullFallback_CollectionFallback_UsesVersionCompatibleSyntax()
    {
        // Tests that DefaultConstruct fallback uses version-compatible empty collection syntax
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Item { public int Id { get; set; } }
                public class ItemDto { public int Id { get; set; } }
                public class Source { public List<Item> Items { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Items", NullFallback = FreakyKit.Forge.NullFallback.DefaultConstruct)]
                    public List<ItemDto> Items { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDto(Source source);
                    public static partial ItemDto ToItemDto(Item item);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Verify the fallback uses version-compatible syntax (Enumerable.Empty<object>()) not C# 12 "[]"
        Assert.Contains("Enumerable.Empty<object>()", generated);
        Assert.DoesNotContain(": []", generated);
    }
}
