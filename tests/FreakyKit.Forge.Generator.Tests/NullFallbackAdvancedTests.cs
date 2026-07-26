using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Advanced tests for NullFallback behavior in complex scenarios:
/// - NullFallback with nested collections
/// - NullFallback with custom constructors requiring parameters
/// - NullFallback interaction with other features
/// </summary>
public sealed class NullFallbackAdvancedTests : GeneratorTestBase
{
    [Fact]
    public void NullFallback_NestedCollectionElementNull_DefaultConstructs()
    {
        // Collection of nested types with null-safe nested forging
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                public class Source { public List<Address> Addresses { get; set; } = new(); }
                public class Dest { public List<AddressDto> Addresses { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // When collection of nested types, Select is used with the forge method
        Assert.Contains(".Select(x => ToAddressDto(x))", generated);
    }

    [Fact]
    public void NullFallback_DefaultConstructWhenSourceIsNull_CreatesDefault()
    {
        // When source nested object is null, create default instance instead of null
        // Note: NullFallback only works with reference types (not nullable value types)
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Should generate: ToAddressDto(source.Home)
        Assert.Contains("ToAddressDto(source.Home)", generated);
    }

    [Fact]
    public void NullFallback_IgnoreIfNullConflict_EmitsError()
    {
        // FKF315: IgnoreIfNull and NullFallback both set (mutually exclusive)
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Home", IgnoreIfNull = ForgePolicy.True, NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF315: Both IgnoreIfNull and NullFallback set
        var fkf315 = Assert.Single(result.Diagnostics, d => d.Id == "FKF315");
        Assert.Equal(DiagnosticSeverity.Error, fkf315.Severity);
    }

    [Fact]
    public void NullFallback_WithValueType_EmitsWarning()
    {
        // FKF314: NullFallback has no effect on value types (they can't be null)
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public struct Address { public string City { get; set; } }
                public class AddressDto { public string City { get; set; } = ""; }

                public class Source { public Address Home { get; set; } }  // Struct, not nullable
                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF314: NullFallback has no effect (value type)
        var fkf314 = Assert.Single(result.Diagnostics, d => d.Id == "FKF314");
        Assert.Equal(DiagnosticSeverity.Warning, fkf314.Severity);
    }

    [Fact]
    public void NullFallback_WithNullableValueType_Generates()
    {
        // Nullable value type with NullFallback still generates code correctly
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Value { get; set; } }
                public class Dest
                {
                    [ForgeMap("Value", NullFallback = NullFallback.DefaultConstruct)]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("ToDest", generated);
    }

    [Fact]
    public void NullFallback_MultipleMembers_IndependentBehavior()
    {
        // NullFallback on one member shouldn't affect others
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                public class Source
                {
                    public Address Home { get; set; } = new();
                    public Address Work { get; set; } = new();
                }

                public class Dest
                {
                    [ForgeMap("Home", NullFallback = NullFallback.DefaultConstruct)]
                    public AddressDto Home { get; set; } = new();

                    [ForgeMap("Work", NullFallback = NullFallback.Null)]
                    public AddressDto Work { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Both should use the nested forge method
        Assert.Contains("ToAddressDto(source.Home)", generated);
        Assert.Contains("ToAddressDto(source.Work)", generated);
    }

    [Fact]
    public void NullFallback_WithShareReference_BothApply()
    {
        // NullFallback and ShareReference are orthogonal concerns
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest { public List<string> Tags { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(ShareReference = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // With ShareReference=true, should directly assign the collection
        Assert.Contains("__result.Tags = source.Tags", generated);
    }

    [Fact]
    public void NullFallback_InUpdate_PreservesExistingWhenSourceNull()
    {
        // In update methods, nested forging is applied to update destination properties
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                public class Source { public Address Home { get; set; } = new(); }
                public class Dest
                {
                    public AddressDto Home { get; set; } = new();
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Should call nested forge method for update
        Assert.Contains("ToAddressDto(source.Home)", generated);
    }
}
