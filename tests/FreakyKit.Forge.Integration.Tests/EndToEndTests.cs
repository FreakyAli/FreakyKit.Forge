using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Integration.Tests;

/// <summary>
/// End-to-end integration tests combining the generator and analyzer.
/// </summary>
public sealed class EndToEndTests : IntegrationTestBase
{
    [Fact]
    public void E2E_SimpleMapping_GeneratesAndNoErrors()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }

                [ForgeClass]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("PersonDto ToDto(", generated);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Age = source.Age", generated);
    }

    [Fact]
    public void E2E_Error_BlocksGeneration_NoSourceOutput()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int    Value { get; set; } }
                public class Dest   { public string Value { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.True(result.HasErrors);
        Assert.False(result.HasGeneratedSource);
    }

    [Fact]
    public void E2E_MixedWarningsAndSuccess_GeneratesWithWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Extra { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        // FKF101 warning (Extra unused) — does NOT block generation
        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        Assert.Contains(result.AllDiagnostics, d =>
            d.Id == "FKF101" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void E2E_MultipleForgeClasses_AllGenerated()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }
                public class B    { public int Y { get; set; } }
                public class BDto { public int Y { get; set; } }

                [ForgeClass]
                public static partial class AForges
                {
                    public static partial ADto ToDto(A source);
                }

                [ForgeClass]
                public static partial class BForges
                {
                    public static partial BDto ToDto(B source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.Equal(2, result.RunResult.GeneratedTrees.Length);
    }

    [Fact]
    public void E2E_NestedForging_WithAllowNestedTrue_Works()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address    { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person     { public string Name { get; set; } = ""; public Address    Home { get; set; } = new(); }
                public class PersonDto  { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

                [ForgeClass]
                public static partial class PersonForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [Forge(AllowNestedForging = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        Assert.True(result.HasGeneratedSource);

        var combinedSource = string.Join("\n", result.RunResult.GeneratedTrees.Select(t => t.GetText().ToString()));
        Assert.Contains("ToAddressDto(source.Home)", combinedSource);
    }

    [Fact]
    public void E2E_ExplicitMode_UnmarkedMethodIgnored_NoGenerationForIt()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge]
                    public static partial Dest ToDestExplicit(Source source);

                    // No [Forge] — should be ignored, not generate, emit FKF002
                    public static partial Dest ToDestIgnored(Source source);
                }
            }
            """;

        var result = RunFull(source);

        // FKF002 warning for the ignored method
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF002");

        // Only ONE method should be generated
        var generated = string.Join("\n", result.RunResult.GeneratedTrees.Select(t => t.GetText().ToString()));
        Assert.Contains("ToDestExplicit(", generated);
        Assert.DoesNotContain("ToDestIgnored(", generated);
    }

    [Fact]
    public void E2E_ParameterizedConstructorSelection_CorrectArgs()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest
                {
                    public string Name { get; }
                    public int    Age  { get; }
                    public Dest(string name, int age) { Name = name; Age = age; }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.False(result.HasErrors);
        var generated = result.RunResult.GeneratedTrees[0].GetText().ToString();
        Assert.Contains("new Dest(source.Name, source.Age)", generated);
    }

    [Fact]
    public void E2E_ConstructorError_BlocksEntireClass()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    private Dest() { }    // No public constructor
                    public string Name { get; set; } = "";
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunFull(source);

        Assert.True(result.HasErrors);
        Assert.Contains(result.AllDiagnostics, d => d.Id == "FKF502");
        Assert.False(result.HasGeneratedSource);
    }
}
