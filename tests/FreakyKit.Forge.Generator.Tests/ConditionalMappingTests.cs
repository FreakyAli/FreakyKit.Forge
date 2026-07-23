using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ConditionalMappingTests : GeneratorTestBase
{
    [Fact]
    public void Condition_CorrectSignature_Generates()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", Condition = nameof(IsPositive))]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);

                    internal static bool IsPositive(Source source) => source.Value > 0;
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Verify condition guards the assignment
        Assert.Contains("if (IsPositive(source))", generated);
        Assert.Contains("__result.Value = source.Value", generated);
    }

    [Fact]
    public void Condition_NotFound_EmitsFKF510()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    [ForgeMap("Name", Condition = "Missing")]
                    public string Name { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF510");
    }

    [Fact]
    public void Condition_InvalidSignature_EmitsFKF511()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    [ForgeMap("Name", Condition = nameof(ShouldMap))]
                    public string Name { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);

                    internal static bool ShouldMap() => true;
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF511");
    }

    [Fact]
    public void Condition_NotAccessible_EmitsFKF512()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", Condition = nameof(IsPositive))]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);

                    private static bool IsPositive(Source source) => source.Value > 0;
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF512");
    }

    [Fact]
    public void IgnoreIfDefault_WithoutCondition_Generates()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    [ForgeMap("Name", IgnoreIfDefault = true)]
                    public string Name { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void IgnoreIfDefault_WithCondition_Combined()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    [ForgeMap("Name", IgnoreIfDefault = true, Condition = nameof(IsValid))]
                    public string Name { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);

                    internal static bool IsValid(Source source) => !string.IsNullOrEmpty(source.Name);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void IgnoreIfDefault_OnInt_Generates()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Count { get; set; } }
                public class Dest
                {
                    [ForgeMap("Count", IgnoreIfDefault = true)]
                    public int Count { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void IgnoreIfDefault_OnNullableType_Generates()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", IgnoreIfDefault = true)]
                    public int? Value { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void Condition_WrongReturnType_StillValidatesSignature()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", Condition = nameof(GetValue))]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);

                    internal static int GetValue(Source source) => source.Value;
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF511");
    }

    [Fact]
    public void Condition_MultipleParameters_EmitsFKF511()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", Condition = nameof(Validate))]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDto(Source source);

                    internal static bool Validate(Source source, int extra) => source.Value > 0;
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF511");
    }
}
