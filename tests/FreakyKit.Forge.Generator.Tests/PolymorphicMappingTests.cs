using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class PolymorphicMappingTests : GeneratorTestBase
{
    [Fact]
    public void PolymorphicDispatch_InheritanceHierarchy_GeneratesSwitchExpression()
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

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source switch", generated);
        Assert.Contains("TestNs.Dog __p0 => MapDog(__p0)", generated);
        Assert.Contains("TestNs.Cat __p1 => MapCat(__p1)", generated);
        Assert.Contains("throw new InvalidOperationException", generated);
    }

    [Fact]
    public void PolymorphicDispatch_WithBaseFallback_IncludesBaseArm()
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

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("TestNs.Dog __p0 => MapDog(__p0)", generated);
        Assert.Contains("TestNs.Animal __p1 => MapBase(__p1)", generated);
    }

    [Fact]
    public void PolymorphicDispatch_InterfaceReturnType_Works()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { public string Breed { get; set; } = ""; }
                public class Cat : Animal { public bool Indoor { get; set; } }

                public interface IAnimalDto { string Name { get; set; } }
                public class DogDto : IAnimalDto { public string Name { get; set; } = ""; public string Breed { get; set; } = ""; }
                public class CatDto : IAnimalDto { public string Name { get; set; } = ""; public bool Indoor { get; set; } }

                [Forge]
                public static partial class AnimalForges
                {
                    public static partial DogDto MapDog(Dog source);
                    public static partial CatDto MapCat(Cat source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
                    public static partial IAnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("TestNs.Dog __p0 => MapDog(__p0)", generated);
        Assert.Contains("TestNs.Cat __p1 => MapCat(__p1)", generated);
    }

    [Fact]
    public void FKF800_MethodNotFound_EmitsError()
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
                    [ForgePolymorphic(typeof(Dog), "NonExistentMethod")]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF800");
    }

    [Fact]
    public void FKF801_ReturnTypeMismatch_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { public string Breed { get; set; } = ""; }

                public class AnimalDto { public string Name { get; set; } = ""; }
                public class UnrelatedDto { public string Breed { get; set; } = ""; }

                [Forge]
                public static partial class AnimalForges
                {
                    public static partial UnrelatedDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF801");
    }

    [Fact]
    public void FKF802_SourceTypeMismatch_EmitsError()
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

        var result = RunGenerator(source);
        AssertHasError(result, "FKF802");
    }

    [Fact]
    public void FKF803_UnreachablePattern_EmitsError()
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

                    [ForgePolymorphic(typeof(Animal), nameof(MapBase))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF803");
    }

    [Fact]
    public void FKF804_IncompatibleOptions_EmitsError()
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
                    public static partial DogDto MapDog(Dog source);

                    [ForgeMethod(AllowFlattening = true)]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF804");
    }

    [Fact]
    public void FKF805_ExpressionNotSupported_EmitsError()
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
                    public static partial DogDto MapDog(Dog source);

                    [ForgeMethod(GenerateExpression = true)]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF805");
    }

    [Fact]
    public void FKF806_DuplicateSourceType_EmitsError()
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
                    public static partial DogDto MapDog(Dog source);
                    public static partial DogDto MapDog2(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog2))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF806");
    }

    [Fact]
    public void PolymorphicDispatch_UserDeclarationOrder_Preserved()
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
                    public static partial CatDto MapCat(Cat source);
                    public static partial DogDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        var catIndex = generated.IndexOf("TestNs.Cat __p0 => MapCat(__p0)");
        var dogIndex = generated.IndexOf("TestNs.Dog __p1 => MapDog(__p1)");
        Assert.True(catIndex >= 0, "Cat arm should be present in generated code");
        Assert.True(dogIndex >= 0, "Dog arm should be present in generated code");
        Assert.True(catIndex < dogIndex, "Cat arm should appear before Dog arm (user-declared order)");
    }

    [Fact]
    public void PolymorphicDispatch_NoForgeMethodOptions_NoError()
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
                    public static partial DogDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void PolymorphicDispatch_ThreeLevelsDeep_Works()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { public string Breed { get; set; } = ""; }
                public class Poodle : Dog { public string Size { get; set; } = ""; }

                public class AnimalDto { public string Name { get; set; } = ""; }
                public class DogDto : AnimalDto { public string Breed { get; set; } = ""; }
                public class PoodleDto : DogDto { public string Size { get; set; } = ""; }

                [Forge]
                public static partial class AnimalForges
                {
                    public static partial PoodleDto MapPoodle(Poodle source);
                    public static partial DogDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Poodle), nameof(MapPoodle))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        var poodleIndex = generated.IndexOf("TestNs.Poodle __p0 => MapPoodle(__p0)");
        var dogIndex = generated.IndexOf("TestNs.Dog __p1 => MapDog(__p1)");
        Assert.True(poodleIndex >= 0, "Poodle arm should be present in generated code");
        Assert.True(dogIndex >= 0, "Dog arm should be present in generated code");
        Assert.True(poodleIndex < dogIndex, "Poodle arm should appear before Dog arm");
    }

    [Fact]
    public void PolymorphicDispatch_WithForgeUses_ResolvesFromIncludedClass()
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
                public static partial class DogForges
                {
                    public static partial DogDto MapDog(Dog source);
                }

                [Forge]
                [ForgeUses(typeof(DogForges))]
                public static partial class AnimalForges
                {
                    [ForgePolymorphic(typeof(Dog), nameof(DogForges.MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 2);
        Assert.Contains("global::TestNs.DogForges.MapDog(__p0)", generated);
    }

    [Fact]
    public void FKF807_PolymorphicWithoutForgeClass_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { public string Breed { get; set; } = ""; }

                public class AnimalDto { public string Name { get; set; } = ""; }
                public class DogDto : AnimalDto { public string Breed { get; set; } = ""; }

                public static partial class NotAForgeClass
                {
                    public static partial DogDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF807");
    }

    [Fact]
    public void FKF804_DefaultForgeMethodOptions_NoError()
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
                    public static partial DogDto MapDog(Dog source);

                    [ForgeMethod]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }
}
