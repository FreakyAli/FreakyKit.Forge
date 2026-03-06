using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class PrivateMethodIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_ShouldIncludePrivate_GeneratesPrivateMethodBody()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge(ShouldIncludePrivate = true)]
                public static partial class MyForges
                {
                    static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("ToDest(", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void E2E_ShouldIncludePrivate_EmitsFKF011Info()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge(ShouldIncludePrivate = true)]
                public static partial class MyForges
                {
                    static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.Contains(result.AllDiagnostics, d =>
            d.Id == "FKF011" && d.Severity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void E2E_PrivateMethodWithoutFlag_Ignored()
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
                    static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);

        // FKF010 warning emitted by the analyzer
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF010");

        // Private method is ignored — no method body generated for it
        var generated = string.Join("\n", result.RunResult.GeneratedTrees.Select(t => t.GetText(TestContext.Current.CancellationToken).ToString()));
        Assert.DoesNotContain("ToDest(", generated);
    }
}
