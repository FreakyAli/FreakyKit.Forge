using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class FlatteningGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Flattening_AddressCity_MapsToAddressDotCity()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCity { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.AddressCity = source.Address.City", generated);
    }

    [Fact]
    public void Flattening_Disabled_SkipsFlattenedMembers()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCity { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.DoesNotContain("AddressCity", generated);
    }

    [Fact]
    public void Flattening_MixedWithDirectMatch()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; public string Zip { get; set; } = ""; }
                public class Source  { public string Name { get; set; } = ""; public Address Address { get; set; } = new(); }
                public class Dest    { public string Name { get; set; } = ""; public string AddressCity { get; set; } = ""; public string AddressZip { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.AddressCity = source.Address.City", generated);
        Assert.Contains("__result.AddressZip = source.Address.Zip", generated);
    }
}
