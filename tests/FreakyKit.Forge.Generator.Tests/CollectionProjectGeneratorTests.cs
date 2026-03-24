using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class CollectionProjectGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void CollectionProject_SameElementType_GeneratesDirectMaterialization()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial List<Source> ToList(List<Source> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains(".ToList()", generated);
        Assert.DoesNotContain("Select(x => x)", generated);
    }

    [Fact]
    public void CollectionProject_DifferentElementTypes_UsesNestedForgeMethod()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial PersonDto ToDto(Person source);
                    public static partial List<PersonDto> ToDtos(List<Person> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Select(x => ToDto(x))", generated);
        Assert.Contains(".ToList()", generated);
        // The projection method body should be a single return — no construction
        Assert.Contains("return source", generated);
    }

    [Fact]
    public void CollectionProject_NoMatchingElementForge_EmitsFKF200()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    // No ToDto forge method — can't project
                    public static partial List<PersonDto> ToDtos(List<Person> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF200");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void CollectionProject_ArrayToArray_UsesToArray()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto ToItemDto(Item source);
                    public static partial ItemDto[] ToItemDtos(Item[] source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Select(x => ToItemDto(x))", generated);
        Assert.Contains(".ToArray()", generated);
    }

    [Fact]
    public void CollectionProject_NullSafe_ReferenceTypeSource()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial PersonDto ToDto(Person source);
                    public static partial List<PersonDto> ToDtos(List<Person> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source != null ?", generated);
        Assert.Contains(": null", generated);
    }

    [Fact]
    public void CollectionProject_CompilesWithoutErrors()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial PersonDto ToDto(Person source);
                    public static partial List<PersonDto> ToDtos(List<Person> source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        AssertNoErrors(result);
    }
}
