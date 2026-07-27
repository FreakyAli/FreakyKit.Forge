using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for expression nesting depth diagnostics (FKF508/509).
/// FKF508/509 are triggered when an expression property inlines expression bodies from
/// other forge methods recursively, creating deep nesting.
/// </summary>
public sealed class ExpressionNestingTests : GeneratorTestBase
{
    [Fact]
    public void FKF508_DeepExpressionNesting_NestedForgeInlining()
    {
        // Create a chain where each method maps using another forge with GenerateExpression.
        // With warning threshold at 4, need depth 5 to trigger FKF508 (depth > 4)
        // Map1 → Map2 → Map3 → Map4 → Map5 → Map6 reaches depth 5
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                // Level 6: leaf type
                public class Src6 { public string Value { get; set; } = ""; }
                public class Dst6 { public string Value { get; set; } = ""; }

                // Level 5: contains Level 6
                public class Src5 { public Src6 Child { get; set; } = new(); }
                public class Dst5 { public Dst6 Child { get; set; } = new(); }

                // Level 4: contains Level 5
                public class Src4 { public Src5 Child { get; set; } = new(); }
                public class Dst4 { public Dst5 Child { get; set; } = new(); }

                // Level 3: contains Level 4
                public class Src3 { public Src4 Child { get; set; } = new(); }
                public class Dst3 { public Dst4 Child { get; set; } = new(); }

                // Level 2: contains Level 3
                public class Src2 { public Src3 Child { get; set; } = new(); }
                public class Dst2 { public Dst3 Child { get; set; } = new(); }

                // Level 1: contains Level 2
                public class Src1 { public Src2 Child { get; set; } = new(); }
                public class Dst1 { public Dst2 Child { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial Dst6 Map6(Src6 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst5 Map5(Src5 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst4 Map4(Src4 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst3 Map3(Src3 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst2 Map2(Src2 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst1 Map1(Src1 src);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        // FKF508 should trigger for depth > 4 (6 nested forge calls reach depth 5)
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF508" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void FKF509_ExpressionNestingErrorThreshold_TriggersAt7Levels()
    {
        // With error threshold at 7, we need 8 nested forge methods to trigger it
        // Map1 → Map2 → Map3 → Map4 → Map5 → Map6 → Map7 → Map8 (depth reaches 7)
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Src8 { public string Value { get; set; } = ""; }
                public class Dst8 { public string Value { get; set; } = ""; }

                public class Src7 { public Src8 Child { get; set; } = new(); }
                public class Dst7 { public Dst8 Child { get; set; } = new(); }

                public class Src6 { public Src7 Child { get; set; } = new(); }
                public class Dst6 { public Dst7 Child { get; set; } = new(); }

                public class Src5 { public Src6 Child { get; set; } = new(); }
                public class Dst5 { public Dst6 Child { get; set; } = new(); }

                public class Src4 { public Src5 Child { get; set; } = new(); }
                public class Dst4 { public Dst5 Child { get; set; } = new(); }

                public class Src3 { public Src4 Child { get; set; } = new(); }
                public class Dst3 { public Dst4 Child { get; set; } = new(); }

                public class Src2 { public Src3 Child { get; set; } = new(); }
                public class Dst2 { public Dst3 Child { get; set; } = new(); }

                public class Src1 { public Src2 Child { get; set; } = new(); }
                public class Dst1 { public Dst2 Child { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial Dst8 Map8(Src8 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst7 Map7(Src7 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst6 Map6(Src6 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst5 Map5(Src5 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst4 Map4(Src4 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst3 Map3(Src3 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst2 Map2(Src2 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst1 Map1(Src1 src);
                }
            }
            """;

        var result = RunGenerator(source);

        // FKF509 should now trigger at depth >= 7 (it's expected to error)
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF509" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ExpressionNesting_ShallowNesting_NoWarning()
    {
        // Shallow nesting (only 2 levels) should not trigger FKF508/509
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Src2 { public string Value { get; set; } = ""; }
                public class Dst2 { public string Value { get; set; } = ""; }

                public class Src1 { public Src2 Child { get; set; } = new(); }
                public class Dst1 { public Dst2 Child { get; set; } = new(); }

                [Forge]
                public static partial class Forges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial Dst2 Map2(Src2 src);

                    [ForgeMethod(GenerateExpression = true, AllowNestedForging = true)]
                    public static partial Dst1 Map1(Src1 src);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        // Should not have FKF508 or FKF509 for shallow nesting
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF508");
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF509");
    }
}
