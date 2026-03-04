using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ReverseGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Reverse_GeneratesBothMethods()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(GenerateReverse = true, ReverseName = "FromDto")]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("PersonDto ToDto(Person source)", generated);
        Assert.Contains("Person FromDto(PersonDto source)", generated);
    }

    [Fact]
    public void Reverse_GeneratesCorrectAssignments()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(GenerateReverse = true, ReverseName = "ToSource")]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Forward
        Assert.Contains("__result.Name = source.Name", generated);
        // Reverse — should also have source.Name
        Assert.Contains("Source ToSource(Dest source)", generated);
    }

    [Fact]
    public void NoReverse_OnlyForwardMethod()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Dest ToDest(Source source)", generated);
        // Should not contain a reverse method
        Assert.DoesNotContain("Source ToDest", generated);
        Assert.DoesNotContain("FromDest", generated);
    }
}
