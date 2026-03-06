using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF200 (Type Safety) and FKF300 (Nested Forging) diagnostics.
/// </summary>
public sealed class TypeSafetyAndNestedTests : AnalyzerTestBase
{
    // ─── FKF200: Incompatible member types ───────────────────────────────────

    [Fact]
    public void FKF200_IncompatibleTypes_NoForgeMethod_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int    Value { get; set; } }
                public class Dest   { public string Value { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void FKF200_CompatibleTypes_NoError() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Value { get; set; } = ""; }
                public class Dest   { public string Value { get; set; } = ""; }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF200");

    // ─── FKF300: Nested forging disabled ─────────────────────────────────────

    [Fact]
    public void FKF300_NestedTypeForgeExists_AllowNestedFalse_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address    { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source     { public Address    Addr { get; set; } = new(); }
                public class Dest       { public AddressDto Addr { get; set; } = new(); }
                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF300");
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void FKF300_NestedTypeForgeExists_AllowNestedTrue_NoWarning() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address    { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source     { public Address    Addr { get; set; } = new(); }
                public class Dest       { public AddressDto Addr { get; set; } = new(); }
                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF300");

    [Fact]
    public void FKF200_NestedTypeNoForgeMethod_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address    { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source     { public Address    Addr { get; set; } = new(); }
                public class Dest       { public AddressDto Addr { get; set; } = new(); }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF200");
        AssertNotContainsDiagnostic(source, "FKF300");
    }
}
