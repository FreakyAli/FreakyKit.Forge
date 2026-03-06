using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

/// <summary>
/// End-to-end integration tests for update mapping (void return, 2 parameters).
/// </summary>
public sealed class UpdateMappingIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_UpdateMapping_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("void Update(Source source, Dest existing)", generated);
        Assert.Contains("existing.Name = source.Name", generated);
        Assert.Contains("existing.Age = source.Age", generated);
        Assert.DoesNotContain("var __result", generated);
        Assert.DoesNotContain("return ", generated);

        // FKF040 info diagnostic should be present
        Assert.Contains(result.AllDiagnostics, d =>
            d.Id == "FKF040" && d.Severity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void E2E_UpdateMapping_WithCreateMethod_BothWork()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDto(Source source);
                    public static partial void ApplyTo(Source source, Dest existing);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = string.Join("\n", result.RunResult.GeneratedTrees.Select(t => t.GetText(TestContext.Current.CancellationToken).ToString()));

        // Create method present
        Assert.Contains("Dest ToDto(Source source)", generated);
        Assert.Contains("var __result = new Dest()", generated);

        // Update method present
        Assert.Contains("void ApplyTo(Source source, Dest existing)", generated);
        Assert.Contains("existing.Name = source.Name", generated);
    }
}
