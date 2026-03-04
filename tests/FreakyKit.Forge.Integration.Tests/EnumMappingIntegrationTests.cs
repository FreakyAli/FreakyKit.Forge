using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

/// <summary>
/// End-to-end integration tests for enum-to-enum mapping.
/// Runs both generator and analyzer together.
/// </summary>
public sealed class EnumMappingIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_EnumCast_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public string Name { get; set; } = ""; public SourceStatus Status { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public DestStatus   Status { get; set; } }

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
        Assert.Contains("(DestStatus)source.Status", generated);
        Assert.Contains("__result.Name = source.Name", generated);

        // No FKF200 should be emitted for enum-to-enum
        Assert.DoesNotContain(result.AllDiagnostics, d => d.Id == "FKF200");
    }

    [Fact]
    public void E2E_EnumByName_GeneratesSuccessfully()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public SourceStatus Status { get; set; } }
                public class Dest   { public DestStatus   Status { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(EnumMappingStrategy = ForgeEnumMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("source.Status switch", generated);
        Assert.Contains("SourceStatus.Active => DestStatus.Active", generated);

        // No FKF200 should be emitted for enum-to-enum
        Assert.DoesNotContain(result.AllDiagnostics, d => d.Id == "FKF200");
    }
}
