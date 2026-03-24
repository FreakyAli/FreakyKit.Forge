using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class InitOnlyGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void InitOnlyProperty_UsesObjectInitializer()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Id { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Id { get; init; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        // Id should be in object initializer, not as __result.Id = ...
        Assert.Contains("Id = source.Id", generated);
        Assert.DoesNotContain("__result.Id", generated);
        // Name is regular, assigned normally
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void InitOnlyProperty_CompilesWithoutErrors()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Id { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Id { get; init; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Id = source.Id", generated);
        Assert.DoesNotContain("__result.Id", generated);
    }

    [Fact]
    public void InitOnlyProperty_UpdateMethod_SkippedInAssignment()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Id { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Id { get; init; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("existing.Name = source.Name", generated);
        Assert.DoesNotContain("existing.Id", generated);
    }

    [Fact]
    public void AllInitOnly_CreateMethod_UsesObjectInitializer()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; init; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Name = source.Name", generated);
        Assert.DoesNotContain("__result.Name", generated);
    }

    [Fact]
    public void MixedInitAndRegular_SplitsCorrectly()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public int Id { get; set; }
                    public string FirstName { get; set; } = "";
                    public string LastName { get; set; } = "";
                    public string Email { get; set; } = "";
                }
                public class Dest
                {
                    public int Id { get; init; }
                    public string FirstName { get; init; } = "";
                    public string LastName { get; set; } = "";
                    public string Email { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        var generated = AssertSingleGeneratedFile(result);
        // Init-only in object initializer
        Assert.Contains("Id = source.Id", generated);
        Assert.Contains("FirstName = source.FirstName", generated);
        Assert.DoesNotContain("__result.Id", generated);
        Assert.DoesNotContain("__result.FirstName", generated);
        // Regular via assignment
        Assert.Contains("__result.LastName = source.LastName", generated);
        Assert.Contains("__result.Email = source.Email", generated);
    }

    [Fact]
    public void Record_InitOnlyProperties_UsesObjectInitializer()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                }
                public record Dest
                {
                    public int Id { get; init; }
                    public string Name { get; init; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("Id = source.Id", generated);
        Assert.Contains("Name = source.Name", generated);
        Assert.DoesNotContain("__result.Id", generated);
        Assert.DoesNotContain("__result.Name", generated);
    }

    [Fact]
    public void MixedInitAndRegular_UpdateMethod_SkipsInitOnly()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = "";
                    public string Email { get; set; } = "";
                }
                public class Dest
                {
                    public int Id { get; init; }
                    public string Name { get; set; } = "";
                    public string Email { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.DoesNotContain("existing.Id", generated);
        Assert.Contains("existing.Name = source.Name", generated);
        Assert.Contains("existing.Email = source.Email", generated);
    }
}
