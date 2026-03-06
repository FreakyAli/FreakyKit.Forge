using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class ForgeIgnoreTests : AnalyzerTestBase
{
    [Fact]
    public void ForgeIgnore_OnSource_SuppressesFKF101()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public string InternalId { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void ForgeIgnore_OnDest_SuppressesFKF100()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public int Score { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void ForgeIgnore_OnBothSides_NoWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public string Secret { get; set; } = "";
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore]
                    public int Computed { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF100");
        AssertNotContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void ForgeIgnore_WithFields_FieldAlsoIgnored()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name = "";
                    [ForgeIgnore]
                    public string Secret = "";
                }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(ShouldIncludeFields = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // Secret field should not produce FKF101
        AssertNotContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void ForgeIgnore_Absent_StillEmitsWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public string Extra { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // Without ForgeIgnore, FKF101 should still be emitted for Extra
        AssertContainsDiagnostic(source, "FKF101");
    }
}
