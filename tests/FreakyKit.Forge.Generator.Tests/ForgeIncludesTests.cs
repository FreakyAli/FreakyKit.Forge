using System.Linq;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

public sealed class ForgeIncludesTests : GeneratorTestBase
{
    [Fact]
    public void ForgeIncludes_BasicInheritance_MergesBaseAssignments()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } public string CreatedAt { get; set; } }
                public class BaseDto { public int Id { get; set; } public string CreatedAt { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 2);

        // PersonForges.ToDto should have inherited base assignments + local
        Assert.Contains("__result.Id = source.Id", generated);
        Assert.Contains("__result.CreatedAt = source.CreatedAt", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void ForgeIncludes_MultipleIncludedClasses_MergesAll()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class AuditEntity : BaseEntity { public string AuditBy { get; set; } }
                public class AuditDto : BaseDto { public string AuditBy { get; set; } }

                public class FullEntity : AuditEntity { public string Name { get; set; } }
                public class FullDto : AuditDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                public static partial class AuditForges
                {
                    public static partial AuditDto ToAuditDto(AuditEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges), typeof(AuditForges))]
                public static partial class FullForges
                {
                    public static partial FullDto ToDto(FullEntity source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 3);

        // FullForges.ToDto should have assignments from both base classes + local
        Assert.Contains("__result.Id = source.Id", generated);
        Assert.Contains("__result.AuditBy = source.AuditBy", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void ForgeIncludes_LocalOverridesIncluded_EmitsFKF537()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } public string Status { get; set; } }
                public class BaseDto { public int Id { get; set; } public string Status { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        // Person and PersonDto both have Id, Status, and Name
        // BaseForges has Id and Status — both also on Person/PersonDto directly
        // Local assignments for Id and Status shadow the included ones (FKF537 is Info)
        var fkf537 = result.Diagnostics.Where(d => d.Id == "FKF537").ToList();
        Assert.True(fkf537.Count >= 1, "Expected at least one FKF537 (shadowed assignment) diagnostic");
    }

    [Fact]
    public void ForgeIncludes_NoCompatibleMethod_EmitsFKF536()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class EntityA { public int Id { get; set; } }
                public class DtoA { public int Id { get; set; } }

                public class EntityB { public string Name { get; set; } }
                public class DtoB { public string Name { get; set; } }

                [Forge]
                public static partial class AForges
                {
                    public static partial DtoA ToA(EntityA source);
                }

                [Forge]
                [ForgeIncludes(typeof(AForges))]
                public static partial class BForges
                {
                    public static partial DtoB ToB(EntityB source);
                }
            }
            """;

        var result = RunGenerator(source);
        // FKF536 is Warning, not Error
        AssertNoErrors(result);
        var fkf536 = result.Diagnostics.Where(d => d.Id == "FKF536").ToList();
        Assert.Single(fkf536);
    }

    [Fact]
    public void ForgeIncludes_IncludedClassNotForge_ViaExistingType_EmitsFKF534()
    {
        // Entity exists but has no [Forge] — FKF534, not FKF533
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Entity { public int Id { get; set; } }
                public class Dto { public int Id { get; set; } }

                [Forge]
                [ForgeIncludes(typeof(Entity))]
                public static partial class MyForges
                {
                    public static partial Dto ToDto(Entity source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF534");
    }

    [Fact]
    public void ForgeIncludes_IncludedClassNotForge_EmitsFKF534()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Entity { public int Id { get; set; } }
                public class Dto { public int Id { get; set; } }

                public static class NotAForge
                {
                    public static Dto ToDto(Entity source) => new Dto();
                }

                [Forge]
                [ForgeIncludes(typeof(NotAForge))]
                public static partial class MyForges
                {
                    public static partial Dto ToDto(Entity source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF534");
    }

    [Fact]
    public void ForgeIncludes_SelfInclude_EmitsFKF535()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Entity { public int Id { get; set; } }
                public class Dto { public int Id { get; set; } }

                [Forge]
                [ForgeIncludes(typeof(MyForges))]
                public static partial class MyForges
                {
                    public static partial Dto ToDto(Entity source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF535");
    }

    [Fact]
    public void ForgeIncludes_CircularIncludes_EmitsFKF535()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Entity { public int Id { get; set; } }
                public class Dto { public int Id { get; set; } }

                [Forge]
                [ForgeIncludes(typeof(BForges))]
                public static partial class AForges
                {
                    public static partial Dto ToA(Entity source);
                }

                [Forge]
                [ForgeIncludes(typeof(AForges))]
                public static partial class BForges
                {
                    public static partial Dto ToB(Entity source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF535");
    }

    [Fact]
    public void ForgeIncludes_TransitiveCircular_EmitsFKF535()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Entity { public int Id { get; set; } }
                public class Dto { public int Id { get; set; } }

                [Forge]
                [ForgeIncludes(typeof(BForges))]
                public static partial class AForges
                {
                    public static partial Dto ToA(Entity source);
                }

                [Forge]
                [ForgeIncludes(typeof(CForges))]
                public static partial class BForges
                {
                    public static partial Dto ToB(Entity source);
                }

                [Forge]
                [ForgeIncludes(typeof(AForges))]
                public static partial class CForges
                {
                    public static partial Dto ToC(Entity source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertHasError(result, "FKF535");
    }

    [Fact]
    public void ForgeIncludes_WithForgeUses_BothWork()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Inner { public int Value { get; set; } }
                public class InnerDto { public int Value { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } public Inner Inner { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } public InnerDto Inner { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                public static partial class InnerForges
                {
                    public static partial InnerDto ToInnerDto(Inner source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                [ForgeUses(typeof(InnerForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod(AllowNestedForging = true)]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 3);

        var personOutput = result.RunResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith("TestNs_PersonForges.Forge.g.cs"))
            .Select(t => t.GetText().ToString())
            .FirstOrDefault();
        Assert.NotNull(personOutput);

        // Local Name assignment
        Assert.Contains("__result.Name = source.Name", personOutput);
        // Inherited Id from BaseForges (shadowed by local, but present)
        Assert.Contains("__result.Id = source.Id", personOutput);
        // Nested Inner via InnerForges (discovered through [ForgeUses])
        Assert.Contains("InnerForges", personOutput);
    }

    [Fact]
    public void ForgeIncludes_UpdateMethod_MergesAssignments()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial void Update(Person source, PersonDto dest);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 2);

        // Update method should also get inherited assignments
        Assert.Contains("dest.Name = source.Name", generated);
    }

    [Fact]
    public void ForgeIncludes_ParameterNameSubstitution()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity entity);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 2);

        // The generated PersonForges.ToDto should use "source" (not "entity" from BaseForges)
        // BaseForges.ToBaseDto uses "entity" parameter, but when merged into PersonForges.ToDto,
        // all references should be substituted to "source"
        // Check the PersonForges output specifically
        var personForgesOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PersonForges"));
        Assert.NotNull(personForgesOutput);
        Assert.Contains("source.Id", personForgesOutput);
        Assert.DoesNotContain("entity.Id", personForgesOutput);
    }

    [Fact]
    public void ForgeIncludes_DiamondIncludes_DeduplicatesAssignments()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Root { public int Id { get; set; } }
                public class RootDto { public int Id { get; set; } }

                public class Left : Root { public string L { get; set; } }
                public class LeftDto : RootDto { public string L { get; set; } }

                public class Right : Root { public string R { get; set; } }
                public class RightDto : RootDto { public string R { get; set; } }

                public class Bottom : Root { public string L { get; set; } public string R { get; set; } public string B { get; set; } }
                public class BottomDto : RootDto { public string L { get; set; } public string R { get; set; } public string B { get; set; } }

                [Forge]
                public static partial class RootForges
                {
                    public static partial RootDto ToRootDto(Root source);
                }

                [Forge]
                [ForgeIncludes(typeof(RootForges))]
                public static partial class LeftForges
                {
                    public static partial LeftDto ToLeftDto(Left source);
                }

                [Forge]
                [ForgeIncludes(typeof(RootForges))]
                public static partial class RightForges
                {
                    public static partial RightDto ToRightDto(Right source);
                }

                [Forge]
                [ForgeIncludes(typeof(LeftForges), typeof(RightForges))]
                public static partial class BottomForges
                {
                    public static partial BottomDto ToBottomDto(Bottom source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var bottomOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("BottomForges"));
        Assert.NotNull(bottomOutput);
        Assert.Contains("__result.B = source.B", bottomOutput);

        // Id should appear exactly once despite diamond inheritance
        var idCount = bottomOutput.Split("__result.Id = source.Id").Length - 1;
        Assert.Equal(1, idCount);
    }

    [Fact]
    public void ForgeIncludes_WithoutForge_EmitsFKF538()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Entity { public int Id { get; set; } }
                public class Dto { public int Id { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial Dto ToDto(Entity source);
                }

                [ForgeIncludes(typeof(BaseForges))]
                public static partial class MyForges
                {
                    public static partial Dto ToDto(Entity source);
                }
            }
            """;

        var result = RunGenerator(source);
        Assert.Contains(result.Diagnostics, d => d.Id == "FKF538");
    }

    [Fact]
    public void ForgeIncludes_ValidUsage_NoDiagnostics()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        // Should not have FKF533-535 or FKF538
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF533");
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF534");
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF535");
        Assert.DoesNotContain(result.Diagnostics, d => d.Id == "FKF538");
    }

    [Fact]
    public void ForgeIncludes_WhenLocalAlsoMapsBaseMembers_AllShadowed()
    {
        // When both local method and included method map the same members (because
        // derived types inherit base members), local wins for all. FKF537 info emitted.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Person : BaseEntity { public string ZName { get; set; } }
                public class PersonDto : BaseDto { public string ZName { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var personOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PersonForges"));
        Assert.NotNull(personOutput);

        // Both Id and ZName should be present (local mappings)
        Assert.Contains("__result.Id = source.Id", personOutput);
        Assert.Contains("__result.ZName = source.ZName", personOutput);

        // FKF537 emitted for Id being shadowed
        var fkf537 = result.Diagnostics.Where(d => d.Id == "FKF537").ToList();
        Assert.True(fkf537.Count >= 1, "Expected FKF537 for shadowed assignment");
    }

    [Fact]
    public void ForgeIncludes_ExplicitMode_InheritedFromIncludedClass()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge(Mode = ForgeMode.Explicit)]
                public static partial class BaseForges
                {
                    [ForgeMethod]
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 2);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void ForgeIncludes_InheritsForgeMapCustomMapping()
    {
        // The real value: base method has [ForgeMap] custom mapping that derived inherits
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity
                {
                    public int Id { get; set; }
                    [ForgeMap("DisplayName")]
                    public string FullName { get; set; }
                }
                public class BaseDto
                {
                    public int Id { get; set; }
                    public string DisplayName { get; set; }
                }

                public class Person : BaseEntity { public int Age { get; set; } }
                public class PersonDto : BaseDto { public int Age { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var personOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PersonForges"));
        Assert.NotNull(personOutput);

        // Should have Age (local) and inherited DisplayName mapping from FullName
        Assert.Contains("__result.Age = source.Age", personOutput);
        Assert.Contains("__result.DisplayName = source.FullName", personOutput);
    }

    [Fact]
    public void ForgeIncludes_WithExplicitModeOnConsumer_OnlyForgeMethodsIncluded()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge(Mode = ForgeMode.Explicit)]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    [ForgeMethod]
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 2);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    [Fact]
    public void ForgeIncludes_IncompatibleDestType_EmitsFKF536()
    {
        // PersonDto does NOT inherit from BaseDto — type compatibility fails, FKF536
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity
                {
                    [ForgeMap("DisplayName")]
                    public string FullName { get; set; }
                }
                public class BaseDto
                {
                    public string DisplayName { get; set; }
                }

                public class Person : BaseEntity { public int Age { get; set; } }
                // PersonDto does NOT inherit from BaseDto — no type compatibility
                public class PersonDto { public int Age { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var fkf536 = result.Diagnostics.Where(d => d.Id == "FKF536").ToList();
        Assert.Single(fkf536);
    }

    [Fact]
    public void ForgeIncludes_InheritedBaseMembers_EmitsFKF537Shadowed()
    {
        // PersonDto inherits BaseDto so it HAS all base members.
        // All base members are locally discoverable, so FKF537 (shadowed) fires.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } public string ExtraField { get; set; } }
                public class BaseDto { public int Id { get; set; } public string ExtraField { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var fkf537 = result.Diagnostics.Where(d => d.Id == "FKF537").ToList();
        Assert.True(fkf537.Count >= 1);
    }

    [Fact]
    public void ForgeIncludes_ConstructorArgOverlap_EmitsFKF540()
    {
        // If PersonDto has a constructor parameter "id", the merged Id assignment
        // from the profile should be skipped with FKF540 (constructor already handles it)
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } public string CreatedAt { get; set; } }
                public class BaseDto { public int Id { get; set; } public string CreatedAt { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto
                {
                    public PersonDto(int id) { Id = id; }
                    public string Name { get; set; }
                }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var personOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PersonForges"));
        Assert.NotNull(personOutput);

        // Id should be in constructor, not double-assigned
        Assert.Contains("new PersonDto(", personOutput);
        Assert.Contains("source.Name", personOutput);

        // FKF540 emitted for constructor-handled member
        var fkf540 = result.Diagnostics.Where(d => d.Id == "FKF540").ToList();
        Assert.True(fkf540.Count >= 1, "Expected FKF540 for constructor-handled member");
    }

    [Fact]
    public void ForgeIncludes_InitOnlyOnDerived_RecheckInitFlag()
    {
        // Base type has regular setter, derived type overrides with init-only
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto
                {
                    public string Name { get; init; }
                }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var personOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PersonForges"));
        Assert.NotNull(personOutput);

        // Name should be in object initializer (init-only), not separate __result.Name assignment
        Assert.Contains("Name = source.Name", personOutput);
        Assert.DoesNotContain("__result.Name = source.Name", personOutput);
    }

    [Fact]
    public void ForgeIncludes_RecordClass_Inheritance()
    {
        // Record classes can inherit from other record classes
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public record BaseEntity(int Id);
                public record BaseDto { public int Id { get; init; } }

                public record Person(int Id, string Name) : BaseEntity(Id);
                public record PersonDto : BaseDto { public string Name { get; init; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertGeneratedFiles(result, 2);

        // Should have Name mapping in generated PersonForges output
        var personOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PersonForges"));
        Assert.NotNull(personOutput);
        Assert.Contains("Name = source.Name", personOutput);
    }

    [Fact]
    public void ForgeIncludes_RecordStruct_NoInheritance_FKF536()
    {
        // Record structs cannot inherit — type compatibility check should reject
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public record struct BaseValue(int Id);
                public record struct BaseDto { public int Id { get; set; } }

                public record struct PersonValue { public int Id { get; set; } public string Name { get; set; } }
                public record struct PersonDto { public int Id { get; set; } public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseValue source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(PersonValue source);
                }
            }
            """;

        var result = RunGenerator(source);
        // No error — FKF536 warning because no compatible method (structs don't have inheritance)
        AssertNoErrors(result);
        var fkf536 = result.Diagnostics.Where(d => d.Id == "FKF536").ToList();
        Assert.Single(fkf536);
    }

    [Fact]
    public void ForgeIncludes_UpdateMethod_SkipsInitOnlyFromProfile_EmitsFKF541()
    {
        // Update methods can't set init-only properties, so inherited init-only assignments
        // should be skipped with FKF541
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; init; } }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial void Update(Person source, PersonDto dest);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var personOutput = result.RunResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .FirstOrDefault(t => t.Contains("PersonForges"));
        Assert.NotNull(personOutput);

        // Name should be assigned (not init-only)
        Assert.Contains("dest.Name = source.Name", personOutput);

        // FKF541 emitted for init-only member skipped in update
        var fkf541 = result.Diagnostics.Where(d => d.Id == "FKF541").ToList();
        Assert.True(fkf541.Count >= 1, "Expected FKF541 for init-only member skipped in update");
    }

    [Fact]
    public void ForgeIncludes_SkipsPolymorphicDispatchMethods()
    {
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } }
                public class BaseDto { public int Id { get; set; } }

                public class Dog : BaseEntity { public string Breed { get; set; } }
                public class DogDto : BaseDto { public string Breed { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);

                    public static partial DogDto ToDogDto(Dog source);

                    [ForgePolymorphic(typeof(Dog), nameof(ToDogDto))]
                    public static partial BaseDto Dispatch(BaseEntity source);
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class DogForges
                {
                    public static partial DogDto ToDto(Dog source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        // Polymorphic dispatch methods are skipped during profile merging
        var dogOutput = result.RunResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith("TestNs_DogForges.Forge.g.cs"))
            .Select(t => t.GetText().ToString())
            .FirstOrDefault();
        Assert.NotNull(dogOutput);
        Assert.Contains("__result.Breed = source.Breed", dogOutput);
        Assert.Contains("__result.Id = source.Id", dogOutput);
    }

    [Fact]
    public void ForgeIncludes_ProfileWithConditionMethod_NoErrors()
    {
        // Profile class has a condition method on a member.
        // When inherited, the condition method name is qualified with the profile class FQN
        // so it resolves in the consuming class. With C# inheritance the local member
        // discovery shadows the inherited one, but the qualification logic is exercised
        // for any non-shadowed members.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class BaseEntity { public int Id { get; set; } public string Status { get; set; } }
                public class BaseDto
                {
                    public int Id { get; set; }
                    [ForgeMap("Status", Condition = "ShouldMapStatus")]
                    public string Status { get; set; }
                }

                public class Person : BaseEntity { public string Name { get; set; } }
                public class PersonDto : BaseDto { public string Name { get; set; } }

                [Forge]
                public static partial class BaseForges
                {
                    public static partial BaseDto ToBaseDto(BaseEntity source);

                    public static bool ShouldMapStatus(BaseEntity source) => source.Status != null;
                }

                [Forge]
                [ForgeIncludes(typeof(BaseForges))]
                public static partial class PersonForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        // BaseForges output should use ShouldMapStatus condition
        var baseOutput = result.RunResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith("TestNs_BaseForges.Forge.g.cs"))
            .Select(t => t.GetText().ToString())
            .FirstOrDefault();
        Assert.NotNull(baseOutput);
        Assert.Contains("ShouldMapStatus", baseOutput);

        // PersonForges should compile without errors (condition is shadowed by local discovery
        // but qualification logic handles non-shadowed cases)
        var personOutput = result.RunResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith("TestNs_PersonForges.Forge.g.cs"))
            .Select(t => t.GetText().ToString())
            .FirstOrDefault();
        Assert.NotNull(personOutput);
        Assert.Contains("__result.Name = source.Name", personOutput);
    }
}
