using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class NullableGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void NullableInt_ToInt_GeneratesDotValue()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Age { get; set; } }
                public class Dest   { public int  Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Age = source.Age.Value", generated);
    }

    [Fact]
    public void Int_ToNullableInt_DirectAssignment()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int  Age { get; set; } }
                public class Dest   { public int? Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Age = source.Age", generated);
        Assert.DoesNotContain("source.Age.Value", generated);
    }

    [Fact]
    public void NullableInt_InConstructor_GeneratesDotValue()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Age { get; set; } }
                public class Dest
                {
                    public int Age { get; }
                    public Dest(int age) { Age = age; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("new Dest(source.Age.Value)", generated);
    }

    [Fact]
    public void MixedNullable_WithRegularProps()
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

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Score = source.Score.Value", generated);
    }

    [Fact]
    public void NullableInt_WithDefaultValue_OnSource_GeneratesNullCoalescing()
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

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Age ?? 0", generated);
        Assert.DoesNotContain("source.Age.Value", generated);
    }

    [Fact]
    public void NullableInt_WithDefaultValue_OnDest_GeneratesNullCoalescing()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Score { get; set; } }
                public class Dest   { [ForgeMap("Score", DefaultValue = -1)] public int Score { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Score ?? -1", generated);
        Assert.DoesNotContain("source.Score.Value", generated);
    }

    [Fact]
    public void NullableInt_WithDefaultValue_InConstructor_GeneratesNullCoalescing()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Age", DefaultValue = 0)] public int? Age { get; set; } }
                public class Dest
                {
                    public int Age { get; }
                    public Dest(int age) { Age = age; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Age ?? 0", generated);
        Assert.DoesNotContain("source.Age.Value", generated);
    }

    [Fact]
    public void NullableInt_WithDefaultValue_SuppressesFKF201Warning()
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

        var result = RunGenerator(source);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF201");
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Age ?? 0", generated);
    }
}
