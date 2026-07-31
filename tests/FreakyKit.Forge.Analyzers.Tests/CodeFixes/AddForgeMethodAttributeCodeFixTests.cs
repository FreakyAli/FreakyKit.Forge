using System.Threading.Tasks;
using FreakyKit.Forge.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public class AddForgeMethodAttributeCodeFixTests : CodeFixTestBase
{
    protected override CodeFixProvider CreateCodeFixProvider() => new AddForgeMethodAttributeCodeFix();

    [Fact]
    public async Task AddsForgeMethodAttributeInExplicitMode()
    {
        const string source = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

[Forge(Mode = ForgeMode.Explicit)]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        const string expected = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

[Forge(Mode = ForgeMode.Explicit)]
public static partial class MyForges
{
    [ForgeMethod]
    public static partial Dest ToDto(Source source);
}";

        await VerifyCodeFixAsync(source, expected, "FKF002");
    }
}
