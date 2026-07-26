using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for ForgePolicy tri-state behavior on ShareReference and IgnoreIfNull.
/// Verifies the inheritance chain: Member Explicit → Method Explicit → Global Default.
/// </summary>
public sealed class ForgePolicyTriStateTests : GeneratorTestBase
{
    // ─── ShareReference inheritance chain ───────────────────────────────────────

    [Fact]
    public void ShareReference_MethodExplicitFalse_EnforcesDeepCopy()
    {
        // Method-level explicit False should force deep-copy (not inherit default)
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest   { public List<int> Values { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = ForgePolicy.False)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Values = source.Values != null ? new List<int>(source.Values) : null", generated);
    }

    [Fact]
    public void ShareReference_MethodExplicitTrue_ForcesReferenceShare()
    {
        // Method-level explicit True should force reference-sharing
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest   { public List<int> Values { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Values = source.Values", generated);
        Assert.DoesNotContain("new List<int>(source.Values)", generated);
    }

    [Fact]
    public void ShareReference_MemberInheritMethodTrue_InheritsMethodValue()
    {
        // Member-level Inherit should inherit from method-level True
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Values", ShareReference = ForgePolicy.Inherit)]
                    public List<int> Values { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Should reference-share (inherited from method-level True)
        Assert.Contains("__result.Values = source.Values", generated);
        Assert.DoesNotContain("new List<int>(source.Values)", generated);
    }

    [Fact]
    public void ShareReference_MemberInheritMethodFalse_InheritsMethodValue()
    {
        // Member-level Inherit should inherit from method-level False
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Values", ShareReference = ForgePolicy.Inherit)]
                    public List<int> Values { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = ForgePolicy.False)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Should deep-copy (inherited from method-level False)
        Assert.Contains("__result.Values = source.Values != null ? new List<int>(source.Values) : null", generated);
    }

    [Fact]
    public void ShareReference_MemberExplicitOverridesMethod()
    {
        // Member-level explicit True should override method-level False
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); }
                public class Dest
                {
                    [ForgeMap("Values", ShareReference = ForgePolicy.True)]
                    public List<int> Values { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = ForgePolicy.False)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Should reference-share (member-level explicit True wins)
        Assert.Contains("__result.Values = source.Values", generated);
        Assert.DoesNotContain("new List<int>(source.Values)", generated);
    }

    // ─── IgnoreIfNull inheritance chain ────────────────────────────────────────

    [Fact]
    public void IgnoreIfNull_MethodExplicitTrue_WrapsAllAssignments()
    {
        // Method-level explicit True should wrap all assignments
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("if (source.Name != null)", generated);
    }

    [Fact]
    public void IgnoreIfNull_MethodExplicitFalse_NoNullChecks()
    {
        // Method-level explicit False should not wrap assignments
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.False)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.DoesNotContain("if (source.Name != null)", generated);
    }

    [Fact]
    public void IgnoreIfNull_MemberInheritMethodTrue_InheritsMethodValue()
    {
        // Member-level Inherit should inherit from method-level True
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest
                {
                    [ForgeMap("Name", IgnoreIfNull = ForgePolicy.Inherit)]
                    public string Name { get; set; } = "";
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Should have null check (inherited from method-level True)
        Assert.Contains("if (source.Name != null)", generated);
    }

    [Fact]
    public void IgnoreIfNull_MemberExplicitOverridesMethod()
    {
        // Member-level explicit False should override method-level True
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest
                {
                    [ForgeMap("Name", IgnoreIfNull = ForgePolicy.False)]
                    public string Name { get; set; } = "";
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Should NOT have null check (member-level explicit False wins)
        Assert.DoesNotContain("if (source.Name != null)", generated);
    }

    [Fact]
    public void IgnoreIfNull_MemberInheritBothInherit_UsesDefault()
    {
        // Both method-level and member-level Inherit → use default (no null checks)
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest
                {
                    [ForgeMap("Name", IgnoreIfNull = ForgePolicy.Inherit)]
                    public string Name { get; set; } = "";
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.Inherit)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // Should use default (no null check)
        Assert.DoesNotContain("if (source.Name != null)", generated);
    }
}
