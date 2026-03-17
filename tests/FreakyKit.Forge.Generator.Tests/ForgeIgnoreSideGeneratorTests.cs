using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ForgeIgnoreSideGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void ForgeIgnore_Side_Both_ExcludesMemberFromBothSides()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public string Secret { get; set; } = "";
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                }

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
        Assert.Contains("source.Name", generated);
        Assert.DoesNotContain("source.Secret", generated);
    }

    [Fact]
    public void ForgeIgnore_Side_Source_ExcludesOnlyFromSourceSide()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore(Side = ForgeIgnoreSide.Source)]
                    public string InternalId { get; set; } = "";
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                }

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
        Assert.Contains("source.Name", generated);
        Assert.DoesNotContain("source.InternalId", generated);
    }

    [Fact]
    public void ForgeIgnore_Side_Destination_ExcludesOnlyFromDestSide()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore(Side = ForgeIgnoreSide.Destination)]
                    public int ComputedScore { get; set; }
                }

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
        Assert.Contains("source.Name", generated);
        Assert.DoesNotContain("ComputedScore", generated);
    }

    [Fact]
    public void ForgeIgnore_Side_Source_DestMemberStillMapped_ViaForgeMap()
    {
        // Source.InternalId is ignored on source side only.
        // Dest has InternalId mapped from AltId via [ForgeMap].
        // The dest InternalId should still be mapped.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore(Side = ForgeIgnoreSide.Source)]
                    public string InternalId { get; set; } = "";
                    public string AltId { get; set; } = "";
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    [ForgeMap("AltId")]
                    public string InternalId { get; set; } = "";
                }

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
        Assert.Contains("source.AltId", generated);
        Assert.Contains("InternalId", generated);
    }
}
