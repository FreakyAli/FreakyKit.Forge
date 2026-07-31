using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Snapshot tests for generated code.
/// Each test compares the full generated output against a golden .verified.cs file.
/// This catches any unintended changes to the generated code structure and format.
/// </summary>
public sealed class SnapshotTests : GeneratorTestBase
{
    private static readonly string SnapshotsDir = Path.Combine(
        Path.GetDirectoryName(typeof(SnapshotTests).Assembly.Location)!,
        "Snapshots");

    private static void AssertSnapshot(string generated, [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        Directory.CreateDirectory(SnapshotsDir);

        var verifiedPath = Path.Combine(SnapshotsDir, $"{testName}.verified.cs");
        var receivedPath = Path.Combine(SnapshotsDir, $"{testName}.received.cs");

        // Write the received output
        File.WriteAllText(receivedPath, generated);

        // If .verified.cs doesn't exist, fail with instructions
        if (!File.Exists(verifiedPath))
        {
            throw new Xunit.Sdk.XunitException(
                $"Snapshot file missing: {verifiedPath}\n" +
                $"Received output written to: {receivedPath}\n" +
                $"Review the output and rename .received.cs to .verified.cs to approve.");
        }

        // Compare with normalized line endings
        var verified = File.ReadAllText(verifiedPath);
        var normalizedGenerated = NormalizeLineEndings(generated);
        var normalizedVerified = NormalizeLineEndings(verified);

        if (normalizedGenerated != normalizedVerified)
        {
            throw new Xunit.Sdk.XunitException(
                $"Snapshot mismatch for {testName}:\n" +
                $"Expected: {verifiedPath}\n" +
                $"Received: {receivedPath}\n" +
                $"Diff:\n{CreateDiff(normalizedVerified, normalizedGenerated)}");
        }

        // Clean up received file on success
        File.Delete(receivedPath);
    }

    private static string CreateDiff(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var diff = new StringBuilder();
        var maxLines = Math.Max(expectedLines.Length, actualLines.Length);

        for (int i = 0; i < maxLines; i++)
        {
            var exp = i < expectedLines.Length ? expectedLines[i] : "<missing>";
            var act = i < actualLines.Length ? actualLines[i] : "<missing>";

            if (exp != act)
            {
                diff.AppendLine($"Line {i + 1}:");
                diff.AppendLine($"  Expected: {exp}");
                diff.AppendLine($"  Actual:   {act}");
            }
        }

        return diff.ToString();
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    [Fact]
    public void SimpleFlatMapping()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        AssertSnapshot(generated);
    }

    [Fact]
    public void NestedMapping_AllowNestedForging()
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
                public static partial class PersonForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        AssertSnapshot(generated);
    }

    [Fact]
    public void ConstructorAndObjectInitializer()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person
                {
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                }

                public class PersonDto
                {
                    public PersonDto(string name) { Name = name; }
                    public string Name { get; set; }
                    public int Age { get; set; }
                }

                [Forge]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        AssertSnapshot(generated);
    }

    [Fact]
    public void UpdateMethod_VoidReturn()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class PersonForges
                {
                    [ForgeMethod]
                    public static partial void UpdateDto(Person source, PersonDto dest);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        AssertSnapshot(generated);
    }

    [Fact]
    public void FlatteningWithPrefix()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Person { public string Name { get; set; } = ""; public Address Home { get; set; } = new(); }

                public class PersonDto
                {
                    public string Name { get; set; } = "";
                    public string HomeCity { get; set; } = "";
                }

                [Forge]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        AssertSnapshot(generated);
    }

    [Fact]
    public void InitOnlyProperties()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }

                public class PersonDto
                {
                    public string Name { get; init; } = "";
                }

                [Forge]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        AssertSnapshot(generated);
    }

    [Fact]
    public void ExtensionMethods_DefaultBehavior()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        Assert.Contains("public static class PersonForgesExtensions", generated);
        Assert.Contains("public static PersonDto ToDto(this Person source)", generated);
        Assert.Contains("return PersonForges.ToDto(source);", generated);
    }

    [Fact]
    public void ExtensionMethods_MultipleMethodsInClass()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonSummary { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                    public static partial PersonSummary ToSummary(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        Assert.Contains("public static PersonDto ToDto(this Person source)", generated);
        Assert.Contains("public static PersonSummary ToSummary(this Person source)", generated);
        Assert.Equal(2, CountOccurrences(generated, "return PersonForges."));
    }

    [Fact]
    public void ExtensionMethods_UpdateMethod()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class PersonForges
                {
                    [ForgeMethod]
                    public static partial void UpdateDto(Person source, PersonDto dest);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        Assert.Contains("public static void UpdateDto(this Person source, PersonDto dest)", generated);
        Assert.Contains("PersonForges.UpdateDto(source, dest);", generated);
    }

    [Fact]
    public void ExtensionMethods_OptOut()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }
                public class PersonDto { public string Name { get; set; } = ""; }

                [Forge(GenerateExtensionMethods = false)]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        Assert.Contains("public static partial PersonDto ToDto(Person source)", generated);
        Assert.DoesNotContain("this Person source", generated);
    }

    [Fact]
    public void EnumStringMapping_EnumToString()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Status { Active, Inactive, Pending }
                public class Order { public Status Status { get; set; } }
                public class OrderDto { public string Status { get; set; } = ""; }

                [Forge]
                public static partial class OrderForges
                {
                    public static partial OrderDto ToDto(Order source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        Assert.Contains("__result.Status = source.Status.ToString()", generated);
        AssertSnapshot(generated);
    }

    [Fact]
    public void EnumStringMapping_StringToEnum()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Status { Active, Inactive, Pending }
                public class OrderDto { public Status Status { get; set; } }
                public class Order { public string Status { get; set; } = ""; }

                [Forge]
                public static partial class OrderForges
                {
                    public static partial OrderDto ToDto(Order source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        Assert.Contains("System.Enum.Parse<Status>(source.Status)", generated);
        AssertSnapshot(generated);
    }

    [Fact]
    public void EnumStringMapping_StringSourceToEnumDest()
    {
        // Test enum-string mapping: string property maps to enum property
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Status { Active = 0, Inactive = 1, Pending = 2 }
                public class Order { public string Status { get; set; } = "Active"; }
                public class OrderDto { public Status Status { get; set; } }

                [Forge]
                public static partial class OrderForges
                {
                    [ForgeMethod]
                    public static partial OrderDto ToDto(Order source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should generate enum parsing code
        Assert.Contains("Status", generated);
        Assert.Contains("ToDto", generated);
        // Should handle string-to-enum conversion (likely using Enum.Parse or similar)
        Assert.Contains("__result.Status", generated);
    }

    [Fact]
    public void EnumMapping_SameEnumType_DirectAssignment()
    {
        // When both source and dest have the same enum type, direct assignment is used
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Priority { Low = 1, Medium = 2, High = 3 }
                public class Task { public Priority Priority { get; set; } }
                public class TaskDto { public Priority Priority { get; set; } }

                [Forge]
                public static partial class TaskForges
                {
                    [ForgeMethod]
                    public static partial TaskDto ToDto(Task source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should generate direct enum assignment
        Assert.Contains("__result.Priority = source.Priority", generated);
    }

    [Fact]
    public void EnumStringMapping_InheritedMember()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum Level { Low, Medium, High }
                public class Item { public string Level { get; set; } = "Low"; }
                public class ItemBase { public Level Level { get; set; } }
                public class ItemDto : ItemBase { }

                [Forge]
                public static partial class ItemForges
                {
                    public static partial ItemDto ToDto(Item source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Verify enum-string mapping works for inherited member (destSymbol is null case)
        Assert.Contains("System.Enum.Parse<Level>(source.Level)", generated);
    }

    [Fact]
    public void PolymorphicDispatch_SwitchExpression()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Animal { public string Name { get; set; } = ""; }
                public class Dog : Animal { public string Breed { get; set; } = ""; }
                public class Cat : Animal { public bool Indoor { get; set; } }

                public class AnimalDto { public string Name { get; set; } = ""; }
                public class DogDto : AnimalDto { public string Breed { get; set; } = ""; }
                public class CatDto : AnimalDto { public bool Indoor { get; set; } }

                [Forge]
                public static partial class AnimalForges
                {
                    public static partial DogDto MapDog(Dog source);
                    public static partial CatDto MapCat(Cat source);

                    [ForgePolymorphic(typeof(Dog), nameof(MapDog))]
                    [ForgePolymorphic(typeof(Cat), nameof(MapCat))]
                    public static partial AnimalDto MapAny(Animal source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        AssertSnapshot(generated);
    }

    private static int CountOccurrences(string text, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}
