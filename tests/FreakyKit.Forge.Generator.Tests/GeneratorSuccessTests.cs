using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for successful code generation scenarios.
/// </summary>
public sealed class GeneratorSuccessTests : GeneratorTestBase
{
    [Fact]
    public void SimplePropertyMapping_GeneratesCorrectBody()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

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
        Assert.Contains("public static partial Dest ToDest(Source source)", generated);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Age = source.Age", generated);
        Assert.Contains("return __result", generated);
    }

    [Fact]
    public void ParameterlessConstructor_UsedForConstruction()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string X { get; set; } = ""; }
                public class Dest
                {
                    public string X { get; set; } = "";
                    public Dest() { }
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
        Assert.Contains("new Dest()", generated);
        Assert.DoesNotContain("new Dest(source", generated);
    }

    [Fact]
    public void ParameterizedConstructor_UsedWhenNoParameterless()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest
                {
                    public string Name { get; }
                    public int    Age  { get; }
                    public Dest(string name, int age) { Name = name; Age = age; }
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
        Assert.Contains("new Dest(source.Name, source.Age)", generated);
    }

    [Fact]
    public void NestedForging_AllowNestedTrue_InjectsNestedCall()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address    { public string City { get; set; } = ""; }
                public class AddressDto { public string City { get; set; } = ""; }
                public class Source     { public string Name { get; set; } = ""; public Address    Addr { get; set; } = new(); }
                public class Dest       { public string Name { get; set; } = ""; public AddressDto Addr { get; set; } = new(); }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial AddressDto ToAddressDto(Address source);

                    [Forge(AllowNestedForging = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        // Both methods are in the same forge class → one generated file
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("ToAddressDto(source.Addr)", generated);
    }

    [Fact]
    public void FieldOptIn_IncludesFieldsInGeneration()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Tag; public string Name { get; set; } = ""; }
                public class Dest   { public string Tag; public string Name { get; set; } = ""; }

                [ForgeClass]
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
        Assert.Contains("__result.Tag = source.Tag", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void ZeroOutput_WhenErrorDiagnosticExists()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int    Value { get; set; } }
                public class Dest   { public string Value { get; set; } = ""; }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF200");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void MultipleForgeClasses_EachGetsOwnFile()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }
                public class B    { public string Y { get; set; } = ""; }
                public class BDto { public string Y { get; set; } = ""; }

                [ForgeClass]
                public static partial class AForges
                {
                    public static partial ADto ToDto(A source);
                }

                [ForgeClass]
                public static partial class BForges
                {
                    public static partial BDto ToDto(B source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        AssertGeneratedFiles(result, 2);
    }

    [Fact]
    public void ReadOnlyProperty_SetViaConstructor_NotReassigned()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; }      // read-only, only settable via ctor
                    public Dest(string name) { Name = name; }
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
        // Name should appear only in constructor, not as an assignment
        Assert.Contains("new Dest(source.Name)", generated);
        Assert.DoesNotContain("__result.Name = ", generated);
    }

    [Fact]
    public void ImplicitMode_AllShapedMethodsGenerated()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A    { public string X { get; set; } = ""; }
                public class ADto { public string X { get; set; } = ""; }
                public class B    { public int Y { get; set; } }
                public class BDto { public int Y { get; set; } }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial ADto MapA(A source);
                    public static partial BDto MapB(B source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("MapA(", generated);
        Assert.Contains("MapB(", generated);
    }
}
