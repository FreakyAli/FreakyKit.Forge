using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class ForgeMapTests : AnalyzerTestBase
{
    [Fact]
    public void ForgeMap_SourceSide_NoWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name")] public string FirstName { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF100");
        AssertNotContainsDiagnostic(source, "FKF101");
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void ForgeMap_DestSide_NoWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string FirstName { get; set; } = ""; }
                public class Dest   { [ForgeMap("FirstName")] public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF100");
        AssertNotContainsDiagnostic(source, "FKF101");
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void ForgeMap_BothSides_MeetInMiddle()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("CommonKey")] public string SrcName { get; set; } = ""; }
                public class Dest   { [ForgeMap("CommonKey")] public string DstName { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF100");
        AssertNotContainsDiagnostic(source, "FKF101");
        AssertNotContainsDiagnostic(source, "FKF200");
    }

    [Fact]
    public void ForgeMap_DuplicateTarget_EmitsFKF105()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("Name")] public string First { get; set; } = "";
                    [ForgeMap("Name")] public string Last { get; set; } = "";
                }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF105");
    }

    [Fact]
    public void ForgeMap_WithTypeMismatch_EmitsFKF200()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name")] public int Count { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF200");
    }
}
