using System.Threading.Tasks;
using FreakyKit.Forge.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public class AddForgeAttributeCodeFixTests : GeneratorCodeFixTestBase
{
    protected override CodeFixProvider CreateCodeFixProvider() => new AddForgeAttributeCodeFix();

    [Fact]
    public async Task FKF525_AddsForgeToClassWithForgeMethod()
    {
        const string source = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

public static partial class MyForges
{
    [ForgeMethod]
    public static partial Dest ToDto(Source source);
}";

        const string expected = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

[Forge]
public static partial class MyForges
{
    [ForgeMethod]
    public static partial Dest ToDto(Source source);
}";

        await VerifyCodeFixAsync(source, expected, "FKF525");
    }

    [Fact]
    public async Task FKF526_AddsForgeToClassWithForgeConverter()
    {
        const string source = @"
using FreakyKit.Forge;

public static partial class MyForges
{
    [ForgeConverter]
    public static string Convert(int value) => value.ToString();
}";

        const string expected = @"
using FreakyKit.Forge;

[Forge]
public static partial class MyForges
{
    [ForgeConverter]
    public static string Convert(int value) => value.ToString();
}";

        await VerifyCodeFixAsync(source, expected, "FKF526");
    }
}
