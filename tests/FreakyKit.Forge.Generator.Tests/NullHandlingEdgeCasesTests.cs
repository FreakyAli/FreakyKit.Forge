using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Edge case tests for null handling in mapping configuration.
/// Covers interactions between IgnoreIfNull, DefaultValue, and ShareReference.
/// </summary>
public sealed class NullHandlingEdgeCasesTests : GeneratorTestBase
{
    [Fact]
    public void IgnoreIfNull_MethodLevel_SkipsNullSourceMembers()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest { public string? Name { get; set; } }

                [Forge]
                public static partial class Mapper
                {
                    [ForgeMethod(IgnoreIfNull = true)]
                    public static partial Dest Map(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should wrap null assignment in if check
        Assert.Contains("if (source.Name != null)", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void DefaultValue_NullableToNonNullable_ProvidesJFallback()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int? Count { get; set; } }
                public class Dest { public int Count { get; set; } }

                [Forge]
                public static partial class Mapper
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should safely handle nullable to non-nullable
        Assert.Contains("source.Count", generated);
    }

    [Fact]
    public void ShareReference_SameTypeCollection_AssignsDirectly()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<string>? Tags { get; set; } }
                public class Dest { public List<string>? Tags { get; set; } }

                [Forge]
                public static partial class Mapper
                {
                    [ForgeMethod(ShareReference = true)]
                    public static partial Dest Map(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // ShareReference = true means assign reference directly, not copy
        Assert.Contains("__result.Tags = source.Tags", generated);
        // Should NOT contain copy constructor
        Assert.DoesNotContain("new List<string>(source.Tags)", generated);
    }

    [Fact]
    public void ShareReference_False_CopiesCollection()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<string> Tags { get; set; } = new(); }
                public class Dest { public List<string> Tags { get; set; } = new(); }

                [Forge]
                public static partial class Mapper
                {
                    [ForgeMethod(ShareReference = false)]
                    public static partial Dest Map(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // ShareReference = false (default) uses copy constructor
        Assert.Contains("new List<string>(source.Tags)", generated);
    }

    [Fact]
    public void IgnoreIfNull_WithNestedForging_GeneratesNullCheck()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Person { public Address? Address { get; set; } }

                public class AddressDto { public string City { get; set; } = ""; }
                public class PersonDto { public AddressDto? Address { get; set; } }

                [Forge]
                public static partial class Mapper
                {
                    [ForgeMethod(AllowNestedForging = true, IgnoreIfNull = true)]
                    public static partial PersonDto Map(Person source);

                    [ForgeMethod]
                    public static partial AddressDto MapAddress(Address address);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // IgnoreIfNull with nested forging should generate null-safe call
        Assert.Contains("source.Address != null ? MapAddress(source.Address) : null", generated);
    }

    [Fact]
    public void UpdateMethod_WithIgnoreIfNull_PreservesExistingValues()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class UpdateDto { public string? Name { get; set; } }
                public class User { public string Name { get; set; } = "default"; }

                [Forge]
                public static partial class Mapper
                {
                    [ForgeMethod(IgnoreIfNull = true)]
                    public static partial void UpdateUser(UpdateDto dto, User user);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Update method with IgnoreIfNull should skip assignment when source is null
        Assert.Contains("if (dto.Name != null)", generated);
        Assert.Contains("user.Name = dto.Name", generated);
    }

    [Fact]
    public void NullableSourceType_GeneratesSafeAccess()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest { public string? Name { get; set; } }

                [Forge]
                public static partial class Mapper
                {
                    public static partial Dest Map(Source? source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should handle nullable source parameter
        Assert.Contains("__result = new Dest()", generated);
    }
}
