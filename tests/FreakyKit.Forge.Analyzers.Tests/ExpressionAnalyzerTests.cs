using System.Linq;
using FreakyKit.Forge.Analyzers;
using FreakyKit.Forge.Diagnostics;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for the analyzer's exposure of the Projection Expression diagnostics (FKF504, FKF505).
/// The generator owns the conditions in Phase 1; the analyzer just registers the descriptors so
/// IDE tooling (e.g. Roslyn's diagnostic catalogue, "Error List" filters) can surface them.
/// </summary>
public sealed class ExpressionAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public void Analyzer_SupportedDiagnostics_IncludesFKF504()
    {
        var analyzer = new ForgeAnalyzer();
        Assert.Contains(analyzer.SupportedDiagnostics, d => d.Id == "FKF504");
    }

    [Fact]
    public void Analyzer_SupportedDiagnostics_IncludesFKF505()
    {
        var analyzer = new ForgeAnalyzer();
        Assert.Contains(analyzer.SupportedDiagnostics, d => d.Id == "FKF505");
    }

    [Fact]
    public void FKF504_DescriptorSeverity_IsError()
    {
        var analyzer = new ForgeAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == "FKF504");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, descriptor.DefaultSeverity);
    }

    [Fact]
    public void FKF505_DescriptorSeverity_IsWarning()
    {
        var analyzer = new ForgeAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == "FKF505");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
    }

    [Fact]
    public void FKF504_DescriptorCategory_IsMethodShape()
    {
        var analyzer = new ForgeAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == "FKF504");
        Assert.Equal("FreakyKit.Forge.MethodShape", descriptor.Category);
    }

    [Fact]
    public void FKF505_DescriptorCategory_IsMethodShape()
    {
        var analyzer = new ForgeAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics.First(d => d.Id == "FKF505");
        Assert.Equal("FreakyKit.Forge.MethodShape", descriptor.Category);
    }
}
