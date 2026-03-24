using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ForgeMapGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void ForgeMap_SourceSide_GeneratesCorrectAssignment()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name")] public string FirstName { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

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
        Assert.Contains("__result.Name = source.FirstName", generated);
        Assert.DoesNotContain("__result.FirstName", generated);
    }

    [Fact]
    public void ForgeMap_DestSide_GeneratesCorrectAssignment()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string FirstName { get; set; } = ""; }
                public class Dest   { [ForgeMap("FirstName")] public string Name { get; set; } = ""; }

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
        Assert.Contains("__result.Name = source.FirstName", generated);
        Assert.DoesNotContain("source.Name", generated);
    }

    [Fact]
    public void ForgeMap_BothSides_MeetInMiddle()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("CommonKey")] public string SrcField { get; set; } = ""; }
                public class Dest   { [ForgeMap("CommonKey")] public string DstField { get; set; } = ""; }

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
        Assert.Contains("__result.DstField = source.SrcField", generated);
        Assert.DoesNotContain("__result.SrcField", generated);
        Assert.DoesNotContain("source.DstField", generated);
    }

    [Fact]
    public void ForgeMap_WithRegularProps_MixedMapping()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public int Age { get; set; }
                    [ForgeMap("FullName")] public string FirstName { get; set; } = "";
                }
                public class Dest
                {
                    public int Age { get; set; }
                    public string FullName { get; set; } = "";
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
        Assert.Contains("__result.Age = source.Age", generated);
        Assert.Contains("__result.FullName = source.FirstName", generated);
    }
}
