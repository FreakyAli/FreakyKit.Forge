using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class NullFallbackGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void NullFallback_DefaultConstruct_GeneratesDefaultConstructor()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

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
        Assert.Contains("source.Home != null ? ToAddressDto(source.Home) : new AddressDto()", generated);
    }

    [Fact]
    public void NullFallback_Null_DefaultBehavior()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.Null)]
                    public AddressDto Home { get; set; } = new();
                }

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
    public void NullFallback_DefaultConstruct_WithValueType_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public struct Address { public string City { get; set; } }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

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
        Assert.Single(result.Diagnostics, d => d.Id == "FKF314");
    }

    [Fact]
    public void NullFallback_WithIgnoreIfNull_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Home", IgnoreIfNull = true, NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

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
        Assert.Single(result.Diagnostics, d => d.Id == "FKF315");
    }

    [Fact]
    public void NullFallback_WithCollections_UsesEmptyCollectionSyntax()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public List<Address> Homes { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Homes", NullFallback = NullFallback.DefaultConstruct)]
                    public List<AddressDto> Homes { get; set; } = new();
                }

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
        Assert.Contains(": Enumerable.Empty<object>()", generated);
    }

    [Fact]
    public void NullFallback_InUpdateMethod_Works()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial void Update(Source source, Dest existing);
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Home != null ? ToAddressDto(source.Home) : new AddressDto()", generated);
    }

    [Fact]
    public void NullFallback_MultipleMembers_EachIndependent()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source
                {
                    public Address Home { get; set; } = new();
                    public Address Work { get; set; } = new();
                }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();

                    [ForgeMap("Work", NullFallback = NullFallback.Null)]
                    public AddressDto Work { get; set; } = new();
                }

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
        Assert.Contains("source.Home != null ? ToAddressDto(source.Home) : new AddressDto()", generated);
        Assert.Contains("source.Work != null ? ToAddressDto(source.Work) : null", generated);
    }

    [Fact]
    public void NullFallback_DefaultConstruct_InExpressionProperty()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true, GenerateExpression = true)]
                    public static partial Dest ToDest(Source source);
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Both imperative and expression should include the fallback
        Assert.Contains("source.Home != null ? ToAddressDto(source.Home) : new AddressDto()", generated);
    }

    [Fact]
    public void NullFallback_WithHashSet_GeneratesCorrectly()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Item { public int Id { get; set; } }
                public class ItemDto { public int Id { get; set; } }
                public class Source { public HashSet<Item> Items { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Items", NullFallback = NullFallback.DefaultConstruct)]
                    public HashSet<ItemDto> Items { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                    public static partial ItemDto ToItemDto(Item item);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Items != null", generated);
        Assert.Contains("Enumerable.Empty<object>()", generated);
    }

    [Fact]
    public void NullFallback_WithImmutableList_GeneratesCorrectly()
    {
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Immutable;
            namespace TestNs
            {
                public class Item { public int Id { get; set; } }
                public class ItemDto { public int Id { get; set; } }
                public class Source { public ImmutableList<Item> Items { get; set; } = ImmutableList<Item>.Empty; }
                public class Dest
                {
                    [ForgeMap("Items", NullFallback = NullFallback.DefaultConstruct)]
                    public ImmutableList<ItemDto> Items { get; set; } = ImmutableList<ItemDto>.Empty;
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                    public static partial ItemDto ToItemDto(Item item);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Items != null", generated);
        Assert.Contains("Enumerable.Empty<object>()", generated);
    }

    [Fact]
    public void NullFallback_AllMembersIgnored_SkipsExpressionProperty()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Id { get; set; } }
                public class Dest
                {
                    [ForgeMap("Id", IgnoreIfNull = true)]
                    public int Id { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // When all members are ignored, expression property should not be generated
        Assert.DoesNotContain("public static System.Linq.Expressions.Expression<System.Func<Source, Dest>>", generated);
    }
}
