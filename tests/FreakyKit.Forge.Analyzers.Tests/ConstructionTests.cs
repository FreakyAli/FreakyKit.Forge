using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF500, FKF501, FKF502 — Construction diagnostics.
/// </summary>
public sealed class ConstructionTests : AnalyzerTestBase
{
    // ─── FKF500: Constructor ambiguity ────────────────────────────────────────

    [Fact]
    public void FKF500_MultipleViableConstructors_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int    Age  { get; set; }
                    public Dest(string name) { Name = name; }
                    public Dest(int age)     { Age = age; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF500");
    }

    [Fact]
    public void FKF500_SingleViableConstructor_NoError() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; }
                    public Dest(string name) { Name = name; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF500");

    // ─── FKF501: Missing constructor parameter ────────────────────────────────

    [Fact]
    public void FKF501_SingleConstructorWithUnmatchedParam_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name  { get; }
                    public string Email { get; }
                    public Dest(string name, string email) { Name = name; Email = email; }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF501");
    }

    [Fact]
    public void FKF501_AllConstructorParamsSatisfied_NoError() =>
        AssertNotContainsDiagnostic("""
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
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF501");

    // ─── FKF502: No viable constructor ───────────────────────────────────────

    [Fact]
    public void FKF502_NoPublicConstructors_EmitsError()
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
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF502");
    }

    [Fact]
    public void FKF502_ParameterlessConstructorExists_NoError() =>
        AssertNotContainsDiagnostic("""
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public Dest() { }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """, "FKF502");

    [Fact]
    public void FKF502_MultipleCtors_NoneViable_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public Dest(int x)    { }
                    public Dest(double y) { }
                }
                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF502");
    }
}
