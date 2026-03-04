using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class FieldIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_IncludeFields_MapsFieldsAndProperties()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Score;
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Score;
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(IncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Score = source.Score", generated);
    }

    [Fact]
    public void E2E_IncludeFields_EmitsFKF401Info()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Score;
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Score;
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(IncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.Contains(result.AllDiagnostics, d =>
            d.Id == "FKF401" && d.Severity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void E2E_WithoutIncludeFields_FieldsIgnored()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Score;
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Score;
                }

                [ForgeClass]
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
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.DoesNotContain("__result.Score", generated);
    }
}
