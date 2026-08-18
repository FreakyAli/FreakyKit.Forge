using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Member Matching diagnostics (FKF100–FKF112, FKF530–FKF542).
/// Tests member discovery, matching, flattening, and [ForgeIncludes] behavior.
/// </summary>
public sealed class MemberMatchingDiagnosticsTests : DiagnosticsTestBase
{
    // ─── FKF100: Destination member missing source ───────────────────────────

    [Fact]
    public void FKF100_DestinationMemberNoSourceMatch_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            public class Source { public int X { get; set; } }
            public class Dest { public int X { get; set; } public int Unmatched { get; set; } }

            [Forge]
            public static partial class Forges
            {
                [ForgeMethod]
                public static partial Dest Map(Source s);
            }
            """;

        AssertDiagnosticEmitted(source, "FKF100");
        AssertDiagnosticWithSeverity(source, "FKF100", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF100_AllDestinationMembersMatched_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            public class Source { public int X { get; set; } public int Y { get; set; } }
            public class Dest { public int X { get; set; } public int Y { get; set; } }

            [Forge]
            public static partial class Forges
            {
                [ForgeMethod]
                public static partial Dest Map(Source s);
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF100");
    }

    // ─── FKF101: Source member unused ───────────────────────────────────────

    [Fact]
    public void FKF101_SourceMemberNoDestinationMatch_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            public class Source { public int X { get; set; } public int Extra { get; set; } }
            public class Dest { public int X { get; set; } }

            [Forge]
            public static partial class Forges
            {
                [ForgeMethod]
                public static partial Dest Map(Source s);
            }
            """;

        AssertDiagnosticEmitted(source, "FKF101");
        AssertDiagnosticWithSeverity(source, "FKF101", DiagnosticSeverity.Warning);
    }

    // ─── FKF104: [ForgeMap] target not found ────────────────────────────────

    [Fact]
    public void FKF104_ForgeMapTargetMissing_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest
                {
                    [ForgeMap("NonExistent")]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF104");
        AssertDiagnosticWithSeverity(source, "FKF104", DiagnosticSeverity.Error);
    }

    // ─── FKF105: Duplicate [ForgeMap] target ────────────────────────────────

    [Fact]
    public void FKF105_DuplicateForgeMapTarget_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest
                {
                    [ForgeMap("Name")]
                    public int First { get; set; }

                    [ForgeMap("Name")]
                    public int Second { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF105");
        AssertDiagnosticWithSeverity(source, "FKF105", DiagnosticSeverity.Warning);
    }

    // ─── FKF106: Flattened mapping applied ───────────────────────────────────

    [Fact]
    public void FKF106_FlatteningApplied_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source { public Address Address { get; set; } = new(); }
                public class Dest { public string AddressCity { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF106");
        AssertDiagnosticWithSeverity(source, "FKF106", DiagnosticSeverity.Info);
    }

    // ─── FKF107: Read-only destination member skipped ────────────────────────

    [Fact]
    public void FKF107_ReadOnlyDestinationMember_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest { public string Name { get; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF107");
        AssertDiagnosticWithSeverity(source, "FKF107", DiagnosticSeverity.Info);
    }

    // ─── FKF109: Both [ForgeIgnore] and [ForgeMap] ───────────────────────────

    [Fact]
    public void FKF109_BothIgnoreAndMap_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest
                {
                    [ForgeIgnore]
                    [ForgeMap("Other")]
                    public int Value { get; set; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF109");
        AssertDiagnosticWithSeverity(source, "FKF109", DiagnosticSeverity.Warning);
    }

    // ─── FKF110: Strict mode - destination member missing source ─────────────

    [Fact]
    public void FKF110_StrictModeDestinationUnmapped_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            public class Source { public int X { get; set; } }
            public class Dest { public int X { get; set; } public int Y { get; set; } }

            [Forge]
            public static partial class Forges
            {
                [ForgeMethod(StrictMapping = true)]
                public static partial Dest Map(Source s);
            }
            """;

        AssertDiagnosticEmitted(source, "FKF110");
        AssertDiagnosticWithSeverity(source, "FKF110", DiagnosticSeverity.Error);
    }

    // ─── FKF111: Strict mode - source member unused ────────────────────────

    [Fact]
    public void FKF111_StrictModeSourceUnused_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            public class Source { public int X { get; set; } public int Extra { get; set; } }
            public class Dest { public int X { get; set; } }

            [Forge]
            public static partial class Forges
            {
                [ForgeMethod(StrictMapping = true)]
                public static partial Dest Map(Source s);
            }
            """;

        AssertDiagnosticEmitted(source, "FKF111");
        AssertDiagnosticWithSeverity(source, "FKF111", DiagnosticSeverity.Error);
    }

    // ─── FKF112: [ForgeMap] maps to own name ────────────────────────────────

    [Fact]
    public void FKF112_ForgeMapToOwnName_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest
                {
                    [ForgeMap("Name")]
                    public string Name { get; set; } = "";
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF112");
        AssertDiagnosticWithSeverity(source, "FKF112", DiagnosticSeverity.Warning);
    }

    // ─── FKF530: Ambiguous flattening ───────────────────────────────────────

    [Fact]
    public void FKF530_AmbiguousFlattening_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Address { public class City { public string Name { get; set; } = ""; } public City CityObj { get; set; } = new(); }
                public class Source { public Address Address { get; set; } = new(); public string AddressCity { get; set; } = ""; }
                public class Dest { public string AddressCityName { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF530");
        AssertDiagnosticWithSeverity(source, "FKF530", DiagnosticSeverity.Error);
    }
}
