using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for ForgeMode.Explicit: only [Forge]-decorated methods generate code.
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

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
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
        // In explicit mode, a properly-shaped method without [Forge] is skipped.
        // The generator still emits the class wrapper, but no method body is generated.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass(Mode = ForgeMode.Explicit)]
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
    }

    [Fact]
    public void ExplicitMode_MixedMethods_OnlyAttributedGenerated()
    {
        // Two methods, only one has [Forge]. Only that one should appear in generated code.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }
                public class B    { public int Y { get; set; } }
                public class BDto { public int Y { get; set; } }

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
                    public static partial ADto ToADto(A source);

                    public static partial BDto ToBDto(B source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("ADto ToADto(A source)", generated);
        Assert.Contains("__result.X = source.X", generated);
        // ToBDto should NOT be generated since it lacks [Forge] in explicit mode
        Assert.DoesNotContain("ToBDto", generated);
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

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
                    public static partial ADto ToADto(A source);

                    [Forge]
                    public static partial BDto ToBDto(B source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("ToADto(", generated);
        Assert.Contains("ToBDto(", generated);
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

                [ForgeClass]
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
    }
}
