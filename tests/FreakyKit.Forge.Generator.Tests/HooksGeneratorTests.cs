using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class HooksGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void BeforeHook_GeneratesCall()
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
                    public static partial Dest ToDest(Source source);
                    static partial void OnBeforeToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("OnBeforeToDest(source);", generated);
        // Before hook should come before assignments
        var beforeIdx = generated.IndexOf("OnBeforeToDest");
        var assignIdx = generated.IndexOf("__result.Name");
        Assert.True(beforeIdx < assignIdx);
    }

    [Fact]
    public void AfterHook_GeneratesCall()
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
                    public static partial Dest ToDest(Source source);
                    static partial void OnAfterToDest(Source source, Dest result);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("OnAfterToDest(source, __result);", generated);
        // After hook should come after assignments
        var assignIdx = generated.IndexOf("__result.Name");
        var afterIdx = generated.IndexOf("OnAfterToDest");
        Assert.True(afterIdx > assignIdx);
    }

    [Fact]
    public void BothHooks_GeneratesBothCalls()
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
                    public static partial Dest ToDest(Source source);
                    static partial void OnBeforeToDest(Source source);
                    static partial void OnAfterToDest(Source source, Dest result);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("OnBeforeToDest(source);", generated);
        Assert.Contains("OnAfterToDest(source, __result);", generated);
    }

    [Fact]
    public void NoHooks_NoHookCalls()
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
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.DoesNotContain("OnBefore", generated);
        Assert.DoesNotContain("OnAfter", generated);
    }
}
