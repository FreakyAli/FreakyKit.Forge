using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class CrossClassNestedForgeTests : GeneratorTestBase
{
    [Fact]
    public void ForgeUses_DiscoversMethodsFromIncludedClass()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

                [Forge]
                public static partial class AddressForges
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                [ForgeUses(typeof(AddressForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ForgeUses_UsesFirstMatchWhenMultipleClassesHaveSameMethod()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

                [Forge]
                public static partial class AddressForges1
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                public static partial class AddressForges2
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto2(Address source);
                }

                [Forge]
                [ForgeUses(typeof(AddressForges1), typeof(AddressForges2))]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }


    [Fact]
    public void ForgeUses_IncludedClassNotDecorated_EmitsFKF521()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

                public static partial class NotAForge
                {
                    public static partial AddressDto ToDto(Address source);
                }

                [Forge]
                [ForgeUses(typeof(NotAForge))]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF521");
    }

    [Fact]
    public void ForgeUses_SelfInclude_EmitsFKF522()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                [ForgeUses(typeof(PersonForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF522");
    }

    [Fact]
    public void ForgeUses_MultipleIncludes_DiscoversFromAll()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Company { public string Name { get; set; } = ""; }
                public class CompanyDto { public string Name { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); public Company Workplace { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); public CompanyDto Workplace { get; set; } = new(); }

                [Forge]
                public static partial class AddressForges
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                public static partial class CompanyForges
                {
                    [ForgeMethod]
                    public static partial CompanyDto ToCompanyDto(Company source);
                }

                [Forge]
                [ForgeUses(typeof(AddressForges), typeof(CompanyForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ForgeUses_ShadowedMethod_EmitsFKF523()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

                [Forge]
                public static partial class AddressForges1
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                public static partial class AddressForges2
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                [ForgeUses(typeof(AddressForges1), typeof(AddressForges2))]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF523");
    }

    [Fact]
    public void ForgeUses_WithoutAllowNestedForging_StillDiscoversMethods()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

                [Forge]
                public static partial class AddressForges
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                [ForgeUses(typeof(AddressForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ForgeUses_NoIncludedMethods_WorksWithoutNestedForging()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class UnusedForges
                {
                    [ForgeMethod]
                    public static partial PersonDto UnusedToDto(Person source);
                }

                [Forge]
                [ForgeUses(typeof(UnusedForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
    }

    [Fact]
    public void ForgeUses_ThreeClassesWithShadowing_UsesFirstMatch()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }
                public class PersonDto { public string Name { get; set; } = ""; public AddressDto Home { get; set; } = new(); }

                [Forge]
                public static partial class AddressForges1
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                public static partial class AddressForges2
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                public static partial class AddressForges3
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                [Forge]
                [ForgeUses(typeof(AddressForges1), typeof(AddressForges2), typeof(AddressForges3))]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToPersonDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        // Should have 2 FKF523 warnings (one for each shadowed method)
        var shadowingWarnings = result.Diagnostics.Where(d => d.Id == "FKF523").ToList();
        Assert.Equal(2, shadowingWarnings.Count);
    }

    [Fact]
    public void ForgeUses_MissingForgeAttribute_EmitsFKF524()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                [Forge]
                public static partial class AddressForges
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                // Missing [Forge] attribute — should emit FKF524
                [ForgeUses(typeof(AddressForges))]
                public static partial class PersonForges
                {
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Single(result.Diagnostics, d => d.Id == "FKF524");
    }

    [Fact]
    public void ForgeUses_WithForgeAttribute_NoFKF524Error()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                [Forge]
                public static partial class AddressForges
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                // Correct — has both [Forge] and [ForgeUses]
                [Forge]
                [ForgeUses(typeof(AddressForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        var hasFkf524 = result.Diagnostics.Any(d => d.Id == "FKF524");
        Assert.False(hasFkf524);
    }

    [Fact]
    public void ForgeUses_MultipleClassesMissingForge_EmitsFKF524ForEach()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }

                [Forge]
                public static partial class AddressForges
                {
                    [ForgeMethod]
                    public static partial AddressDto ToAddressDto(Address source);
                }

                // Missing [Forge] — should emit FKF524
                [ForgeUses(typeof(AddressForges))]
                public static partial class BadForges1
                {
                    public static partial AddressDto ToAddressDto(Address source);
                }

                // Missing [Forge] — should emit FKF524
                [ForgeUses(typeof(AddressForges))]
                public static partial class BadForges2
                {
                    public static partial AddressDto ToAddressDto(Address source);
                }
            }
            """;

        var result = RunGenerator(source);
        var fkf524Errors = result.Diagnostics.Where(d => d.Id == "FKF524").ToList();
        Assert.Equal(2, fkf524Errors.Count);
    }
}
