using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class CollectionGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Collection_SameType_DeepCopiesByDefault()
    {
        // Same List<string> on both sides — Forge deep-copies via copy ctor by default so
        // mutations to the DTO don't leak back to the source. The opt-out is ShareReference = ForgePolicy.True.
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
        Assert.Contains("__result.Tags = source.Tags != null ? new List<string>(source.Tags) : null", generated);
    }

    [Fact]
    public void Collection_SameType_ShareReferenceTrue_OptsOutOfCopy()
    {
        // With [ForgeMethod(ShareReference = ForgePolicy.True)], same-type mutable collection members are
        // reference-shared instead of copied. Faster, less alloc, but mutations to the DTO leak
        // back to the source. FKF311 (Info) is emitted for visibility.
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
                    [ForgeMethod(ShareReference = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Tags = source.Tags;", generated);
        Assert.DoesNotContain("new List<string>(source.Tags)", generated);
        // FKF311 emitted
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF311");
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

    [Fact]
    public void FKF310_CollectionMapping_EmitsDiagnostic()
    {
        // FKF310: Info diagnostic when collection mapping is applied
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
        // FKF310 should be emitted for collection mapping
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF310");
        // Verify collection mapping is generated correctly
        Assert.Contains("source.Values != null ? source.Values.ToArray() : null", generated);
    }
}
