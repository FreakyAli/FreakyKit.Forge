using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for private method handling in the source generator.
/// Verifies that private methods are included when ShouldIncludePrivate = true on [Forge].
/// </summary>
public sealed class PrivateMethodGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void PrivateMethod_IncludedWhenEnabled()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge(ShouldIncludePrivate = true)]
                public static partial class MyForges
                {
                    private static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // The generator emits the method body (private methods are included)
        Assert.Contains("Dest ToDest(Source source)", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void PrivateMethod_GeneratesCorrectBody()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge(ShouldIncludePrivate = true)]
                public static partial class MyForges
                {
                    private static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Dest ToDest(Source source)", generated);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Age = source.Age", generated);
        Assert.Contains("return __result", generated);
    }

    [Fact]
    public void MixedVisibility_BothGenerated_WhenIncludePrivateEnabled()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }
                public class B    { public int Y { get; set; } }
                public class BDto { public int Y { get; set; } }

                [Forge(ShouldIncludePrivate = true)]
                public static partial class MyForges
                {
                    public static partial ADto ToADto(A source);
                    private static partial BDto ToBDto(B source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("public static partial ADto ToADto(A source)", generated);
        // Private method is included; generator emits its body
        Assert.Contains("BDto ToBDto(B source)", generated);
    }

    [Fact]
    public void PrivateMethod_GeneratesImplementation()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge(ShouldIncludePrivate = true)]
                public static partial class MyForges
                {
                    private static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Private methods should generate the implementation
        Assert.Contains("Dest ToDest(Source source)", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }
}
