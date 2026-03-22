using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class NullSafeNestedGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void NestedForge_NullSafe_WhenSourceIsReferenceType()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest   { public AddressDto Home { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Home != null ? ToAddressDto(source.Home) : null", generated);
    }

    [Fact]
    public void NestedForge_NullSafe_CompilesWithoutErrors()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest   { public AddressDto Home { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Home != null ? ToAddressDto(source.Home) : null", generated);
    }

    [Fact]
    public void Flattening_NullSafe_UsesNullConditionalAccess()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string Street { get; set; } = ""; public string City { get; set; } = ""; }
                public class Source  { public Address Home { get; set; } = new(); }
                public class Dest    { public string HomeStreet { get; set; } = ""; public string HomeCity { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Home?.Street", generated);
        Assert.Contains("source.Home?.City", generated);
        Assert.DoesNotContain("source.Home.Street", generated);
        Assert.DoesNotContain("source.Home.City", generated);
    }

    [Fact]
    public void Collection_NullSafe_GuardsReferenceTypeCollection()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest   { public string[] Tags { get; set; } = System.Array.Empty<string>(); }

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
        Assert.Contains("source.Tags != null ? source.Tags.ToArray() : null", generated);
    }

    [Fact]
    public void Collection_NullSafe_WithNestedForge_GuardsCollection()
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
        Assert.Contains("source.Items != null ? source.Items.Select(x => ToItemDto(x)).ToList() : null", generated);
    }
}
