using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF041 — Update destination has no settable members.
/// </summary>
public sealed class UpdateErrorAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public void FKF041_DestHasNoSettableMembers_EmitsError()
    {
        const string source = """
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
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF041");
        AssertDiagnosticSeverity(source, "FKF041", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF041_DestHasSettableMembers_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF041");
    }

    [Fact]
    public void FKF041_DestHasMixedMembers_NoError()
    {
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
                    public int Age { get; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        // At least one settable member (Name), so no FKF041
        AssertNotContainsDiagnostic(source, "FKF041");
    }

    [Fact]
    public void FKF041_CreateShape_NeverEmitted()
    {
        const string source = """
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
            """;

        // FKF041 only applies to update shape, not create shape
        AssertNotContainsDiagnostic(source, "FKF041");
    }
}
