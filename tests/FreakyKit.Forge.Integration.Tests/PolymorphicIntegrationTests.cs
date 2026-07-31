using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class PolymorphicIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_PolymorphicDispatch_GeneratesValidCode()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { public string Breed { get; set; } = ""; }
                public class Cat : Animal { public bool Indoor { get; set; } }

                public class AnimalDto { public string Name { get; set; } = ""; }
                public class DogDto : AnimalDto { public string Breed { get; set; } = ""; }
                public class CatDto : AnimalDto { public bool Indoor { get; set; } }

                [Forge]
                public static partial class AnimalForges
                {
                    public static partial DogDto MapDog(Dog source);
                    public static partial CatDto MapCat(Cat source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.False(result.HasCompilationErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("source switch", generated);
        Assert.Contains("TestNs.Dog __p0 => MapDog(__p0)", generated);
        Assert.Contains("TestNs.Cat __p1 => MapCat(__p1)", generated);
        Assert.Contains("throw new InvalidOperationException", generated);
    }

    [Fact]
    public void E2E_PolymorphicDispatch_WithBaseFallback_GeneratesValidCode()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { public string Breed { get; set; } = ""; }

                public class AnimalDto { public string Name { get; set; } = ""; }
                public class DogDto : AnimalDto { public string Breed { get; set; } = ""; }

                [Forge]
                public static partial class AnimalForges
                {
                    public static partial AnimalDto MapBase(Animal source);
                    public static partial DogDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    [ForgePolymorphic(typeof(Animal), nameof(MapBase))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.False(result.HasCompilationErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("TestNs.Dog __p0 => MapDog(__p0)", generated);
        Assert.Contains("TestNs.Animal __p1 => MapBase(__p1)", generated);
    }

    [Fact]
    public void E2E_PolymorphicDispatch_ErrorBlocksGeneration()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Unrelated { public string Name { get; set; } = ""; }
                public class AnimalDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class AnimalForges
                {
                    public static partial AnimalDto MapUnrelated(Unrelated source);

                    [ForgePolymorphic(typeof(Unrelated), nameof(MapUnrelated))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.True(result.HasErrors);
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF802");
    }

    [Fact]
    public void E2E_PolymorphicDispatch_AnalyzerAndGeneratorAgree()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { }
                public class AnimalDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class AnimalForges
                {
                    [ForgePolymorphic(typeof(Dog), "MissingMethod")]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunFull(source);

        var generatorErrors = result.GeneratorDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "FKF800")
            .ToList();
        var analyzerErrors = result.AnalyzerDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == "FKF800")
            .ToList();

        Assert.NotEmpty(generatorErrors);
        Assert.NotEmpty(analyzerErrors);
    }
}
