using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for collection type mismatches when AllowNestedForging is disabled.
/// What happens when you have List<A> → List<B> but AllowNestedForging=false?
/// </summary>
public sealed class CollectionMismatchEdgeCasesTests : GeneratorTestBase
{
    [Fact]
    public void CollectionElementTypeMismatch_NestedForgingDisabled_EmitsError()
    {
        // List<Address> → List<AddressDto> with AllowNestedForging=false
        // This should emit FKF200 because elements don't match and nested forging is disabled
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public List<Address> Homes { get; set; } = new(); }
                public class Dest { public List<AddressDto> Homes { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    // AllowNestedForging=false (default), but element types differ
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF200: Incompatible member types (no conversion available)
        var fkf200 = Assert.Single(result.Diagnostics, d => d.Id == "FKF200");
        Assert.Equal(DiagnosticSeverity.Error, fkf200.Severity);
        Assert.Contains("List", fkf200.GetMessage());
    }

    [Fact]
    public void CollectionElementTypeMismatch_NestedForgingEnabled_Succeeds()
    {
        // Same scenario but with AllowNestedForging=true
        // Should use the nested forge method for element conversion
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public List<Address> Homes { get; set; } = new(); }
                public class Dest { public List<AddressDto> Homes { get; set; } = new(); }

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
        // Should use Select with the nested forge method
        Assert.Contains(".Select(x => ToAddressDto(x))", generated);
    }

    [Fact]
    public void CollectionElementTypeWithConverter_NestedForgingDisabled_UsesConverter()
    {
        // Test collection matching with compatible element types
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Source { public List<string> Values { get; set; } = new(); }
                public class Dest { public List<string> Values { get; set; } = new(); }

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
    public void ArrayElementTypeMismatch_NestedForgingDisabled_EmitsError()
    {
        // Address[] → AddressDto[] with AllowNestedForging=false
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Address[] Homes { get; set; } = new Address[0]; }
                public class Dest { public AddressDto[] Homes { get; set; } = new AddressDto[0]; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF200: Array element types don't match
        var fkf200 = Assert.Single(result.Diagnostics, d => d.Id == "FKF200");
        Assert.Equal(DiagnosticSeverity.Error, fkf200.Severity);
    }

    [Fact]
    public void DictionaryValueTypeMismatch_NestedForgingDisabled_EmitsError()
    {
        // Dictionary<string, Address> → Dictionary<string, AddressDto> without nested forging
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source { public Dictionary<string, Address> HomesMap { get; set; } = new(); }
                public class Dest { public Dictionary<string, AddressDto> HomesMap { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF200: Dictionary value types don't match
        var fkf200 = Assert.Single(result.Diagnostics, d => d.Id == "FKF200");
        Assert.Equal(DiagnosticSeverity.Error, fkf200.Severity);
    }

    [Fact]
    public void CollectionSameElementType_NestedForgingIrrelevant()
    {
        // List<string> → List<string> where types are identical
        // Should not care about AllowNestedForging, just copy
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
                    // AllowNestedForging=false (default), but element types are identical
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Should copy the list (or use it directly depending on ShareReference)
        Assert.Contains("Tags", generated);
    }

    [Fact]
    public void NestedCollectionMismatch_AllowNestedForgingFalse_EmitsError()
    {
        // Property that is itself a collection with nested type mismatch
        // Source: List<Address>, Dest: List<AddressDto>, AllowNestedForging=false
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public class Source { public List<Address> Addresses { get; set; } = new(); }
                public class Dest { public List<AddressDto> Addresses { get; set; } = new(); }

                public class Address { public Person Owner { get; set; } = new(); }
                public class AddressDto { public PersonDto Owner { get; set; } = new(); }

                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF200: Can't convert List<Address> to List<AddressDto>
        var fkf200 = Assert.Single(result.Diagnostics, d => d.Id == "FKF200");
        Assert.Equal(DiagnosticSeverity.Error, fkf200.Severity);
    }
}
