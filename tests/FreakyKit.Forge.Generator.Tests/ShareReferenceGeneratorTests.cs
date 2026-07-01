using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for the ShareReference feature: same-type mutable collections deep-copy by default,
/// can be opted out of via method-level or per-member flags. FKF311/312/313 diagnostics.
/// </summary>
public sealed class ShareReferenceGeneratorTests : GeneratorTestBase
{
    // ─── Default behavior: deep-copy same-type mutable collections ───────────

    [Fact]
    public void Default_SameTypeList_DeepCopiesViaConstructor()
    {
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
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Values = source.Values != null ? new List<int>(source.Values) : null", generated);
    }

    [Fact]
    public void Default_SameTypeDictionary_DeepCopiesViaConstructor()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public Dictionary<string, int> Scores { get; set; } = new(); }
                public class Dest   { public Dictionary<string, int> Scores { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Scores = source.Scores != null ? new Dictionary<string, int>(source.Scores) : null", generated);
    }

    [Fact]
    public void Default_SameTypeHashSet_DeepCopiesViaConstructor()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public HashSet<int> Ids { get; set; } = new(); }
                public class Dest   { public HashSet<int> Ids { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Ids = source.Ids != null ? new HashSet<int>(source.Ids) : null", generated);
    }

    [Fact]
    public void Default_SameTypeArray_CopiesViaToArray()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int[] Codes { get; set; } = System.Array.Empty<int>(); }
                public class Dest   { public int[] Codes { get; set; } = System.Array.Empty<int>(); }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Codes = source.Codes != null ? source.Codes.ToArray() : null", generated);
    }

    [Fact]
    public void Default_SameTypeImmutableArray_StillReferenceShared()
    {
        // ImmutableArray is immutable — sharing the reference is safe. No copy.
        const string source = """
            using System.Collections.Immutable;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public ImmutableArray<int> Codes { get; set; } }
                public class Dest   { public ImmutableArray<int> Codes { get; set; } }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Codes = source.Codes;", generated);
    }

    [Fact]
    public void Default_SameTypePrimitive_StillDirectAssignment()
    {
        // int → int, no semantic difference between copy and ref since it's a value type
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public int Age { get; set; } }
                public class Dest   { public int Age { get; set; } }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Age = source.Age;", generated);
    }

    [Fact]
    public void Default_SameTypeString_StillDirectAssignment()
    {
        // strings are immutable; ref sharing is fine
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string Name { get; set; } = ""; }
                public class Dest   { public string Name { get; set; } = ""; }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Name = source.Name;", generated);
    }

    // ─── Method-level opt-out: ShareReference = true on [ForgeMethod] ────────

    [Fact]
    public void MethodLevel_ShareReferenceTrue_AllCollectionsSharedAndFKF311Emitted()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> Values { get; set; } = new(); public Dictionary<string, int> Map { get; set; } = new(); }
                public class Dest   { public List<int> Values { get; set; } = new(); public Dictionary<string, int> Map { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Values = source.Values;", generated);
        Assert.Contains("__result.Map = source.Map;", generated);
        Assert.DoesNotContain("new List<int>(source.Values)", generated);
        Assert.DoesNotContain("new Dictionary<string, int>(source.Map)", generated);
        // FKF311 fires once per shared member
        Assert.Equal(2, result.Diagnostics.Count(d => d.Id == "FKF311"));
    }

    // ─── Per-member override on destination ──────────────────────────────────

    [Fact]
    public void PerMember_DestSide_ShareReferenceFalse_OverridesMethodLevelTrue()
    {
        // Method says share all; one dest member explicitly opts back into copy.
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> A { get; set; } = new(); public List<int> B { get; set; } = new(); }
                public class Dest
                {
                    public List<int> A { get; set; } = new();
                    [ForgeMap("B", ShareReference = false)]
                    public List<int> B { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        // A inherits method-level share=true → ref-share
        Assert.Contains("__result.A = source.A;", generated);
        // B explicit per-member false → deep-copy
        Assert.Contains("__result.B = source.B != null ? new List<int>(source.B) : null", generated);
    }

    [Fact]
    public void PerMember_SourceSide_ShareReferenceFalse_OverridesMethodLevelTrue()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public List<int> A { get; set; } = new();
                    [ForgeMap("B", ShareReference = false)]
                    public List<int> B { get; set; } = new();
                }
                public class Dest { public List<int> A { get; set; } = new(); public List<int> B { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(ShareReference = true)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.A = source.A;", generated);
        Assert.Contains("__result.B = source.B != null ? new List<int>(source.B) : null", generated);
    }

    [Fact]
    public void PerMember_DestSide_ShareReferenceTrue_OverridesMethodDefaultFalse()
    {
        // Method doesn't set flag (default copy); one dest member opts into ref-share.
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public List<int> A { get; set; } = new(); public List<int> B { get; set; } = new(); }
                public class Dest
                {
                    public List<int> A { get; set; } = new();
                    [ForgeMap("B", ShareReference = true)]
                    public List<int> B { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        // A copies (default)
        Assert.Contains("__result.A = source.A != null ? new List<int>(source.A) : null", generated);
        // B explicit per-member true → ref-share
        Assert.Contains("__result.B = source.B;", generated);
        // FKF311 fires for B only
        Assert.Equal(1, result.Diagnostics.Count(d => d.Id == "FKF311"));
    }

    // ─── Conflict detection: FKF313 ─────────────────────────────────────────

    [Fact]
    public void Conflict_SrcTrueDestFalse_DestWins_FKF313Warning()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("Tags", ShareReference = true)]
                    public List<string> Tags { get; set; } = new();
                }
                public class Dest
                {
                    [ForgeMap("Tags", ShareReference = false)]
                    public List<string> Tags { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF313");
        var generated = AssertSingleGeneratedFile(result);
        // Destination wins (false) → deep copy
        Assert.Contains("__result.Tags = source.Tags != null ? new List<string>(source.Tags) : null", generated);
    }

    [Fact]
    public void Conflict_SrcFalseDestTrue_DestWins_FKF313Warning()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("Tags", ShareReference = false)]
                    public List<string> Tags { get; set; } = new();
                }
                public class Dest
                {
                    [ForgeMap("Tags", ShareReference = true)]
                    public List<string> Tags { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF313");
        var generated = AssertSingleGeneratedFile(result);
        // Destination wins (true) → ref-share
        Assert.Contains("__result.Tags = source.Tags;", generated);
    }

    [Fact]
    public void NoConflict_BothSidesAgree_NoFKF313()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    [ForgeMap("Tags", ShareReference = true)]
                    public List<string> Tags { get; set; } = new();
                }
                public class Dest
                {
                    [ForgeMap("Tags", ShareReference = true)]
                    public List<string> Tags { get; set; } = new();
                }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF313");
    }

    // ─── FKF312: same-type custom class reference-shared ────────────────────

    [Fact]
    public void SameTypeCustomClass_AlwaysReferenceShared_FKF312Info()
    {
        // Address Home on both sides → ref-share by default. Forge doesn't auto-clone custom
        // classes; users need a distinct DTO type + AllowNestedForging. FKF312 informs them.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Address { public string City { get; set; } = ""; }
                public class Source { public Address Home { get; set; } = new(); }
                public class Dest   { public Address Home { get; set; } = new(); }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Home = source.Home;", generated);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF312");
    }

    // ─── Interface-typed destinations ────────────────────────────────────────

    [Fact]
    public void Default_SameTypeIList_CopiesToList()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public IList<int> Values { get; set; } = new List<int>(); }
                public class Dest   { public IList<int> Values { get; set; } = new List<int>(); }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Values = source.Values != null ? new List<int>(source.Values) : null", generated);
    }

    [Fact]
    public void Default_SameTypeIReadOnlyList_CopiesToList()
    {
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public IReadOnlyList<int> Values { get; set; } = new List<int>(); }
                public class Dest   { public IReadOnlyList<int> Values { get; set; } = new List<int>(); }
                [Forge]
                public static partial class F
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("__result.Values = source.Values != null ? new List<int>(source.Values) : null", generated);
    }
}
