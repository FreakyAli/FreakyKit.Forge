using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class IgnoreGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void ForgeIgnore_OnSourceMember_NotAssigned()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public string Secret { get; set; } = "";
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public string Secret { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Name = source.Name", generated);
        // Secret is ignored on source side, so dest.Secret has no source match — not assigned
        Assert.DoesNotContain("__result.Secret = source.Secret", generated);
    }

    [Fact]
    public void ForgeIgnore_OnDestMember_NotAssigned()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Score { get; set; }
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public int Score { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Name = source.Name", generated);
        // Score is ignored on dest side — not assigned even though source has it
        Assert.DoesNotContain("__result.Score", generated);
    }

    [Fact]
    public void ForgeIgnore_NotInConstructorArgs()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeIgnore]
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                }
                public class Dest
                {
                    public string Name { get; }
                    public int Age { get; }
                    public Dest(string name, int age) { Name = name; Age = age; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // Name is ignored on source, so the constructor can't be satisfied
        // This should result in an error (FKF501 or FKF502)
        Assert.Contains(result.Diagnostics, d =>
            d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error &&
            (d.Id == "FKF501" || d.Id == "FKF502"));
        var generated = result.RunResult.GeneratedTrees;
        Assert.Empty(generated);
    }
}
