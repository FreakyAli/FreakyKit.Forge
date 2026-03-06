using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class ConstructorIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_ParameterizedConstructor_NoParameterless_GeneratesCorrectly()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
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

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("new Dest(source.Name, source.Age)", generated);
    }

    [Fact]
    public void E2E_ParameterizedConstructor_WithExtraSettableProperty()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                    public string Email { get; set; } = "";
                }
                public class Dest
                {
                    public string Name { get; }
                    public int Age { get; }
                    public string Email { get; set; } = "";
                    public Dest(string name, int age) { Name = name; Age = age; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        // Constructor args
        Assert.Contains("new Dest(source.Name, source.Age)", generated);
        // Additional settable property assigned after construction
        Assert.Contains("__result.Email = source.Email", generated);
    }

    [Fact]
    public void E2E_NoPublicConstructor_EmitsFKF502Error()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    private Dest() { }
                    public string Name { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.True(result.HasErrors);
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF502");
        Assert.False(result.HasGeneratedSource);
    }
}
