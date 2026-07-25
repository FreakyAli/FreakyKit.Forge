using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for dictionary mapping feature (dict↔object conversions).
/// Verifies that the generator correctly detects Dictionary<string, T> parameters
/// and generates appropriate mapping code with policy support.
/// </summary>
public sealed class DictionaryMappingTests : GeneratorTestBase
{
    [Fact]
    public void DictToObject_ExactKeyMatching_GeneratesTryGetValue()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Person FromDict(Dictionary<string, object> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should use TryGetValue for exact key matching
        Assert.Contains("TryGetValue(\"Name\"", generated);
        Assert.Contains("TryGetValue(\"Age\"", generated);
        Assert.Contains("__result.Name", generated);
        Assert.Contains("__result.Age", generated);
    }

    [Fact]
    public void ObjectToDict_BasicMapping_GeneratesDirectAssignment()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dictionary<string, object> ToDict(Person person);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should assign properties to dictionary with exact keys
        Assert.Contains("__result[\"Name\"]", generated);
        Assert.Contains("__result[\"Age\"]", generated);
        Assert.Contains("person.Name", generated);
        Assert.Contains("person.Age", generated);
    }

    [Fact]
    public void DictToObject_CamelCasePolicy_GeneratesFirstOrDefault()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string FirstName { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    [ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase)]
                    public static partial Person FromDict(Dictionary<string, object> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // CamelCase policy should use FirstOrDefault with OrdinalIgnoreCase
        Assert.Contains("FirstOrDefault", generated);
        Assert.Contains("StringComparison.OrdinalIgnoreCase", generated);
        Assert.Contains("\"firstName\"", generated);
    }

    [Fact]
    public void DictToObject_SnakeCasePolicy_GeneratesFirstOrDefault()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string FirstName { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    [ForgeDictionary(KeyCasing = KeyCasingPolicy.SnakeCase)]
                    public static partial Person FromDict(Dictionary<string, object> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // SnakeCase policy should be applied
        Assert.Contains("FirstOrDefault", generated);
        Assert.Contains("StringComparison.OrdinalIgnoreCase", generated);
        Assert.Contains("\"first_name\"", generated);
    }

    [Fact]
    public void DictToObject_IgnoreCasePolicy_GeneratesFirstOrDefault()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    [ForgeDictionary(KeyCasing = KeyCasingPolicy.IgnoreCase)]
                    public static partial Person FromDict(Dictionary<string, object> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // IgnoreCase should use FirstOrDefault with OrdinalIgnoreCase
        Assert.Contains("FirstOrDefault", generated);
        Assert.Contains("StringComparison.OrdinalIgnoreCase", generated);
    }

    [Fact]
    public void ObjectToDict_NullValueSkipPolicy_GeneratesNullCheck()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string? Name { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    [ForgeDictionary(NullValue = NullValuePolicy.Skip)]
                    public static partial Dictionary<string, object> ToDict(Person person);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Skip policy should only add non-null values
        Assert.Contains("!= null", generated);
        Assert.Contains("__val_Name", generated);
    }

    [Fact]
    public void ObjectToDict_NullValueIncludePolicy_NoNullCheck()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string? Name { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    [ForgeDictionary(NullValue = NullValuePolicy.Include)]
                    public static partial Dictionary<string, object> ToDict(Person person);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Include policy should add all values including nulls
        Assert.Contains("__result[\"Name\"] = person.Name;", generated);
    }

    [Fact]
    public void DictToObject_NonStringKeyType_EmitsFKF700()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Person FromDict(Dictionary<int, object> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        var fkf700 = result.Diagnostics.ToList()
            .Where(d => d.Id == "FKF700")
            .ToList();

        // Should emit FKF700 for non-string keys
        Assert.NotEmpty(fkf700);
    }

    [Fact]
    public void DictToObject_MultipleProperties_GeneratesForEach()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Address
                {
                    public string Street { get; set; } = "";
                    public string City { get; set; } = "";
                    public string ZipCode { get; set; } = "";
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Address FromDict(Dictionary<string, object> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should generate code for all three properties
        Assert.Contains("TryGetValue(\"Street\"", generated);
        Assert.Contains("TryGetValue(\"City\"", generated);
        Assert.Contains("TryGetValue(\"ZipCode\"", generated);
    }

    [Fact]
    public void DictToObject_NullCheck_BeforeProcessing()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Person? FromDict(Dictionary<string, object>? dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should check if dict is null before accessing
        Assert.Contains("== null", generated);
        Assert.Contains("return null;", generated);
    }

    [Fact]
    public void DictToObject_StringDict_WithIntParsing()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Person { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Person FromDict(Dictionary<string, string> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should generate int.Parse for Age
        Assert.Contains("int.Parse", generated);
        // Should handle Name as direct string assignment
        Assert.Contains("__val_Name", generated);
    }

    [Fact]
    public void DictToObject_StringDict_WithBoolParsing()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Settings { public bool IsEnabled { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Settings FromDict(Dictionary<string, string> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should generate bool.Parse
        Assert.Contains("bool.Parse", generated);
    }

    [Fact]
    public void DictToObject_StringDict_WithDoubleParsing()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Data { public double Value { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Data FromDict(Dictionary<string, string> dict);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Should generate double.Parse with InvariantCulture
        Assert.Contains("double.Parse", generated);
        Assert.Contains("CultureInfo.InvariantCulture", generated);
    }
}
