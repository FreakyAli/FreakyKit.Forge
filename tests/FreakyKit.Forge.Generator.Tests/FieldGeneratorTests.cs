using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for field mapping in the source generator.
/// Verifies that fields are skipped by default and included when IncludeFields = true on [Forge].
/// </summary>
public sealed class FieldGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Fields_SkippedByDefault()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Tag; public string Name { get; set; } = ""; }
                public class Dest   { public string Tag; public string Name { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);

        var generated = AssertSingleGeneratedFile(result);
        // Property Name should be mapped
        Assert.Contains("__result.Name = source.Name", generated);
        // Field Tag should NOT be mapped (fields excluded by default)
        Assert.DoesNotContain("__result.Tag", generated);
    }

    [Fact]
    public void Fields_IncludedWhenEnabled()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Tag; public string Name { get; set; } = ""; }
                public class Dest   { public string Tag; public string Name { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(IncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Tag = source.Tag", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void Fields_MixedFieldsAndProperties()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string FieldA;
                    public int FieldB;
                    public string PropC { get; set; } = "";
                }
                public class Dest
                {
                    public string FieldA;
                    public int FieldB;
                    public string PropC { get; set; } = "";
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(IncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.FieldA = source.FieldA", generated);
        Assert.Contains("__result.FieldB = source.FieldB", generated);
        Assert.Contains("__result.PropC = source.PropC", generated);
    }

    [Fact]
    public void Fields_OnlySourceHasField_DestHasProperty()
    {
        // Source has a field, dest has a property with the same name.
        // When IncludeFields = true, fields from source participate in matching.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Tag; }
                public class Dest   { public string Tag { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(IncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Tag = source.Tag", generated);
    }

    [Fact]
    public void Fields_SourcePropertyDestField_Included()
    {
        // Source has a property, dest has a field with the same name.
        // When IncludeFields = true, fields on dest are included.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Tag { get; set; } = ""; }
                public class Dest   { public string Tag; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(IncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Tag = source.Tag", generated);
    }
}
