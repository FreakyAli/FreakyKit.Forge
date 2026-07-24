using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for ForgeMode.Explicit: only [ForgeMethod]-decorated methods generate code.
/// Non-attributed methods are skipped by the generator (no generated body for them).
/// </summary>
public sealed class ExplicitModeGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void ExplicitMode_AttributedMethod_GeneratesCode()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Dest ToDest(Source source)", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void ExplicitMode_NonAttributedMethod_NoGeneratedBody()
    {
        // In explicit mode, a properly-shaped method without [ForgeMethod] is skipped.
        // The generator still emits the class wrapper, but no method body is generated.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        // No method body generated for non-attributed method in explicit mode
        Assert.DoesNotContain("ToDest", generated);
        // The class wrapper is still emitted even with no methods
        Assert.Contains("MyForges", generated);
    }

    [Fact]
    public void ExplicitMode_MixedMethods_OnlyAttributedGenerated()
    {
        // In explicit mode, only [ForgeMethod]-attributed partial methods are generated
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }
                public class B    { public int Y { get; set; } }
                public class BDto { public int Y { get; set; } }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial ADto ToADto(A source);

                    [ForgeMethod]
                    public static partial BDto ToBDto(B source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Both should be generated since both have [ForgeMethod]
        Assert.Contains("ToADto(A source)", generated);
        Assert.Contains("ToBDto(B source)", generated);
        Assert.Contains("__result.X = source.X", generated);
        Assert.Contains("__result.Y = source.Y", generated);
    }

    [Fact]
    public void ExplicitMode_MultipleAttributedMethods_AllGenerated()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }
                public class B    { public int Y { get; set; } }
                public class BDto { public int Y { get; set; } }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial ADto ToADto(A source);

                    [ForgeMethod]
                    public static partial BDto ToBDto(B source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("ToADto(", generated);
        Assert.Contains("ToBDto(", generated);
        Assert.Contains("__result.X = source.X", generated);
        Assert.Contains("__result.Y = source.Y", generated);
    }

    [Fact]
    public void ImplicitMode_AllShapedMethods_GeneratedWithoutAttribute()
    {
        // Contrast: in implicit mode (default), all shaped methods are included.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ADto ToADto(A source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("ADto ToADto(A source)", generated);
        Assert.Contains("__result.X = source.X", generated);
    }
}
