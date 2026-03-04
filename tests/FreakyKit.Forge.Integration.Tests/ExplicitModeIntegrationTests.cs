using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class ExplicitModeIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_ExplicitMode_OnlyAttributedMethodGenerated()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class DestA  { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class DestB  { public string Name { get; set; } = ""; public int Age { get; set; } }

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
                    public static partial DestA ToDestA(Source source);

                    public static partial DestB ToDestB(Source source);
                }
            }
            """;

        var result = RunFull(source);

        // FKF002 warning for the unmarked method
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF002");

        // Only the attributed method generates code
        var generated = string.Join("\n", result.RunResult.GeneratedTrees.Select(t => t.GetText().ToString()));
        Assert.Contains("ToDestA(", generated);
        Assert.DoesNotContain("ToDestB(", generated);
    }

    [Fact]
    public void E2E_ExplicitMode_AttributedMethodMapsCorrectly()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Age = source.Age", generated);
    }

    [Fact]
    public void E2E_ExplicitMode_NoAttributedMethods_NoGeneration()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);

        // FKF002 warning for the unmarked method
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF002");

        // The unmarked method should NOT appear in generated code
        var generated = string.Join("\n", result.RunResult.GeneratedTrees.Select(t => t.GetText().ToString()));
        Assert.DoesNotContain("ToDest(", generated);
    }
}
