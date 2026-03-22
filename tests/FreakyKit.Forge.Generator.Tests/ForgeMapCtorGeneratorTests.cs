using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ForgeMapCtorGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void ForgeMap_OnCtorParam_RemapsSourceMember()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string FullName { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; }
                    public Dest([ForgeMap("FullName")] string name) { Name = name; }
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
        Assert.Contains("new Dest(source.FullName)", generated);
        Assert.DoesNotContain("FKF501", generated);
    }

    [Fact]
    public void ForgeMap_OnCtorParam_WithoutAttribute_FKF501()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string FullName { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; }
                    public Dest(string name) { Name = name; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF501");
    }

    [Fact]
    public void ForgeMap_OnCtorParam_NullableSourceMember()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Score { get; set; } }
                public class Dest
                {
                    public int Value { get; }
                    public Dest([ForgeMap("Score")] int value) { Value = value; }
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
        Assert.Contains("source.Score.Value", generated);
        Assert.DoesNotContain("FKF501", generated);
    }

    [Fact]
    public void ForgeMap_OnCtorParam_MixedWithDirectParams()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string FullName { get; set; } = "";
                    public int Age { get; set; }
                }
                public class Dest
                {
                    public string Name { get; }
                    public int Age { get; }
                    public Dest([ForgeMap("FullName")] string name, int age) { Name = name; Age = age; }
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
        Assert.Contains("source.FullName", generated);
        Assert.Contains("source.Age", generated);
    }
}
