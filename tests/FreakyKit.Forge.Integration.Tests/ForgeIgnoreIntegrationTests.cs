using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class ForgeIgnoreIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_ForgeIgnore_SourceMember_GeneratesWithoutIgnored()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public string InternalId { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }

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
        // No FKF101 for InternalId
        Assert.DoesNotContain(result.AllDiagnostics, d => d.Id == "FKF101");
    }

    [Fact]
    public void E2E_ForgeIgnore_DestMember_GeneratesWithoutIgnored()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
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

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);
        // No FKF100 for Score
        Assert.DoesNotContain(result.AllDiagnostics, d => d.Id == "FKF100");

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.DoesNotContain("__result.Score", generated);
    }

    [Fact]
    public void E2E_ForgeIgnore_BothSides_CleanGeneration()
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
                    [ForgeIgnore]
                    public int Computed { get; set; }
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
        Assert.DoesNotContain(result.AllDiagnostics, d => d.Id == "FKF100");
        Assert.DoesNotContain(result.AllDiagnostics, d => d.Id == "FKF101");
    }
}
