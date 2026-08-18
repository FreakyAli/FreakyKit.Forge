using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.DiagnosticUnitTests;

/// <summary>
/// Unit tests for Method Shape diagnostics (FKF020–FKF051).
/// Tests forge method shape validation, update mode, and hooks.
/// </summary>
public sealed class MethodShapeDiagnosticsTests : DiagnosticsTestBase
{
    // ─── FKF020: Forge method declares a body ───────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF020_ForgeMethodWithBody_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s)
                    {
                        return new Dest();
                    }
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF020");
        AssertDiagnosticWithSeverity(source, "FKF020", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF020_PartialDeclarationWithoutBody_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF020");
    }

    // ─── FKF030: Forge method name overloaded ───────────────────────────────

    [Fact]
    public void FKF030_DuplicateMethodNames_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source1 { }
                public class Source2 { }
                public class Dest1 { }
                public class Dest2 { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest1 Map(Source1 s);

                    [ForgeMethod]
                    public static partial Dest2 Map(Source2 s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF030");
        AssertDiagnosticWithSeverity(source, "FKF030", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF030_UniqueMethodNames_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source1 { }
                public class Source2 { }
                public class Dest1 { }
                public class Dest2 { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest1 MapA(Source1 s);

                    [ForgeMethod]
                    public static partial Dest2 MapB(Source2 s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF030");
    }

    // ─── FKF040: Update mode activated ──────────────────────────────────────

    [Fact]
    public void FKF040_UpdateMethodWithVoidReturn_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial void Update(Source s, Dest d);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF040");
        AssertDiagnosticWithSeverity(source, "FKF040", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF040_CreateMethodWithReturnType_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF040");
    }

    // ─── FKF041: Update destination has no settable members ──────────────────

    [Fact]
    public void FKF041_UpdateWithNoSettableMembers_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }

                public class Dest
                {
                    public string Value { get; }
                }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial void Update(Source s, Dest d);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF041");
        AssertDiagnosticWithSeverity(source, "FKF041", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF041_UpdateWithSettableMembers_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public string Value { get; set; } = ""; }
                public class Dest { public string Value { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial void Update(Source s, Dest d);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF041");
    }

    // ─── FKF042: Zero members mapped ────────────────────────────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF042_NoMatchingMembers_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { public int X { get; set; } }
                public class Dest { public int Y { get; set; } }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF042");
        AssertDiagnosticWithSeverity(source, "FKF042", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF042_WithMatchingMembers_DoesNotEmit()
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
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF042");
    }

    // ─── FKF043: Flattening enabled but no members flattened ────────────────

    [Fact(Skip = "Requires full compilation context; better tested via integration tests")]
    public void FKF043_FlatteningEnabledButNoFlattenedMembers_EmitsWarning()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source { public Address Address { get; set; } = new(); }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(AllowFlattening = true)]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF043");
        AssertDiagnosticWithSeverity(source, "FKF043", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF043_FlatteningWithFlattenedMembers_DoesNotEmit()
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

        AssertDiagnosticNotEmitted(source, "FKF043");
    }

    // ─── FKF050: Before hook detected ───────────────────────────────────────

    [Fact]
    public void FKF050_BeforeHookExists_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);

                    static partial void OnBeforeMap(Source s);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF050");
        AssertDiagnosticWithSeverity(source, "FKF050", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF050_NoBeforeHook_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF050");
    }

    // ─── FKF051: After hook detected ────────────────────────────────────────

    [Fact]
    public void FKF051_AfterHookExists_EmitsInfo()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);

                    static partial void OnAfterMap(Source s, Dest result);
                }
            }
            """;

        AssertDiagnosticEmitted(source, "FKF051");
        AssertDiagnosticWithSeverity(source, "FKF051", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF051_NoAfterHook_DoesNotEmit()
    {
        const string source = """
            using FreakyKit.Forge;

            namespace TestNs
            {
                public class Source { }
                public class Dest { }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod]
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertDiagnosticNotEmitted(source, "FKF051");
    }
}
