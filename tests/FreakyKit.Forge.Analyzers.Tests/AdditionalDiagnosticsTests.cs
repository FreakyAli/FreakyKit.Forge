using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Analyzers.Tests;

/// <summary>
/// Tests for FKF102, FKF103, FKF104, FKF202, inherited members,
/// FQN attribute matching, and ForgeMap on constructor parameters.
/// </summary>
public sealed class AdditionalDiagnosticsTests : AnalyzerTestBase
{
    // ─── FKF102: Member ignored via [ForgeIgnore] ────────────────────────────

    [Fact]
    public void FKF102_ForgeIgnoreOnSourceMember_EmitsInfo()
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
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF102");
        AssertDiagnosticSeverity(source, "FKF102", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF102_ForgeIgnoreOnDestMember_EmitsInfo()
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
                    public int Computed { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF102");
        AssertDiagnosticSeverity(source, "FKF102", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF102_NoForgeIgnore_NoInfo()
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
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF102");
    }

    // ─── FKF103: Custom member mapping via [ForgeMap] ────────────────────────

    [Fact]
    public void FKF103_ForgeMapOnSourceMember_EmitsInfo()
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

        AssertContainsDiagnostic(source, "FKF103");
        AssertDiagnosticSeverity(source, "FKF103", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF103_ForgeMapOnDestMember_EmitsInfo()
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

        AssertContainsDiagnostic(source, "FKF103");
        AssertDiagnosticSeverity(source, "FKF103", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF103_NoForgeMap_NoInfo()
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
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF103");
    }

    // ─── FKF104: ForgeMap target not found ───────────────────────────────────

    [Fact]
    public void FKF104_ForgeMapTargetDoesNotExist_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("NonExistent")] public string FirstName { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF104");
        AssertDiagnosticSeverity(source, "FKF104", DiagnosticSeverity.Error);
    }

    [Fact]
    public void FKF104_ForgeMapTargetExists_NoError()
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

        AssertNotContainsDiagnostic(source, "FKF104");
    }

    [Fact]
    public void FKF104_ForgeMapTargetOnDestSide_DoesNotExistOnSource_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { [ForgeMap("DoesNotExist")] public string DisplayName { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF104");
    }

    // ─── FKF202: Nullable mapping applied (reference type) ───────────────────

    [Fact]
    public void FKF202_IntToNullableInt_EmitsInfo()
    {
        // int -> int? triggers FKF202 (nullable mapping applied) rather than FKF201
        // because the SOURCE is non-nullable and the DEST is nullable.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest   { public int? Value { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertContainsDiagnostic(source, "FKF202");
        AssertDiagnosticSeverity(source, "FKF202", DiagnosticSeverity.Info);
    }

    [Fact]
    public void FKF202_ExactSameType_NoInfo()
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
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF202");
    }

    [Fact]
    public void FKF202_ExactSameValueType_NoInfo()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Value { get; set; } }
                public class Dest   { public int Value { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF202");
    }

    // ─── Inherited members ──────────────────────────────────────────────────

    [Fact]
    public void InheritedMembers_SourceBaseClass_NoFalseWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseSource
                {
                    public string Name { get; set; } = "";
                }
                public class Source : BaseSource
                {
                    public int Age { get; set; }
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // Name is inherited from BaseSource — should still match Dest.Name
        AssertNotContainsDiagnostic(source, "FKF100");
        AssertNotContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void InheritedMembers_DestBaseClass_NoFalseWarnings()
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
                public class BaseDest
                {
                    public string Name { get; set; } = "";
                }
                public class Dest : BaseDest
                {
                    public int Age { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // Name is inherited from BaseDest — should still match Source.Name
        AssertNotContainsDiagnostic(source, "FKF100");
        AssertNotContainsDiagnostic(source, "FKF101");
    }

    [Fact]
    public void InheritedMembers_BothSidesInherited_NoFalseWarnings()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseSource { public string Id { get; set; } = ""; }
                public class Source : BaseSource { public string Name { get; set; } = ""; }

                public class BaseDest { public string Id { get; set; } = ""; }
                public class Dest : BaseDest { public string Name { get; set; } = ""; }

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

    // ─── FQN attribute matching ─────────────────────────────────────────────

    [Fact]
    public void FQN_DifferentNamespaceForgeAttribute_DoesNotTriggerAnalysis()
    {
        const string source = """
            using System;
            namespace OtherLib
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ForgeAttribute : Attribute { }
            }
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [OtherLib.Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // The analyzer should NOT activate because the [Forge] attribute is from
        // OtherLib, not FreakyKit.Forge. No diagnostics should be emitted.
        AssertNoDiagnostics(source);
    }

    [Fact]
    public void FQN_CorrectNamespaceForgeAttribute_TriggersAnalysis()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; public string Extra { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // The real FreakyKit.Forge.ForgeAttribute should trigger analysis
        AssertContainsDiagnostic(source, "FKF101");
    }

    // ─── ForgeMap on constructor parameters ──────────────────────────────────

    [Fact]
    public void ForgeMapOnCtorParam_MatchesSourceMember_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string FirstName { get; set; } = "";
                    public int Age { get; set; }
                }
                public class Dest
                {
                    public string Name { get; }
                    public int Age { get; }
                    public Dest([ForgeMap("FirstName")] string name, int age)
                    {
                        Name = name;
                        Age = age;
                    }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // The [ForgeMap("FirstName")] on the constructor param should map
        // param "name" to source member "FirstName" — no missing ctor param error
        AssertNotContainsDiagnostic(source, "FKF501");
        AssertNotContainsDiagnostic(source, "FKF502");
    }

    [Fact]
    public void ForgeMapOnCtorParam_TargetDoesNotExist_EmitsFKF501()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                }
                public class Dest
                {
                    public string Label { get; }
                    public Dest([ForgeMap("NonExistent")] string label)
                    {
                        Label = label;
                    }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // [ForgeMap("NonExistent")] points to a source member that doesn't exist,
        // so the constructor param can't be satisfied — FKF501 expected
        AssertContainsDiagnostic(source, "FKF501");
    }

    [Fact]
    public void ForgeMapOnCtorParam_WithoutForgeMap_FallsBackToParamName()
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
                    public string Name { get; }
                    public int Age { get; }
                    public Dest(string name, int age)
                    {
                        Name = name;
                        Age = age;
                    }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        // Without [ForgeMap], param names "name" and "age" match source members
        AssertNotContainsDiagnostic(source, "FKF501");
        AssertNotContainsDiagnostic(source, "FKF502");
    }

    // ─── FKF005: [Forge] on non-class type ──────────────────────────────────

    [Fact]
    public void FKF005_ForgeOnStruct_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                public partial struct MyForges;
            }
            """;

        AssertContainsDiagnostic(source, "FKF005");
    }

    [Fact]
    public void FKF005_ForgeOnInterface_EmitsError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                [Forge]
                public partial interface IMyForges { }
            }
            """;

        AssertContainsDiagnostic(source, "FKF005");
    }

    [Fact]
    public void FKF005_ForgeOnClass_NoError()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest Map(Source s);
                }
            }
            """;

        AssertNotContainsDiagnostic(source, "FKF005");
    }
}
