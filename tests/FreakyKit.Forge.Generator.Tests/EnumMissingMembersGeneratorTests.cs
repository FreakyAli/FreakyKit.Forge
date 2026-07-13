using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for missing enum members (FKF212) in non-expression contexts.
/// When using ByName strategy with missing enum members, the switch expression will throw.
/// These tests verify behavior in regular (non-expression) mappings.
/// </summary>
public sealed class EnumMissingMembersGeneratorTests : GeneratorTestBase
{
    [Fact]
    public void EnumByName_MissingMember_EmitsWarning()
    {
        // Using MappingStrategy.ByName when source enum has members not in destination
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive, Pending }
                public enum DestStatus { Active, Inactive }  // Missing Pending

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF212: Missing enum member in destination
        var fkf212 = Assert.Single(result.Diagnostics, d => d.Id == "FKF212");
        Assert.Equal(DiagnosticSeverity.Warning, fkf212.Severity);
        Assert.Contains("Pending", fkf212.GetMessage());
    }

    [Fact]
    public void EnumByName_MultipleMissingMembers_EmitsMultipleWarnings()
    {
        // Source has more enum members than destination
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive, Pending, Archived, Suspended }
                public enum DestStatus { Active, Inactive }  // Missing Pending, Archived, Suspended

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // Should have multiple FKF212 warnings, one for each missing member
        var fkf212s = result.Diagnostics.Where(d => d.Id == "FKF212").ToList();
        Assert.Equal(3, fkf212s.Count);
        Assert.All(fkf212s, d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
    }

    [Fact]
    public void EnumByName_AllMembersPresent_NoWarning()
    {
        // Source enum members all exist in destination
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus { Active, Inactive, Extra }  // Has extra, but that's OK

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // No FKF212 warnings should be emitted
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF212");
        AssertNoErrors(result);
    }

    [Fact]
    public void EnumCast_MissingMember_NoWarning()
    {
        // Cast strategy doesn't emit FKF212 because it's just a cast
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active = 0, Inactive = 1, Pending = 2 }
                public enum DestStatus { Active = 0, Inactive = 1 }  // Missing Pending

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.Cast)]  // Cast, not ByName
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // Cast strategy: FKF212 is not emitted (it's just a direct cast)
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF212");
    }

    [Fact]
    public void EnumByName_GeneratesSwitch()
    {
        // Verify that ByName generates a switch expression (not just a cast)
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus { Active, Inactive }

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // ByName generates switch expression (not a simple cast)
        // It should contain pattern matching
        Assert.Contains("switch", generated);
        Assert.Contains("SourceStatus.Active", generated);
    }

    [Fact]
    public void EnumCast_GeneratesDirectCast()
    {
        // Verify that Cast generates a direct cast
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Active, Inactive }
                public enum DestStatus { Active, Inactive }

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.Cast)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Cast should generate a direct cast
        Assert.Contains("(DestStatus)source.Status", generated);
    }

    [Fact]
    public void EnumByName_WithPartialMatch()
    {
        // Source has members that destination doesn't have by exact name
        // Using ByName, this will cause switch expression to throw if those values are encountered
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public enum SourceStatus { Online, Offline, Away }
                public enum DestStatus { Active, Inactive }  // Different names entirely

                public class Source { public SourceStatus Status { get; set; } }
                public class Dest { public DestStatus Status { get; set; } }

                [Forge]
                public static partial class MyForges
                {
                    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        // All source members are missing in destination
        var fkf212s = result.Diagnostics.Where(d => d.Id == "FKF212").ToList();
        Assert.Equal(3, fkf212s.Count);
    }

    [Fact]
    public void EnumInCollection_SameEnumType_Succeeds()
    {
        // When enum types in collection are identical, no issue
        const string source = """
            using FreakyKit.Forge;
            using System.Collections.Generic;
            namespace TestNs
            {
                public enum Status { Active, Inactive, Pending }

                public class Source { public List<Status> Statuses { get; set; } = new(); }
                public class Dest { public List<Status> Statuses { get; set; } = new(); }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);
        // Should copy/handle the list without issues
        Assert.Contains("Statuses", generated);
    }
}
