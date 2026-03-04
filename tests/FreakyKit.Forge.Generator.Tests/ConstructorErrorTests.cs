using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for constructor error paths: FKF500, FKF501, FKF502.
/// Verifies the generator emits correct diagnostics and produces no generated source
/// when the destination type's constructor cannot be resolved.
/// </summary>
public sealed class ConstructorErrorTests : GeneratorTestBase
{
    [Fact]
    public void FKF502_PrivateConstructorOnly_NoViableConstructor()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    private Dest() { }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF502");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void FKF502_ConstructorParamsDoNotMatchSource_NoViableConstructor()
    {
        // Dest has two public constructors, neither of which is parameterless,
        // and neither has parameters matching the source members.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public Dest(int x, int y) { }
                    public Dest(double z) { }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF502");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void FKF500_AmbiguousConstructors_BothSatisfiable()
    {
        // Two constructors that are both fully satisfiable from source members.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                    public Dest(string name) { Name = name; }
                    public Dest(int age) { Age = age; }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF500");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void FKF501_SingleConstructor_ParameterNotInSource()
    {
        // Single public constructor with a parameter that has no matching source member.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; }
                    public int Code { get; }
                    public Dest(string name, int code) { Name = name; Code = code; }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF501");
        AssertNoGeneratedSource(result);
    }

    [Fact]
    public void FKF501_SingleConstructor_ParameterTypeMismatch()
    {
        // Single constructor where source has the member name but a different type.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Age { get; set; } = ""; }
                public class Dest
                {
                    public int Age { get; }
                    public Dest(int age) { Age = age; }
                }

                [ForgeClass]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF501");
        AssertNoGeneratedSource(result);
    }
}
