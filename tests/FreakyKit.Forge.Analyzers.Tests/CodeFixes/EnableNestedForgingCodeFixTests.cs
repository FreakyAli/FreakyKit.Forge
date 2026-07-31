using System.Threading.Tasks;
using FreakyKit.Forge.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public class EnableNestedForgingCodeFixTests : CodeFixTestBase
{
    protected override CodeFixProvider CreateCodeFixProvider() => new EnableNestedForgingCodeFix();

    [Fact]
    public async Task AddsAllowNestedForgingToExistingForgeMethod()
    {
        const string source = @"
using FreakyKit.Forge;

public class Inner { public string Value { get; set; } }
public class InnerDto { public string Value { get; set; } }
public class Source { public Inner Child { get; set; } }
public class Dest { public InnerDto Child { get; set; } }

[Forge]
public static partial class MyForges
{
    public static partial InnerDto ToInnerDto(Inner source);

    [ForgeMethod]
    public static partial Dest ToDto(Source source);
}";

        const string expected = @"
using FreakyKit.Forge;

public class Inner { public string Value { get; set; } }
public class InnerDto { public string Value { get; set; } }
public class Source { public Inner Child { get; set; } }
public class Dest { public InnerDto Child { get; set; } }

[Forge]
public static partial class MyForges
{
    public static partial InnerDto ToInnerDto(Inner source);

    [ForgeMethod(AllowNestedForging = true)]
    public static partial Dest ToDto(Source source);
}";

        await VerifyCodeFixAsync(source, expected, "FKF300");
    }
}
