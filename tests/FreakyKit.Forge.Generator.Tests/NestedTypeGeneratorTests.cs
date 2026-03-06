using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class NestedTypeGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void NestedForgeClass_GeneratesCorrectPartialNesting()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                public partial class Outer
                {
                    [Forge]
                    public static partial class InnerForges
                    {
                        public static partial Dest ToDest(Source source);
                    }
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("partial class Outer", generated);
        Assert.Contains("public static partial class InnerForges", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void DoublyNestedForgeClass_GeneratesCorrectPartialChain()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Age { get; set; } }
                public class Dest   { public int Age { get; set; } }

                public partial class Outer
                {
                    internal partial class Middle
                    {
                        [Forge]
                        public static partial class DeepForges
                        {
                            public static partial Dest ToDest(Source source);
                        }
                    }
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("partial class Outer", generated);
        Assert.Contains("partial class Middle", generated);
        Assert.Contains("public static partial class DeepForges", generated);
        Assert.Contains("__result.Age = source.Age", generated);
    }

    [Fact]
    public void NestedForgeClass_CompilesWithoutErrors()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                public partial class Outer
                {
                    [Forge]
                    public static partial class InnerForges
                    {
                        public static partial Dest ToDest(Source source);
                    }
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
    }

    [Fact]
    public void TopLevelForgeClass_StillWorksWithNoContainingTypes()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        Assert.False(result.HasCompilationErrors);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("public static partial class MyForges", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }
}
