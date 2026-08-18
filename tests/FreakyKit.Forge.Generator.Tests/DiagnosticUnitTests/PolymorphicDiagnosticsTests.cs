using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Polymorphic Mapping diagnostics (FKF800–FKF807).
/// Tests polymorphic dispatch method validation and type compatibility.
/// </summary>
public sealed class PolymorphicDiagnosticsTests : DiagnosticsTestBase
{
    [Fact]
    public void FKF800_PolymorphicMethodNotFound_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { }
                public class Dog : Animal { }

                public class AnimalDto { }
                public class DogDto : AnimalDto { }

                [Forge]
                public static partial class Forges
                {
                    [ForgePolymorphic(typeof(Dog), nameof(NonExistentMethod))]
                    public static partial AnimalDto MapAnimal(Animal source);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF800");
        AssertDiagnosticWithSeverity(source, "FKF800", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF803_UnreachablePolymorphicPattern_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { }
                public class Dog : Animal { }

                public class AnimalDto { }
                public class DogDto : AnimalDto { }

                [Forge]
                public static partial class Forges
                {
                    public static partial DogDto MapDog(Dog source);
                    public static partial AnimalDto MapBase(Animal source);

                    [ForgePolymorphic(typeof(Animal), nameof(MapBase))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF803");
        AssertDiagnosticWithSeverity(source, "FKF803", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF805_ExpressionOnPolymorphic_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { }
                public class Dog : Animal { }

                public class AnimalDto { }
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

        AssertDiagnosticEmitted(source, "FKF805");
        AssertDiagnosticWithSeverity(source, "FKF805", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF806_DuplicatePolymorphicSourceType_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Animal { }
                public class Dog : Animal { }

                public class AnimalDto { }
                public class DogDto : AnimalDto { }

                [Forge]
                public static partial class Forges
                {
                    public static partial DogDto MapDog1(Dog source);
                    public static partial DogDto MapDog2(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog1))]
                    [ForgePolymorphic(typeof(Dog), nameof(MapDog2))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF806");
        AssertDiagnosticWithSeverity(source, "FKF806", DiagnosticSeverity.Error);
    }
}
