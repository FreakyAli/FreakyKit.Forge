using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class NestedTypeIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_NestedForgeClass_GeneratesAndCompiles()
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

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("partial class Outer", generated);
        Assert.Contains("public static partial class InnerForges", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void E2E_DoublyNestedForgeClass_GeneratesAndCompiles()
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

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("partial class Outer", generated);
        Assert.Contains("partial class Middle", generated);
        Assert.Contains("public static partial class DeepForges", generated);
    }
}
