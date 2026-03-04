using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Cross-feature combination tests that verify multiple generator features
/// working together correctly in a single mapping scenario.
/// </summary>
public sealed class CrossFeatureGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void Update_WithCollectionProperty()
    {
        // Update mode (void return, 2 params) combined with collection mapping.
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public List<int> Values { get; set; } = new(); }
                public class Dest   { public string Name { get; set; } = ""; public int[] Values { get; set; } = System.Array.Empty<int>(); }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial void ApplyUpdate(Source source, Dest target);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Update mode: assigns to target parameter directly
        Assert.Contains("void ApplyUpdate(Source source, Dest target)", generated);
        Assert.Contains("target.Name = source.Name", generated);
        Assert.Contains("target.Values = source.Values.ToArray()", generated);
        // No construction or return in update mode
        Assert.DoesNotContain("var __result", generated);
        Assert.DoesNotContain("return ", generated);
    }

    [Fact]
    public void NullableEnum_SameType_UnwrapsValue()
    {
        // Nullable enum to non-nullable of the SAME enum type: uses .Value unwrap.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Status { Active, Inactive }
                public class Source { public Status? Status { get; set; } }
                public class Dest   { public Status  Status { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Status = source.Status.Value", generated);
    }

    [Fact]
    public void NonNullableEnum_ToNullable_SameType_DirectAssignment()
    {
        // Non-nullable enum to nullable of the SAME enum type: direct assignment.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Status { Active, Inactive }
                public class Source { public Status  Status { get; set; } }
                public class Dest   { public Status? Status { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Status = source.Status", generated);
        Assert.DoesNotContain(".Value", generated);
    }

    [Fact]
    public void ForgeIgnore_WithForgeMap_OnSameClass()
    {
        // One member is ignored via [ForgeIgnore], another uses [ForgeMap] for renaming.
        // Both should apply correctly: ignored member absent, mapped member renamed.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeIgnore]
                    public string Secret { get; set; } = "";
                    [ForgeMap("FullName")]
                    public string FirstName { get; set; } = "";
                    public int Age { get; set; }
                }
                public class Dest
                {
                    public string Secret { get; set; } = "";
                    public string FullName { get; set; } = "";
                    public int Age { get; set; }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // ForgeIgnore: Secret should NOT be assigned
        Assert.DoesNotContain("__result.Secret", generated);
        // ForgeMap: FirstName mapped to FullName
        Assert.Contains("__result.FullName = source.FirstName", generated);
        // Regular property
        Assert.Contains("__result.Age = source.Age", generated);
    }

    [Fact]
    public void Reverse_WithNullableProperty()
    {
        // Reverse mapping where source has a nullable int and dest has non-nullable int.
        // The forward direction should use .Value, and the reverse should do direct assignment.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; public int? Score { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int  Score { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(GenerateReverse = true, ReverseName = "FromDto")]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Forward: Person -> PersonDto (nullable int? -> int uses .Value)
        Assert.Contains("PersonDto ToDto(Person source)", generated);
        Assert.Contains("source.Score.Value", generated);
        // Reverse: PersonDto -> Person (int -> int? is direct assignment)
        Assert.Contains("Person FromDto(PersonDto source)", generated);
    }

    [Fact]
    public void CollectionWithNestedForge_AndRegularProps()
    {
        // Collection of complex types with nested forging, combined with regular property mapping.
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item    { public string Label { get; set; } = ""; }
                public class ItemDto { public string Label { get; set; } = ""; }
                public class Source  { public string Title { get; set; } = ""; public int Count { get; set; } public List<Item> Items { get; set; } = new(); }
                public class Dest    { public string Title { get; set; } = ""; public int Count { get; set; } public List<ItemDto> Items { get; set; } = new(); }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                    public static partial ItemDto ToItemDto(Item source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Regular properties
        Assert.Contains("__result.Title = source.Title", generated);
        Assert.Contains("__result.Count = source.Count", generated);
        // Collection with nested forge
        Assert.Contains("__result.Items = source.Items.Select(x => ToItemDto(x)).ToList()", generated);
    }

    [Fact]
    public void Converter_WithEnumCast_MixedMapping()
    {
        // One member uses a converter (DateTime -> string), another uses enum cast.
        const string source = """
            using System;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceKind { TypeA, TypeB }
                public enum DestKind   { TypeA, TypeB }
                public class Source { public DateTime Created { get; set; } public SourceKind Kind { get; set; } }
                public class Dest   { public string Created { get; set; } = ""; public DestKind Kind { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);

                    [ForgeConverter]
                    public static string DateToStr(DateTime value) => value.ToString("o");
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Converter used for DateTime -> string
        Assert.Contains("__result.Created = DateToStr(source.Created)", generated);
        // Enum cast for SourceKind -> DestKind
        Assert.Contains("(DestKind)source.Kind", generated);
    }

    [Fact]
    public void ExplicitMode_WithFieldsAndForgeMap()
    {
        // Explicit mode + IncludeFields + ForgeMap all on the same forge method.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Tag;
                    [ForgeMap("FullName")]
                    public string First { get; set; } = "";
                }
                public class Dest
                {
                    public string Tag;
                    public string FullName { get; set; } = "";
                }

                [ForgeClass(Mode = ForgeMode.Explicit)]
                public static partial class MyForges
                {
                    [Forge(IncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Field included because IncludeFields = true
        Assert.Contains("__result.Tag = source.Tag", generated);
        // ForgeMap applied: First -> FullName
        Assert.Contains("__result.FullName = source.First", generated);
    }

    [Fact]
    public void Flattening_WithNullableProps()
    {
        // Flattening combined with regular nullable property mapping.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source  { public int? Score { get; set; } public Address Address { get; set; } = new(); }
                public class Dest    { public int  Score { get; set; } public string AddressCity { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    [Forge(AllowFlattening = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);

        var generated = AssertSingleGeneratedFile(result);
        // Nullable unwrap
        Assert.Contains("__result.Score = source.Score.Value", generated);
        // Flattened mapping
        Assert.Contains("__result.AddressCity = source.Address.City", generated);
    }
}
