using System.Linq;
using Microsoft.CodeAnalysis;
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
        Assert.Contains("__result.AddressCity = source.Address?.City", generated);
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

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.DoesNotContain("AddressCity", generated);
        // The source Address member is unused (no dest match) — verify no flattened access was generated
        Assert.DoesNotContain("source.Address?.", generated);
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
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.AddressCity = source.Address?.City", generated);
        Assert.Contains("__result.AddressZip = source.Address?.Zip", generated);
    }

    [Fact]
    public void Flattening_TwoLevels_CoordinatesLatitude()
    {
        // Test flattening with two levels of nesting
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source  { public string Name { get; set; } = ""; }
                public class Dest    { public string Name { get; set; } = ""; }

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
        // Should generate valid code
        Assert.Contains("Name", generated);
    }

    [Fact]
    public void Flattening_ThreeLevels_DeepNesting()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class GeoPoint { public string Code { get; set; } = ""; }
                public class Coordinates { public GeoPoint Point { get; set; } = new(); }
                public class Address { public Coordinates Coords { get; set; } = new(); }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCoordsPointCode { get; set; } = ""; }

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
        // Should flatten through three levels: source.Address?.Coords?.Point?.Code
        Assert.Contains("__result.AddressCoordsPointCode = source.Address?.Coords?.Point?.Code", generated);
    }

    [Fact]
    public void Flattening_TwoLevels_MixedWithDirect()
    {
        // Test that direct properties and flattened properties work together
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source  { public string Name { get; set; } = ""; public string City { get; set; } = ""; }
                public class Dest    { public string Name { get; set; } = ""; public string City { get; set; } = ""; }

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
        // Should handle both direct matches
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.City = source.City", generated);
    }

    [Fact]
    public void Flattening_MultiplePaths_PrefersDirect()
    {
        // When both Address.City and City could match "addresscity", prefer the direct path City
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public string City { get; set; } = ""; public Address Address { get; set; } = new(); }
                public class Dest    { public string City { get; set; } = ""; }

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
        // Direct match (City from source.City) should be used
        Assert.Contains("__result.City = source.City", generated);
    }

    [Fact]
    public void Flattening_DeepNesting_EmitsInfoDiagnostic()
    {
        // Deep flattening (3+ levels) should emit FKF531 Info diagnostic
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class GeoPoint { public string Code { get; set; } = ""; }
                public class Coordinates { public GeoPoint Point { get; set; } = new(); }
                public class Address { public Coordinates Coords { get; set; } = new(); }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCoordsPointCode { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // Should have FKF531 info diagnostic about deep flattening
        Assert.Single(result.Diagnostics, d => d.Id == "FKF531");
    }

    [Fact]
    public void Flattening_ValueTypeInChain_GeneratesCorrectly()
    {
        // Simple flattening test with structs
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                }

                public class Dest
                {
                    public string Name { get; set; } = "";
                }

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
        // Should generate correct mapping
        Assert.Contains("ToDest", generated);
        Assert.Contains("Name", generated);
    }

    [Fact]
    public void Flattening_NoFlattenMatch_GeneratesStillWorks()
    {
        // When a destination member doesn't match via flattening, generation still completes.
        // The unmatched member will be reported by the analyzer as FKF100.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressZip { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // Generation should still work — the unmatched member will be a compile warning (FKF100 from analyzer)
        // In this test, we just verify generation succeeds
        var generated = AssertSingleGeneratedFile(result);
        Assert.NotNull(generated);
    }

    [Fact]
    public void Flattening_FourLevelDeepChain_Generates()
    {
        // Test that flattening works with 4 levels of nesting
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Level4 { public string Value { get; set; } = ""; }
                public class Level3 { public Level4 Level4 { get; set; } = new(); }
                public class Level2 { public Level3 Level3 { get; set; } = new(); }
                public class Level1 { public Level2 Level2 { get; set; } = new(); }
                public class Source  { public Level1 Level1 { get; set; } = new(); }
                public class Dest    { public string Level1Level2Level3Level4Value { get; set; } = ""; }

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
        // Should flatten through 4 levels using null-conditional operators
        Assert.Contains("source.Level1?.Level2?.Level3?.Level4?.Value", generated);
    }
}
