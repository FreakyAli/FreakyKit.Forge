using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class IgnoreIfNullGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void IgnoreIfNull_OnForgeMethod_WrapsAllAssignmentsInNullCheck()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } public string? Email { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("if (source.Name != null) __result.Name = source.Name;", generated);
        Assert.Contains("if (source.Email != null) __result.Email = source.Email;", generated);
    }

    [Fact]
    public void IgnoreIfNull_OnForgeMap_WrapsSpecificMemberOnly()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)] public string? Name { get; set; } public string? Email { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("if (source.Name != null) __result.Name = source.Name;", generated);
        Assert.DoesNotContain("if (source.Email != null)", generated);
        Assert.Contains("__result.Email = source.Email;", generated);
    }

    [Fact]
    public void IgnoreIfNull_OnDestMember_WrapsAssignment()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest   { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)] public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("if (source.Name != null) __result.Name = source.Name;", generated);
    }

    [Fact]
    public void IgnoreIfNull_OnUpdateMethod_WrapsAssignments()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } public int Age { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public int Age { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
                    public static partial void Update(Source source, Dest existing);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("if (source.Name != null) existing.Name = source.Name;", generated);
        Assert.Contains("if (source.Age != null) existing.Age = source.Age;", generated);
    }

    [Fact]
    public void IgnoreIfNull_WithoutFlag_NoNullCheck()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        Assert.DoesNotContain("if (source.Name != null)", generated);
        Assert.Contains("__result.Name = source.Name;", generated);
    }

    [Fact]
    public void IgnoreIfNull_MemberFalseOverridesMethodTrue()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.False)] public string? Name { get; set; } public string? Email { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        // Member-level False should override method-level True
        Assert.DoesNotContain("if (source.Name != null) __result.Name = source.Name;", generated);
        Assert.Contains("__result.Name = source.Name;", generated);
        // Other members should still be wrapped (method-level True)
        Assert.Contains("if (source.Email != null) __result.Email = source.Email;", generated);
    }

    [Fact]
    public void IgnoreIfNull_MemberTrueOverridesMethodFalse()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.True)] public string? Name { get; set; } public string? Email { get; set; } }
                public class Dest   { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.False)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        // Member-level True should override method-level False
        Assert.Contains("if (source.Name != null) __result.Name = source.Name;", generated);
        // Other members should not be wrapped (method-level False)
        Assert.DoesNotContain("if (source.Email != null)", generated);
        Assert.Contains("__result.Email = source.Email;", generated);
    }

    [Fact]
    public void IgnoreIfNull_DestMemberFalseOverridesMethodTrue()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public string? Name { get; set; } public string? Email { get; set; } }
                public class Dest   { [ForgeMap("Name", IgnoreIfNull = ForgePolicy.False)] public string Name { get; set; } = ""; public string Email { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        var generated = AssertSingleGeneratedFile(result);
        // Dest member-level False should override method-level True
        Assert.DoesNotContain("if (source.Name != null) __result.Name = source.Name;", generated);
        Assert.Contains("__result.Name = source.Name;", generated);
        // Other members should still be wrapped (method-level True)
        Assert.Contains("if (source.Email != null) __result.Email = source.Email;", generated);
    }
}
