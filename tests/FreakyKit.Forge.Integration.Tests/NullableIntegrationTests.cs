using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

public sealed class NullableIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void E2E_NullableInt_ToInt_GeneratesWithWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Value { get; set; } }
                public class Dest   { public int  Value { get; set; } }

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
        Assert.Contains("source.Value.Value", generated);
    }

    [Fact]
    public void E2E_Int_ToNullableInt_GeneratesCleanly()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int  Value { get; set; } }
                public class Dest   { public int? Value { get; set; } }

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
    }

    [Fact]
    public void E2E_NullableAndRegular_MixedMapping()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int? Score { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int  Score { get; set; } }

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
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Score = source.Score.Value", generated);
    }

    [Fact]
    public void E2E_NullableInt_WithDefaultValue_GeneratesNullCoalescing()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Age", DefaultValue = 0)] public int? Age { get; set; } }
                public class Dest   { public int Age { get; set; } }

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
        Assert.Contains("source.Age ?? 0", generated);
        Assert.DoesNotContain("source.Age.Value", generated);
    }

    [Fact]
    public void E2E_NullableInt_WithDefaultValue_NoFKF201Warning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Score", DefaultValue = -1)] public int? Score { get; set; } }
                public class Dest   { public int Score { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.DoesNotContain(result.GeneratorDiagnostics, d => d.Id == "FKF201");
    }
}
