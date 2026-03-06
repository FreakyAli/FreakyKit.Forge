using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class InitOnlyGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void InitOnlyProperty_SkippedInAssignment()
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
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.DoesNotContain("__result.Id", generated);
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
    public void AllInitOnly_CreateMethod_EmptyBody()
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
    }
}
