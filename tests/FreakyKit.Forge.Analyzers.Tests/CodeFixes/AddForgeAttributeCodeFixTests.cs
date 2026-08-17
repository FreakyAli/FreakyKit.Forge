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

    [Fact]
    public async Task FKF524_AddsForgeToClassWithForgeUses()
    {
        const string source = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

[Forge]
public static partial class OtherForges
{
    public static partial Dest ToDto(Source source);
}

[ForgeUses(typeof(OtherForges))]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        const string expected = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

[Forge]
public static partial class OtherForges
{
    public static partial Dest ToDto(Source source);
}

[ForgeUses(typeof(OtherForges))]
[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        await VerifyCodeFixAsync(source, expected, "FKF524");
    }

    [Fact]
    public async Task FKF538_AddsForgeToClassWithForgeIncludes()
    {
        const string source = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

[Forge]
public static partial class BaseForges
{
    public static partial Dest ToDto(Source source);
}

[ForgeIncludes(typeof(BaseForges))]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        const string expected = @"
using FreakyKit.Forge;

public class Source { public string Name { get; set; } }
public class Dest { public string Name { get; set; } }

[Forge]
public static partial class BaseForges
{
    public static partial Dest ToDto(Source source);
}

[ForgeIncludes(typeof(BaseForges))]
[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}";

        await VerifyCodeFixAsync(source, expected, "FKF538");
    }
}
