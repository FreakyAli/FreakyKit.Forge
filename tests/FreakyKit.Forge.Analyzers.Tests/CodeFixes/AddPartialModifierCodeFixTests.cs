using System.Threading.Tasks;
using FreakyKit.Forge.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public class AddPartialModifierCodeFixTests : CodeFixTestBase
{
    protected override CodeFixProvider CreateCodeFixProvider() => new AddPartialModifierCodeFix();

    [Fact]
    public async Task AddsPartialModifierToForgeClass()
    {
        const string source = @"
using FreakyKit.Forge;

[Forge]
public static class MyForges { }";

        const string expected = @"
using FreakyKit.Forge;

[Forge]
public static partial class MyForges { }";

        await VerifyCodeFixAsync(source, expected, "FKF004");
    }

    [Fact]
    public async Task NoDiagnosticForPartialClass()
    {
        const string source = @"
using FreakyKit.Forge;

[Forge]
public static partial class MyForges { }";

        await VerifyNoCodeFixAsync(source, "FKF004");
    }
}
