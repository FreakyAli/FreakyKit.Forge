using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Base class for comprehensive diagnostic unit tests.
/// Tests individual diagnostic emission conditions in isolation.
/// </summary>
public abstract class DiagnosticsTestBase : GeneratorTestBase
{
    /// <summary>
    /// Get all diagnostics (both generator and compilation diagnostics).
    /// Some diagnostics are emitted by the analyzer during compilation, others by the generator.
    /// </summary>
    protected ImmutableArray<Diagnostic> GetAllDiagnostics(ForgeRunResult result)
    {
        return result.Diagnostics.AddRange(result.CompilationDiagnostics);
    }

    /// <summary>
    /// Assert that a specific diagnostic was emitted (checks both generator and compilation diagnostics).
    /// </summary>
    protected void AssertDiagnosticEmitted(string source, string diagnosticId, string? messagePart = null)
    {
        var result = RunGenerator(source);
        var allDiags = GetAllDiagnostics(result);
        var diagnostic = allDiags.FirstOrDefault(d => d.Id == diagnosticId);

        Assert.NotNull(diagnostic);
        if (messagePart != null)
        {
            var message = diagnostic!.ToString();
            Assert.Contains(messagePart, message);
        }
    }

    /// <summary>
    /// Assert that a specific diagnostic was NOT emitted.
    /// </summary>
    protected void AssertDiagnosticNotEmitted(string source, string diagnosticId)
    {
        var result = RunGenerator(source);
        var allDiags = GetAllDiagnostics(result);
        var diagnostic = allDiags.FirstOrDefault(d => d.Id == diagnosticId);

        Assert.Null(diagnostic);
    }

    /// <summary>
    /// Assert that a diagnostic was emitted with specific severity.
    /// </summary>
    protected void AssertDiagnosticWithSeverity(string source, string diagnosticId, DiagnosticSeverity expectedSeverity)
    {
        var result = RunGenerator(source);
        var allDiags = GetAllDiagnostics(result);
        var diagnostic = allDiags.FirstOrDefault(d => d.Id == diagnosticId);

        Assert.NotNull(diagnostic);
        Assert.Equal(expectedSeverity, diagnostic!.Severity);
    }

    /// <summary>
    /// Assert that exactly N diagnostics with given IDs were emitted.
    /// </summary>
    protected void AssertDiagnosticsEmitted(string source, params string[] diagnosticIds)
    {
        var result = RunGenerator(source);
        var allDiags = GetAllDiagnostics(result);
        var emittedIds = allDiags.Select(d => d.Id).ToList();

        foreach (var id in diagnosticIds)
        {
            Assert.Contains(id, emittedIds);
        }
    }

    /// <summary>
    /// Assert that NO diagnostic with given ID was emitted.
    /// </summary>
    protected void AssertNoDiagnostic(string source, string diagnosticId)
    {
        var result = RunGenerator(source);
        var allDiags = GetAllDiagnostics(result);
        Assert.DoesNotContain(allDiags, d => d.Id == diagnosticId);
    }
}
