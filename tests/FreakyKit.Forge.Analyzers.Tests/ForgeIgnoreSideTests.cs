using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class ForgeIgnoreSideTests : AnalyzerTestBase
{
    [Fact]
    public void ForgeIgnore_Side_Source_SuppressesFKF101_NotFKF100()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore(Side = ForgeIgnoreSide.Source)]
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

        // Source side ignored → no FKF101 for InternalId
        AssertNotContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void ForgeIgnore_Side_Source_DoesNotSuppressFKF100()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    // Not ignored on dest side — will emit FKF100 since there's no source match
                    public int Unmatched { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // FKF100 should still fire for Unmatched on dest
        AssertContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void ForgeIgnore_Side_Destination_SuppressesFKF100_NotFKF101()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore(Side = ForgeIgnoreSide.Destination)]
                    public int ComputedScore { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // Dest side ignored → no FKF100 for ComputedScore
        AssertNotContainsDiagnostic(source, "FKF100");
    }

    [Fact]
    public void ForgeIgnore_Side_Destination_DoesNotSuppressFKF101()
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

        // Extra on source has no dest match → FKF101 should fire
        AssertContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void ForgeIgnore_Side_Both_SuppressesBothWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore(Side = ForgeIgnoreSide.Both)]
                    public string Secret { get; set; } = "";
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    [ForgeIgnore(Side = ForgeIgnoreSide.Both)]
                    public string InternalId { get; set; } = "";
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF101");
        AssertNotContainsDiagnostic(source, "FKF100");
    }
}
