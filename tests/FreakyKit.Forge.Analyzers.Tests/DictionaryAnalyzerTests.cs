using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Analyzer-level tests for dictionary mapping scenarios.
/// Verifies that FKF042/FKF200 are correctly suppressed for dictionary
/// source/destination types, mirroring how collection projections are handled.
/// </summary>
public sealed class DictionaryAnalyzerTests : AnalyzerTestBase
{
    // ─── FKF042: dictionary projection method counts as zero members mapped ───

    [Fact]
    public void FKF042_DictionaryProjectionMethod_NoWarning()
    {
        // A method whose source param and return type are both Dictionary<K,V>
        // should not emit FKF042 — the generator handles it as a DictionaryProject.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    public static partial Dictionary<string, OrderDto> MapOrders(Dictionary<string, Order> source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF042");
    }

    [Fact]
    public void FKF042_DictionarySameValueType_NoWarning()
    {
        // Same K,V type on both sides — still a valid dictionary projection.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Item { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dictionary<string, Item> Copy(Dictionary<string, Item> source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF042");
    }

    // ─── FKF200: dictionary members with different value types ────────────────

    [Fact]
    public void FKF200_DictMember_DifferentValueTypes_WithNestedForging_NoError()
    {
        // Source.Items: Dictionary<string, Order>, Dest.Items: Dictionary<string, OrderDto>.
        // The analyzer must not emit FKF200 — the generator handles this via .ToDictionary().
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }
                public class Source   { public Dictionary<string, Order> Items { get; set; } = new(); }
                public class Dest     { public Dictionary<string, OrderDto> Items { get; set; } = new(); }
                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void FKF200_DictMember_DifferentValueTypes_NoNestedForging_NoAnalyzerError()
    {
        // Even without AllowNestedForging, the analyzer treats dict-to-dict as
        // "handled by generator" and does not emit FKF200. The generator itself
        // emits a diagnostic if the member can't be resolved at code-gen time.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }
                public class Source   { public Dictionary<string, Order> Items { get; set; } = new(); }
                public class Dest     { public Dictionary<string, OrderDto> Items { get; set; } = new(); }
                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    public static partial Dest ToDto(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void FKF200_IDictionaryMember_DifferentValueTypes_NoAnalyzerError()
    {
        // IDictionary<K, V> source member — also suppressed.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }
                public class Source   { public IDictionary<string, Order> Items { get; set; } = new Dictionary<string, Order>(); }
                public class Dest     { public IDictionary<string, OrderDto> Items { get; set; } = new Dictionary<string, OrderDto>(); }
                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void FKF200_IReadOnlyDictionaryMember_DifferentValueTypes_NoAnalyzerError()
    {
        // IReadOnlyDictionary<K, V> source member — also suppressed.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }
                public class Source   { public IReadOnlyDictionary<string, Order> Items { get; set; } = new Dictionary<string, Order>(); }
                public class Dest     { public IReadOnlyDictionary<string, OrderDto> Items { get; set; } = new Dictionary<string, OrderDto>(); }
                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDto(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    // ─── FKF043: AllowFlattening on a dictionary projection method ────────────

    [Fact]
    public void FKF043_AllowFlatteningOnDictionaryProjection_NoWarning()
    {
        // Dictionary projection methods skip FKF043 even if AllowFlattening = true.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Order    { public string Id { get; set; } = ""; }
                public class OrderDto { public string Id { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial OrderDto MapOrder(Order source);
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dictionary<string, OrderDto> MapOrders(Dictionary<string, Order> source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF043");
    }
}
