using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

/// <summary>
/// End-to-end tests for the Projection Expressions feature (Phase 1).
/// Verifies generator + analyzer + compiler all agree the generated expression property is valid C#.
/// </summary>
public sealed class ExpressionIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void Expression_FullPipeline_NoErrors_AndCompiles()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class PersonForges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors,
            string.Join("\n", result.AllDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        Assert.True(result.HasGeneratedSource);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("public static Expression<Func<Person, PersonDto>> ToDtoExpression", generated);
    }

    [Fact]
    public void Expression_UpdateMethod_FKF504_BlocksGeneration()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }

                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial void Update(A source, B existing);
                }
            }
            """;

        var result = RunFull(source);

        Assert.True(result.HasErrors);
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF504" && d.Severity == DiagnosticSeverity.Error);
        Assert.False(result.HasGeneratedSource);
    }

    [Fact]
    public void Expression_WithHook_FKF505_StillGenerates()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }

                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                    static partial void OnBeforeMap(A source);
                }
            }
            """;

        var result = RunFull(source);

        // Warning, not error — generation must still happen.
        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF505" && d.Severity == DiagnosticSeverity.Warning);
        Assert.False(result.HasCompilationErrors);
    }

    [Fact]
    public void Expression_NoFlag_NoExpressionEmitted_NoLinqExpressionsImport()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }

                [Forge]
                public static partial class F
                {
                    public static partial B Map(A source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.DoesNotContain("Expression<Func<", generated);
        Assert.DoesNotContain("using System.Linq.Expressions;", generated);
    }
}
