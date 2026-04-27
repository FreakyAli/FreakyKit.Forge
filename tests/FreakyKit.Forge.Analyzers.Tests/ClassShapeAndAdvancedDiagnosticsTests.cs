using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF003, FKF004, FKF043, FKF112, FKF222, FKF503.
/// Also covers the collection-projection and dictionary suppression paths for FKF042/FKF043.
/// </summary>
public sealed class ClassShapeAndAdvancedDiagnosticsTests : AnalyzerTestBase
{
    // ─── FKF003: Forge class not static ──────────────────────────────────────

    [Fact]
    public void FKF003_NonStaticForgeClass_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF003");
    }

    [Fact]
    public void FKF003_StaticForgeClass_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF003");
    }

    // ─── FKF004: Forge class not partial ─────────────────────────────────────

    [Fact]
    public void FKF004_NonPartialForgeClass_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                public static class MyForges
                {
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF004");
    }

    [Fact]
    public void FKF004_PartialForgeClass_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF004");
    }

    // ─── FKF043: AllowFlattening enabled but nothing flattened ───────────────

    [Fact]
    public void FKF043_FlatteningEnabledNoFlattenedMatch_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF043");
    }

    [Fact]
    public void FKF043_FlatteningEnabledWithFlattenedMatch_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public Address Address { get; set; } = new(); }
                public class Dest    { public string AddressCity { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF043");
    }

    [Fact]
    public void FKF043_FlatteningNotEnabled_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF043");
    }

    [Fact]
    public void FKF043_AllowFlatteningOnCollectionProjection_NoWarning()
    {
        // Collection projection methods skip FKF043 even if AllowFlattening = true and nothing was flattened.
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDto(Source source);
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial List<Dest> ToDtos(List<Source> sources);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF043");
    }

    // ─── FKF112: [ForgeMap] self-reference ───────────────────────────────────

    [Fact]
    public void FKF112_ForgeMapTargetSameAsPropertyName_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("Name")]
                    public string Name { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF112");
    }

    [Fact]
    public void FKF112_ForgeMapTargetDifferentName_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("FullName")]
                    public string Name { get; set; } = "";
                }
                public class Dest { public string FullName { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF112");
    }

    [Fact]
    public void FKF112_NoForgeMap_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF112");
    }

    [Fact]
    public void FKF112_CaseInsensitiveSelfReference_EmitsWarning()
    {
        // The check is OrdinalIgnoreCase — [ForgeMap("name")] on property Name should fire.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("name")]
                    public string Name { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF112");
    }

    [Fact]
    public void FKF112_FieldSelfReference_EmitsWarning()
    {
        // FKF112 applies to fields too, not just properties.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("firstName")]
                    public string firstName = "";
                }
                public class Dest { public string firstName { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF112");
    }

    // ─── FKF222: Duplicate converter for same type pair ──────────────────────

    [Fact]
    public void FKF222_DuplicateConverterForSameTypePair_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            using System;
            namespace TestNs
            {
                public class Source { public DateTime CreatedAt { get; set; } }
                public class Dest   { public string CreatedAt { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate1(DateTime value) => value.ToString("yyyy-MM-dd");

                    [ForgeConverter]
                    public static string ConvertDate2(DateTime value) => value.ToString("dd/MM/yyyy");
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF222");
    }

    [Fact]
    public void FKF222_SingleConverterForTypePair_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            using System;
            namespace TestNs
            {
                public class Source { public DateTime CreatedAt { get; set; } }
                public class Dest   { public string CreatedAt { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate(DateTime value) => value.ToString("yyyy-MM-dd");
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF222");
    }

    [Fact]
    public void FKF222_TwoConvertersDifferentTypePairs_NoWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            using System;
            namespace TestNs
            {
                public class Source { public DateTime CreatedAt { get; set; } public int Count { get; set; } }
                public class Dest   { public string CreatedAt { get; set; } = ""; public string Count { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate(DateTime value) => value.ToString("yyyy-MM-dd");

                    [ForgeConverter]
                    public static string ConvertInt(int value) => value.ToString();
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF222");
    }

    [Fact]
    public void FKF222_NullableAndNonNullableAreDifferentPairs_NoWarning()
    {
        // DateTime → string and DateTime? → string are different type pairs; no duplicate.
        const string source = """
            using FreakyKit.Forge;
            using System;
            namespace TestNs
            {
                public class Source { public DateTime? CreatedAt { get; set; } }
                public class Dest   { public string CreatedAt { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate(DateTime value) => value.ToString("yyyy-MM-dd");

                    [ForgeConverter]
                    public static string ConvertNullableDate(DateTime? value) => value?.ToString("yyyy-MM-dd") ?? "";
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF222");
    }

    [Fact]
    public void FKF222_GenericConverterNotCountedForDuplicateCheck_NoWarning()
    {
        // Generic converters are invalid (FKF221) and excluded from the FKF222 duplicate check.
        const string source = """
            using FreakyKit.Forge;
            using System;
            namespace TestNs
            {
                public class Source { public DateTime CreatedAt { get; set; } }
                public class Dest   { public string CreatedAt { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string ConvertDate(DateTime value) => value.ToString("yyyy-MM-dd");

                    [ForgeConverter]
                    public static string ConvertGeneric<T>(T value) => value!.ToString() ?? "";
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF222");
    }

    // ─── FKF503: Destination type not instantiable ───────────────────────────

    [Fact]
    public void FKF503_AbstractDestinationType_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public abstract class Dest { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF503");
    }

    [Fact]
    public void FKF503_InterfaceDestinationType_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public interface IDest { string Name { get; set; } }
                [Forge]
                public static partial class MyForges
                {
                    public static partial IDest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF503");
    }

    [Fact]
    public void FKF503_ConcreteDestinationType_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF503");
    }

    [Fact]
    public void FKF503_StaticClassDestinationType_EmitsError()
    {
        // Static classes are implicitly abstract+sealed in the CLR; IsAbstract=true, so FKF503 fires.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public static class Dest { }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF503");
    }
}
