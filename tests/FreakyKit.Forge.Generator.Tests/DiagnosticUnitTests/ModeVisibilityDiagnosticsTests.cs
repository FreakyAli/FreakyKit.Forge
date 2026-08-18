using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Mode & Visibility diagnostics (FKF001–FKF011).
/// </summary>
public sealed class ModeVisibilityDiagnosticsTests : DiagnosticsTestBase
{
    // ─── FKF001: Explicit mode activated ─────────────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF001_ExplicitModeSet_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF001");
        AssertDiagnosticWithSeverity(source, "FKF001", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF001_ImplicitModeDefault_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF001");
    }

    // ─── FKF002: Method ignored in explicit mode ────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF002_CandidateMethodWithoutForgeMethodInExplicitMode_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class Forges
                {
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF002", "ignored");
        AssertDiagnosticWithSeverity(source, "FKF002", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF002_CandidateWithForgeMethodInExplicitMode_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF002");
    }

    [Fact]
    public void FKF002_ImplicitModeWithoutAttribute_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF002");
    }

    // ─── FKF003: Forge class not static ──────────────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF003_ForgeAttributeOnNonStaticClass_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF003");
        AssertDiagnosticWithSeverity(source, "FKF003", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF003_StaticForgeClass_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF003");
    }

    // ─── FKF004: Forge class not partial ────────────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF004_ForgeAttributeOnNonPartialClass_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static class Forges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF004");
        AssertDiagnosticWithSeverity(source, "FKF004", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF004_PartialForgeClass_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF004");
    }

    // ─── FKF005: [Forge] on non-class type ──────────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF005_ForgeAttributeOnStruct_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial struct Forges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF005");
        AssertDiagnosticWithSeverity(source, "FKF005", DiagnosticSeverity.Error);
    }

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF005_ForgeAttributeOnInterface_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public partial interface IForges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF005");
    }

    [Fact]
    public void FKF005_ForgeAttributeOnClass_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF005");
    }

    // ─── FKF010: Private forge method ignored ────────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF010_PrivateForgeMethod_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    private static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF010");
        AssertDiagnosticWithSeverity(source, "FKF010", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF010_PublicForgeMethod_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF010");
    }

    [Fact]
    public void FKF010_PrivateMethodWithIncludePrivate_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest { public int Value { get; set; } }

                [Forge(ShouldIncludePrivate = true)]
                public static partial class Forges
                {
                    private static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF010");
    }

    // ─── FKF011: Private visibility enabled ──────────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF011_ShouldIncludePrivateTrue_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge(ShouldIncludePrivate = true)]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF011");
        AssertDiagnosticWithSeverity(source, "FKF011", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF011_ShouldIncludePrivateFalse_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge(ShouldIncludePrivate = false)]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF011");
    }

    [Fact]
    public void FKF011_DefaultIncludePrivate_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                [Forge]
                public static partial class Forges
                {
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF011");
    }
}
