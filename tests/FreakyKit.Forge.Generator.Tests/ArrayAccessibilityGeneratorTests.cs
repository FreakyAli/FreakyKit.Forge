using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for array type accessibility validation in constructor parameters.
/// Arrays have DeclaredAccessibility.NotApplicable, so the element type's accessibility
/// is what matters for validation.
/// </summary>
public sealed class ArrayAccessibilityGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void ArrayConstructorParameter_PublicElementType_GeneratesSuccessfully()
    {
        // Test that array-element constructor parameters with public element types
        // are properly validated and generation succeeds without rejecting the array
        // because DeclaredAccessibility is NotApplicable.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int[] Values { get; set; } = System.Array.Empty<int>(); }
                public class Dest {
                    public int[] Values { get; set; }
                    public Dest(int[] values) { Values = values; }
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
        // Verify that the constructor is correctly called with the array parameter
        Assert.Contains("new Dest(source.Values", generated);
    }

    [Fact]
    public void ArrayConstructorParameter_WithMultipleParameters_GeneratesSuccessfully()
    {
        // Test array parameters alongside other parameters in constructors.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source {
                    public string Name { get; set; } = "";
                    public int[] Values { get; set; } = System.Array.Empty<int>();
                }
                public class Dest {
                    public string Name { get; set; }
                    public int[] Values { get; set; }
                    public Dest(string name, int[] values)
                    {
                        Name = name;
                        Values = values;
                    }
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
        // Verify that both parameters are passed to constructor
        Assert.Contains("new Dest(source.Name, source.Values", generated);
    }

    [Fact]
    public void ArrayConstructorParameter_StringArray_GeneratesSuccessfully()
    {
        // Test string[] in constructor parameters.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string[] Tags { get; set; } = System.Array.Empty<string>(); }
                public class Dest {
                    public string[] Tags { get; set; }
                    public Dest(string[] tags) { Tags = tags; }
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
        Assert.Contains("new Dest(source.Tags", generated);
    }
}
