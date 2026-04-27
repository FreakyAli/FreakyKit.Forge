using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class DictionaryProjectGeneratorTests : GeneratorTestBase
{
    // ─── Method-level dictionary projection ──────────────────────────────────

    [Fact]
    public void DictProject_SameValueType_UsesCopyCtor()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dictionary<string, Item> Copy(Dictionary<string, Item> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("if (source == null) return null;", generated);
        Assert.Contains("return new Dictionary<string, Item>(source);", generated);
        Assert.DoesNotContain("foreach", generated);
    }

    [Fact]
    public void DictProject_DifferentValueTypes_UsesForEachWithNestedForge()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    public static partial Dictionary<string, OrderDto> MapOrderDict(Dictionary<string, Order> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("if (source == null) return null;", generated);
        Assert.Contains("foreach (var __kvp in source)", generated);
        Assert.Contains("__result[__kvp.Key] = MapOrder(__kvp.Value);", generated);
        Assert.Contains(".Count)", generated);
    }

    [Fact]
    public void DictProject_NoMatchingNestedForge_EmitsFKF200()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    // No forge method for Order → OrderDto
                    public static partial Dictionary<string, OrderDto> MapOrderDict(Dictionary<string, Order> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF200");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void DictProject_IDictionarySource_Detected()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto MapItem(Item source);
                    public static partial Dictionary<string, ItemDto> MapItems(IDictionary<string, Item> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("foreach (var __kvp in source)", generated);
        Assert.Contains("MapItem(__kvp.Value)", generated);
    }

    [Fact]
    public void DictProject_IReadOnlyDictionarySource_Detected()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto MapItem(Item source);
                    public static partial Dictionary<string, ItemDto> MapItems(IReadOnlyDictionary<string, Item> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("foreach (var __kvp in source)", generated);
        Assert.Contains("MapItem(__kvp.Value)", generated);
    }

    [Fact]
    public void DictProject_MismatchedKeyTypes_EmitsFKF200()
    {
        // Source has int keys, dest has string keys — key types must match.
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto MapItem(Item source);
                    public static partial Dictionary<string, ItemDto> MapItems(Dictionary<int, Item> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF200");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void DictProject_CompilesWithoutErrors()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    public static partial Dictionary<string, OrderDto> MapOrderDict(Dictionary<string, Order> source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        AssertNoErrors(result);
    }

    // ─── Member-level dictionary mapping ─────────────────────────────────────

    [Fact]
    public void DictMember_SameValueType_DirectAssignment()
    {
        // Same K,V types on both sides → taken by exact-type-match path, no special handling needed
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public Dictionary<string, int> Scores { get; set; } = new(); }
                public class Dest   { public Dictionary<string, int> Scores { get; set; } = new(); }

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
        Assert.Contains("= source.Scores", generated);
    }

    [Fact]
    public void DictMember_DifferentValueTypes_UsesToDictionary()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }
                public class Source  { public Dictionary<string, Item> Items { get; set; } = new(); }
                public class Dest    { public Dictionary<string, ItemDto> Items { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto MapItem(Item source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("ToDictionary(__kvp => __kvp.Key, __kvp => MapItem(__kvp.Value))", generated);
    }

    [Fact]
    public void DictMember_NullSafe_DifferentValueTypes()
    {
        // Different V types resolved via AllowNestedForging → null guard on the ToDictionary call
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }
                public class Source  { public Dictionary<string, Item> Items { get; set; } = new(); }
                public class Dest    { public Dictionary<string, ItemDto> Items { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto MapItem(Item source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Items != null ?", generated);
        Assert.Contains(": null", generated);
    }

    [Fact]
    public void DictMember_CompilesWithoutErrors()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }
                public class Source  { public Dictionary<string, Item> Items { get; set; } = new(); }
                public class Dest    { public Dictionary<string, ItemDto> Items { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto MapItem(Item source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        AssertNoErrors(result);
    }
}
