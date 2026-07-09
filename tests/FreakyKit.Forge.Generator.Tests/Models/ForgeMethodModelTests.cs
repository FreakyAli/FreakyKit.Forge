using System.Collections.Generic;
using System.Linq;
using FreakyKit.Forge.Generator.Models;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests.Models;

/// <summary>
/// Tests for ForgeMethodModel correctness, especially hash/equality consistency.
/// </summary>
public sealed class ForgeMethodModelTests
{
    /// <summary>
    /// Verifies that GetHashCode includes all significant fields from Equals.
    /// Two models differing only in Assignments should still be unequal, and have consistent hash codes.
    /// This test catches violations of the GetHashCode/Equals contract.
    /// </summary>
    [Fact]
    public void ForgeMethodModel_HashCodeConsistentWithEquals()
    {
        var construction = new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>());
        var assignment1 = new MemberAssignmentModel("Prop1", "source.Prop1");
        var assignment2 = new MemberAssignmentModel("Prop2", "source.Prop2");

        var assignments1 = new[] { assignment1 };
        var assignments2 = new[] { assignment1, assignment2 };

        var model1 = new ForgeMethodModel(
            methodName: "ToDto",
            accessibility: "public",
            sourceTypeFqn: "Test.Person",
            sourceTypeShortName: "Person",
            sourceParameterName: "source",
            destTypeFqn: "Test.PersonDto",
            destTypeShortName: "PersonDto",
            construction: construction,
            assignments: assignments1,
            nestedMethods: new List<ForgeMethodModel>(),
            methodKind: ForgeMethodKind.Create
        );

        var model2 = new ForgeMethodModel(
            methodName: "ToDto",
            accessibility: "public",
            sourceTypeFqn: "Test.Person",
            sourceTypeShortName: "Person",
            sourceParameterName: "source",
            destTypeFqn: "Test.PersonDto",
            destTypeShortName: "PersonDto",
            construction: construction,
            assignments: assignments2,  // Different: 2 assignments vs 1
            nestedMethods: new List<ForgeMethodModel>(),
            methodKind: ForgeMethodKind.Create
        );

        // Models should NOT be equal (different assignments)
        Assert.NotEqual(model1, model2);

        // For hash table correctness: if Equals would say two objects are different,
        // they MUST be storable in the same HashSet (they won't collide).
        // After the fix, model1 and model2 will have different hashes due to Assignments.Count.
        var hashSet = new HashSet<ForgeMethodModel> { model1 };
        Assert.DoesNotContain(model2, hashSet);  // model2 is different, so should not be found

        // Both should be addable to the set (no collision)
        hashSet.Add(model2);
        Assert.Equal(2, hashSet.Count);
    }

    /// <summary>
    /// Verifies that models with identical values have the same hash code and are equal.
    /// </summary>
    [Fact]
    public void ForgeMethodModel_IdenticalModelsHaveSameHashAndAreEqual()
    {
        var construction = new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>());
        var assignments = new[] { new MemberAssignmentModel("Prop", "source.Prop") };
        var nested = new List<ForgeMethodModel>();

        var model1 = new ForgeMethodModel(
            methodName: "ToDto",
            accessibility: "public",
            sourceTypeFqn: "Test.Person",
            sourceTypeShortName: "Person",
            sourceParameterName: "source",
            destTypeFqn: "Test.PersonDto",
            destTypeShortName: "PersonDto",
            construction: construction,
            assignments: assignments,
            nestedMethods: nested,
            methodKind: ForgeMethodKind.Create
        );

        var model2 = new ForgeMethodModel(
            methodName: "ToDto",
            accessibility: "public",
            sourceTypeFqn: "Test.Person",
            sourceTypeShortName: "Person",
            sourceParameterName: "source",
            destTypeFqn: "Test.PersonDto",
            destTypeShortName: "PersonDto",
            construction: construction,
            assignments: assignments,
            nestedMethods: nested,
            methodKind: ForgeMethodKind.Create
        );

        // Should be equal
        Assert.Equal(model1, model2);

        // Should have same hash
        Assert.Equal(model1.GetHashCode(), model2.GetHashCode());

        // Should work correctly in HashSet
        var set = new HashSet<ForgeMethodModel> { model1 };
        Assert.Contains(model2, set);  // model2 is equal to model1, so should be found
    }
}
