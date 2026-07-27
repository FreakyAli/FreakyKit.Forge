using System.Collections.Generic;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for dictionary mapping diagnostics (FKF701, FKF702).
/// </summary>
public sealed class DictionaryDiagnosticsTests : GeneratorTestBase
{
    [Fact]
    public void FKF702_ReturnNullOnNonNullable_EmitsError()
    {
        // FKF702: Error when MissingKeyPolicy.ReturnNull is used on non-nullable type
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Data
                {
                    public int Count { get; set; }
                    public string Name { get; set; } = "";
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeDictionary(MissingKey = MissingKeyPolicy.ReturnNull)]
                    public static partial Data FromDict(Dictionary<string, string> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        // Should emit FKF702 for non-nullable int Count
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF702");
    }

    [Fact]
    public void FKF702_NoError_WhenNullableType()
    {
        // No FKF702 when destination member is nullable
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Data
                {
                    public int? Count { get; set; }
                    public string? Name { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeDictionary(MissingKey = MissingKeyPolicy.ReturnNull)]
                    public static partial Data FromDict(Dictionary<string, string> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        // No FKF702 - all members are nullable
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF702");
    }

    [Fact]
    public void FKF702_NoError_WhenDifferentPolicy()
    {
        // No FKF702 when using different missing key policy
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Data
                {
                    public int Count { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeDictionary(MissingKey = MissingKeyPolicy.UseDefault)]
                    public static partial Data FromDict(Dictionary<string, string> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        // No FKF702 - policy is UseDefault, not ReturnNull
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF702");
    }

    [Fact]
    public void FKF701_UnsupportedDictionaryValueType_EmitsError()
    {
        // FKF701: Error when dictionary value type is not a supported type
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class Data { public Person Person { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Data FromDict(Dictionary<string, Person> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        // Should emit FKF701 for unsupported Person type
        AssertHasError(result, "FKF701");
    }

    [Fact]
    public void FKF701_NoError_ForSupportedTypes()
    {
        // No FKF701 for supported primitive types
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Data
                {
                    public int Count { get; set; }
                    public string Name { get; set; } = "";
                    public bool IsActive { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Data FromDict(Dictionary<string, object> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        // No FKF701 - all members are supported types
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF701");
    }
}
