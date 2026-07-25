using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for enum-to-enum mapping in the source generator.
/// Verifies that cast and by-name strategies produce the correct generated code.
/// </summary>
public sealed class EnumGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void EnumCast_GeneratesCastExpression()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public SourceStatus Status { get; set; } }
                public class Dest   { public DestStatus   Status { get; set; } }

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
        Assert.Contains("(DestStatus)source.Status", generated);
        Assert.DoesNotContain("source.Status switch", generated);
    }

    [Fact]
    public void EnumByName_GeneratesSwitchExpression()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public SourceStatus Status { get; set; } }
                public class Dest   { public DestStatus   Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("source.Status switch", generated);
        Assert.Contains("SourceStatus.Active => DestStatus.Active", generated);
        Assert.Contains("SourceStatus.Inactive => DestStatus.Inactive", generated);
        Assert.DoesNotContain("(DestStatus)source.Status", generated);
    }

    [Fact]
    public void EnumWithRegularProps_MixedMapping()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public string Name { get; set; } = ""; public SourceStatus Status { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public DestStatus   Status { get; set; } }

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
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("(DestStatus)source.Status", generated);
    }

    [Fact]
    public void FKF210_EnumCastMapping_EmitsDiagnostic()
    {
        // FKF210: Info diagnostic when enum cast mapping is applied
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public SourceStatus Status { get; set; } }
                public class Dest   { public DestStatus   Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        // FKF210 should be emitted for enum cast mapping
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF210");
    }

    [Fact]
    public void FKF211_EnumNameMapping_EmitsDiagnostic()
    {
        // FKF211: Info diagnostic when enum name-based mapping is applied
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus   { Active, Inactive }
                public class Source { public SourceStatus Status { get; set; } }
                public class Dest   { public DestStatus   Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        // FKF211 should be emitted for enum name-based mapping
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF211");
    }

    [Fact]
    public void FKF230_EnumStringMapping_EmitsDiagnostic()
    {
        // FKF230: Info diagnostic when enum ↔ string mapping is applied
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Status { Active, Inactive }
                public class Source { public Status Status { get; set; } }
                public class Dest   { public string Status { get; set; } = ""; }

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
        // FKF230 should be emitted for enum-string mapping
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF230");
        // Generated code should use ToString() for enum -> string
        Assert.Contains("source.Status.ToString()", generated);
    }

    [Fact]
    public void EnumStringMapping_StringToEnum_UsesEnumParse()
    {
        // Verify string -> enum uses Enum.Parse
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Status { Active, Inactive }
                public class Source { public string Status { get; set; } = ""; }
                public class Dest   { public Status Status { get; set; } }

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
        // Should use Enum.Parse for string -> enum
        Assert.Contains("Enum.Parse", generated);
    }
}
