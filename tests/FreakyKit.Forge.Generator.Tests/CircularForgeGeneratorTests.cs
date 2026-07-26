using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class CircularForgeGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void DirectCircle_SelfCall_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public Person Manager { get; set; } = new(); }
                public class PersonDto { public PersonDto Manager { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF301");
    }

    [Fact]
    public void TwoMethodCycle_ACallsBCallsA_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public Address Home { get; set; } = new(); }
                public class PersonDto { public AddressDto Home { get; set; } = new(); }
                public class Address { public Person Owner { get; set; } = new(); }
                public class AddressDto { public PersonDto Owner { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF301");
    }

    [Fact]
    public void ThreeMethodCycle_ACallsBCallsCCallsA_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public Address Home { get; set; } = new(); }
                public class PersonDto { public AddressDto Home { get; set; } = new(); }
                public class Address { public City City { get; set; } = new(); }
                public class AddressDto { public CityDto City { get; set; } = new(); }
                public class City { public Person Mayor { get; set; } = new(); }
                public class CityDto { public PersonDto Mayor { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial CityDto ToCityDto(City source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF301");
    }

    [Fact]
    public void NoCycle_LinearChain_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public Address Home { get; set; } = new(); }
                public class PersonDto { public AddressDto Home { get; set; } = new(); }
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        AssertSingleGeneratedFile(result);
    }

    [Fact]
    public void NoCycle_DisjointMethods_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        AssertSingleGeneratedFile(result);
    }

    [Fact]
    public void Cycle_WithMultipleNonCyclicMethods_DetectsCycleOnly()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public Address Home { get; set; } = new(); }
                public class PersonDto { public AddressDto Home { get; set; } = new(); }
                public class Address { public Person Owner { get; set; } = new(); }
                public class AddressDto { public PersonDto Owner { get; set; } = new(); }

                public class Company { public string Name { get; set; } = ""; }
                public class CompanyDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial CompanyDto ToCompanyDto(Company source);
                }
            }
            """;

        var result = RunGenerator(source);
        var circularDiags = result.Diagnostics.ToList().Where(d => d.Id == "FKF301").ToList();
        Assert.Single(circularDiags);
    }

    [Fact]
    public void CollectionCycle_ElementForgeMethod_EmitsError()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public List<Address> Addresses { get; set; } = new(); }
                public class PersonDto { public List<AddressDto> Addresses { get; set; } = new(); }
                public class Address { public List<Person> Owners { get; set; } = new(); }
                public class AddressDto { public List<PersonDto> Owners { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF301");
    }

    [Fact]
    public void NoNestedForging_NoCircleDetection()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public Address Home { get; set; } = new(); }
                public class PersonDto { public AddressDto Home { get; set; } = new(); }
                public class Address { public Person Owner { get; set; } = new(); }
                public class AddressDto { public PersonDto Owner { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(AllowNestedForging = false)]
                    public static partial PersonDto ToPersonDto(Person source);
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        // Should not detect a cycle since AllowNestedForging = false prevents nested forge edge discovery
        var circularDiags = result.Diagnostics.ToList().Where(d => d.Id == "FKF301").ToList();
        Assert.Empty(circularDiags);
    }

    // ─── FKF522: Circular ForgeUses (transitive cycles) ─────────────────────────

    [Fact]
    public void ForgeUses_DirectCycle_AUsesA_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                [ForgeUses(typeof(ForgesA))]
                public static partial class ForgesA { }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF522");
    }

    [Fact]
    public void ForgeUses_TwoClassCycle_AUsesBUsesA_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                [ForgeUses(typeof(ForgesB))]
                public static partial class ForgesA { }

                [Forge]
                [ForgeUses(typeof(ForgesA))]
                public static partial class ForgesB { }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF522");
    }

    [Fact]
    public void ForgeUses_ThreeClassCycle_AUsesBUsesCUsesA_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                [ForgeUses(typeof(ForgesB))]
                public static partial class ForgesA { }

                [Forge]
                [ForgeUses(typeof(ForgesC))]
                public static partial class ForgesB { }

                [Forge]
                [ForgeUses(typeof(ForgesA))]
                public static partial class ForgesC { }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF522");
    }

    [Fact]
    public void ForgeUses_LinearChain_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                [ForgeUses(typeof(ForgesB))]
                public static partial class ForgesA { }

                [Forge]
                [ForgeUses(typeof(ForgesC))]
                public static partial class ForgesB { }

                [Forge]
                public static partial class ForgesC { }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ForgeUses_MultipleIncludes_CycleDetected()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                [ForgeUses(typeof(ForgesB), typeof(ForgesC))]
                public static partial class ForgesA { }

                [Forge]
                public static partial class ForgesB { }

                [Forge]
                [ForgeUses(typeof(ForgesA))]
                public static partial class ForgesC { }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF522");
    }
}
