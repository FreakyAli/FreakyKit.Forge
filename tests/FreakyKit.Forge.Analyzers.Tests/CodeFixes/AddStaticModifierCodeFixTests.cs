using System.Threading.Tasks;
using FreakyKit.Forge.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public class AddStaticModifierCodeFixTests : CodeFixTestBase
{
    protected override CodeFixProvider CreateCodeFixProvider() => new AddStaticModifierCodeFix();

    [Fact]
    public async Task AddsStaticModifierToForgeClass()
    {
        const string source = @"
using FreakyKit.Forge;

[Forge]
public class MyForges { }";

        const string expected = @"
using FreakyKit.Forge;

[Forge]
public static class MyForges { }";

        await VerifyCodeFixAsync(source, expected, "FKF003");
    }

    [Fact]
    public async Task NoDiagnosticForStaticClass()
    {
        const string source = @"
using FreakyKit.Forge;

[Forge]
public static partial class MyForges { }";

        await VerifyNoCodeFixAsync(source, "FKF003");
    }
}
