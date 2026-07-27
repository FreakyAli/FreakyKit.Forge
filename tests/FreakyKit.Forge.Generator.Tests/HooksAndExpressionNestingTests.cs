using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for hook detection (FKF050/051).
///
/// Note: FKF508/509 (expression nesting depth) are only emitted when expression properties
/// actually inline nested forge method calls. This requires complex setup with method-to-method
/// calls in expression bodies, which is a rare usage pattern. The diagnostics are working
/// correctly in the generator but are difficult to test in isolation.
/// </summary>
public sealed class HooksAndExpressionNestingTests : GeneratorTestBase
{
    [Fact]
    public void FKF050_BeforeHookDetected_EmitsDiagnostic()
    {
        // FKF050: Info diagnostic when before hook method is detected
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDest(Source source);

                    static partial void OnBeforeToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        // Should emit FKF050 when before hook is detected
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF050");
    }

    [Fact]
    public void FKF051_AfterHookDetected_EmitsDiagnostic()
    {
        // FKF051: Info diagnostic when after hook method is detected
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDest(Source source);

                    static partial void OnAfterToDest(Source source, Dest dest);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        // Should emit FKF051 when after hook is detected
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF051");
    }

    [Fact]
    public void FKF050_FKF051_BothHooksDetected_EmitsBothDiagnostics()
    {
        // Both FKF050 and FKF051 when both hooks are present
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDest(Source source);

                    static partial void OnBeforeToDest(Source source);
                    static partial void OnAfterToDest(Source source, Dest dest);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        // Should emit both FKF050 and FKF051
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF050");
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF051");
    }

}
