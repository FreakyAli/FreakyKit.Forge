using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class IgnoreIfNullIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_IgnoreIfNull_OnMethod_GeneratesNullChecks()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(IgnoreIfNull = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("if (source.Name != null) __result.Name = source.Name;", generated);
        Assert.Contains("if (source.Email != null) __result.Email = source.Email;", generated);
    }

    [Fact]
    public void E2E_IgnoreIfNull_OnForgeMap_GeneratesNullCheckForSpecificMember()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name", IgnoreIfNull = true)] public string Name { get; set; } = ""; public string Email { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("if (source.Name != null) __result.Name = source.Name;", generated);
        Assert.DoesNotContain("if (source.Email != null)", generated);
    }

    [Fact]
    public void E2E_IgnoreIfNull_OnUpdateMethod_GeneratesNullChecks()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(IgnoreIfNull = true)]
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("if (source.Name != null) existing.Name = source.Name;", generated);
        Assert.Contains("if (source.Age != null) existing.Age = source.Age;", generated);
    }
}
