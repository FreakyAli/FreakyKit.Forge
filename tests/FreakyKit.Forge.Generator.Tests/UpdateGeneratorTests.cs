using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for update mapping code generation (void return, 2 parameters).
/// </summary>
public sealed class UpdateGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Update_SimpleMapping_GeneratesAssignments()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);

        // Should use the dest parameter name directly, not __result
        Assert.Contains("existing.Name = source.Name", generated);
        Assert.Contains("existing.Age = source.Age", generated);
        Assert.DoesNotContain("var __result = new", generated);
        Assert.DoesNotContain("return __result", generated);
        Assert.DoesNotContain("return ", generated);
        Assert.Contains("void Update(Source source, Dest existing)", generated);
    }

    [Fact]
    public void Update_NoConstruction_Generated()
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
                    public static partial void ApplyUpdate(Source src, Dest target);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);

        // No construction at all — no new Dest()
        Assert.DoesNotContain("new Dest", generated);
        Assert.Contains("target.Name = src.Name", generated);
    }

    [Fact]
    public void Update_WithRegularCreateMethod_BothGenerated()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDto(Source source);
                    public static partial void ApplyTo(Source source, Dest existing);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);

        // Create method
        Assert.Contains("Dest ToDto(Source source)", generated);
        Assert.Contains("var __result = new Dest()", generated);
        Assert.Contains("return __result", generated);

        // Update method
        Assert.Contains("void ApplyTo(Source source, Dest existing)", generated);
        Assert.Contains("existing.Name = source.Name", generated);
        Assert.Contains("existing.Age = source.Age", generated);
    }
}
