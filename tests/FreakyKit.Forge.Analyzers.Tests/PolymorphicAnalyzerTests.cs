using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class PolymorphicAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public void FKF800_MethodNotFound_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Dog : Animal { }
                public class AnimalDto { public string Name { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgePolymorphic(typeof(Dog), "NonExistent")]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF800");
    }

    [Fact]
    public void FKF801_ReturnTypeMismatch_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Dog : Animal { }
                public class AnimalDto { public string Name { get; set; } }
                public class UnrelatedDto { public string Name { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    public static partial UnrelatedDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF801");
    }

    [Fact]
    public void FKF802_SourceTypeMismatch_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Unrelated { public string Name { get; set; } }
                public class AnimalDto { public string Name { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    public static partial AnimalDto MapUnrelated(Unrelated source);

                    [ForgePolymorphic(typeof(Unrelated), nameof(MapUnrelated))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF802");
    }

    [Fact]
    public void FKF803_UnreachablePattern_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Dog : Animal { }
                public class AnimalDto { public string Name { get; set; } }
                public class DogDto : AnimalDto { }

                [Forge]
                public static partial class Forges
                {
                    public static partial AnimalDto MapBase(Animal source);
                    public static partial DogDto MapDog(Dog source);

                    [ForgePolymorphic(typeof(Animal), nameof(MapBase))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF803");
    }

    [Fact]
    public void FKF804_IncompatibleOptions_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Dog : Animal { }
                public class AnimalDto { public string Name { get; set; } }
                public class DogDto : AnimalDto { }

                [Forge]
                public static partial class Forges
                {
                    public static partial DogDto MapDog(Dog source);

                    [ForgeMethod(AllowFlattening = true)]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF804");
    }

    [Fact]
    public void FKF805_ExpressionNotSupported_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Dog : Animal { }
                public class AnimalDto { public string Name { get; set; } }
                public class DogDto : AnimalDto { }

                [Forge]
                public static partial class Forges
                {
                    public static partial DogDto MapDog(Dog source);

                    [ForgeMethod(GenerateExpression = true)]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF805");
    }

    [Fact]
    public void FKF806_DuplicateSourceType_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Dog : Animal { }
                public class AnimalDto { public string Name { get; set; } }
                public class DogDto : AnimalDto { }

                [Forge]
                public static partial class Forges
                {
                    public static partial DogDto MapDog(Dog source);
                    public static partial DogDto MapDog2(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog2))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF806");
    }

    [Fact]
    public void ValidPolymorphicMethod_NoDiagnosticErrors()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { public string Name { get; set; } }
                public class Dog : Animal { public string Breed { get; set; } }
                public class Cat : Animal { public bool Indoor { get; set; } }

                public class AnimalDto { public string Name { get; set; } }
                public class DogDto : AnimalDto { public string Breed { get; set; } }
                public class CatDto : AnimalDto { public bool Indoor { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    public static partial DogDto MapDog(Dog source);
                    public static partial CatDto MapCat(Cat source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF800");
        AssertNotContainsDiagnostic(source, "FKF801");
        AssertNotContainsDiagnostic(source, "FKF802");
        AssertNotContainsDiagnostic(source, "FKF803");
        AssertNotContainsDiagnostic(source, "FKF804");
        AssertNotContainsDiagnostic(source, "FKF805");
        AssertNotContainsDiagnostic(source, "FKF806");
    }
}
