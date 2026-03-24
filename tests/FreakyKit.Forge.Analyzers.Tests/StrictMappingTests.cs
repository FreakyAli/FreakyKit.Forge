using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

public sealed class StrictMappingTests : AnalyzerTestBase
{
    [Fact]
    public void FKF110_StrictMapping_UnmappedDestMember_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(StrictMapping = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF110");
        AssertDiagnosticSeverity(source, "FKF110", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF111_StrictMapping_UnusedSourceMember_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(StrictMapping = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF111");
        AssertDiagnosticSeverity(source, "FKF111", DiagnosticSeverity.Error);
    }

    [Fact]
    public void StrictMapping_PerfectMatch_NoDriftErrors()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(StrictMapping = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF110");
        AssertNotContainsDiagnostic(source, "FKF111");
    }

    [Fact]
    public void StrictMapping_Off_UnmappedDestMember_EmitsWarningNotError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        // FKF100 (warning) should be emitted, not FKF110 (error)
        AssertContainsDiagnostic(source, "FKF100");
        AssertNotContainsDiagnostic(source, "FKF110");
    }

    [Fact]
    public void StrictMapping_Off_UnusedSourceMember_EmitsWarningNotError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        // FKF101 (warning) should be emitted, not FKF111 (error)
        AssertContainsDiagnostic(source, "FKF101");
        AssertNotContainsDiagnostic(source, "FKF111");
    }

    [Fact]
    public void StrictMapping_IgnoredMember_DoesNotTriggerDrift()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; [ForgeIgnore] public int InternalId { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(StrictMapping = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertNotContainsDiagnostic(source, "FKF111");
    }

    [Fact]
    public void StrictMapping_BothDrift_EmitsBothErrors()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public int Extra { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Missing { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(StrictMapping = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;
        AssertContainsDiagnostic(source, "FKF110");
        AssertContainsDiagnostic(source, "FKF111");
    }
}
