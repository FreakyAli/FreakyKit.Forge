using System.Threading.Tasks;
using FreakyKit.Forge.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests.CodeFixes;

public class RemoveForgeMapCodeFixTests : CodeFixTestBase
{
    protected override CodeFixProvider CreateCodeFixProvider() => new RemoveForgeMapCodeFix();

    [Fact]
    public async Task FKF109_RemovesForgeMapWhenBothIgnoreAndMapPresent()
    {
        const string source = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest
{
    [ForgeIgnore]
    [ForgeMap(""Name"")]
    public string Name { get; set; }
}

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        const string expected = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest
{
    [ForgeIgnore]
    public string Name { get; set; }
}

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        await VerifyCodeFixAsync(source, expected, "FKF109");
    }

    [Fact]
    public async Task FKF112_RemovesForgeMapWhenSelfReferencing()
    {
        const string source = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest
{
    [ForgeMap(""Name"")]
    public string Name { get; set; }
}

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        const string expected = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest
{
    public string Name { get; set; }
}

[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        await VerifyCodeFixAsync(source, expected, "FKF112");
    }
}
