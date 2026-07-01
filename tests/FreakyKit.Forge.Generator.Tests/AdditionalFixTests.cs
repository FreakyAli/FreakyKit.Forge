using System;
using System.Collections.Generic;
using FreakyKit.Forge.Generator.Models;
using Xunit;

namespace FreakyKit.Forge.Generator.Tests;

/// <summary>
/// Tests for specific fixes: expression property accessibility, inherited members,
/// nested generic types, write-only source properties, and model equality.
/// </summary>
public sealed class AdditionalFixTests : GeneratorTestBase
{
    // ─── 1. Expression property accessibility ────────────────────────────────

    [Fact]
    public void ExpressionProperty_InternalMethod_EmitsInternalStatic()
    {
        // When a forge method is declared `internal`, the generated expression
        // property must also be `internal static`, not `public static`.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Person    { public string Name { get; set; } = ""; public int Age { get; set; } }
                public class PersonDto { public string Name { get; set; } = ""; public int Age { get; set; } }
                [Forge]
                public static partial class PersonForges
                {
                    [ForgeMethod(GenerateExpression = true)]
                    internal static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);
        var generated = AssertSingleGeneratedFile(result);

        // Expression property must match the method's accessibility
        Assert.Contains("internal static Expression<Func<Person, PersonDto>> ToDtoExpression", generated);
        // Must NOT be public
        Assert.DoesNotContain("public static Expression<Func<Person, PersonDto>> ToDtoExpression", generated);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
    }

    [Fact]
    public void ExpressionProperty_PublicMethod_EmitsPublicStatic()
    {
        // Sanity check: public method gets public expression property.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class A { public int X { get; set; } }
                public class B { public int X { get; set; } }
                [Forge]
                public static partial class F
                {
                    [ForgeMethod(GenerateExpression = true)]
                    public static partial B Map(A source);
                }
            }
            """;

        var generated = AssertSingleGeneratedFile(RunGenerator(source));
        Assert.Contains("public static Expression<Func<A, B>> MapExpression", generated);
    }

    // ─── 2. Inherited members ────────────────────────────────────────────────

    [Fact]
    public void InheritedProperties_MappedFromBaseClass()
    {
        // Properties declared on a base class should be included in the generated mapping.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class EntityBase { public int Id { get; set; } }
                public class Person : EntityBase { public string Name { get; set; } = ""; }

                public class DtoBase { public int Id { get; set; } }
                public class PersonDto : DtoBase { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial PersonDto ToDto(Person source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Both the inherited Id and the declared Name must be mapped
        Assert.Contains("__result.Id = source.Id", generated);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
    }

    [Fact]
    public void InheritedProperties_MultiLevel_MappedCorrectly()
    {
        // Properties from grandparent class should also be mapped.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class GrandBase  { public int Id { get; set; } }
                public class ParentBase : GrandBase { public string Code { get; set; } = ""; }
                public class Child : ParentBase { public string Name { get; set; } = ""; }

                public class ChildDto { public int Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ChildDto ToDto(Child source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Id = source.Id", generated);
        Assert.Contains("__result.Code = source.Code", generated);
        Assert.Contains("__result.Name = source.Name", generated);
    }

    // ─── 3. Nested generic types ─────────────────────────────────────────────

    [Fact]
    public void NestedGenericType_DictionaryStringListInt_ProducesCorrectShortName()
    {
        // Dictionary<string, List<int>> must produce the full generic form in generated code,
        // not just "Dictionary" or "List" without type arguments.
        const string source = """
            using System.Collections.Generic;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source { public Dictionary<string, List<int>> Data { get; set; } = new(); }
                public class Dest   { public Dictionary<string, List<int>> Data { get; set; } = new(); }

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
        // The assignment must reference the full generic type, not truncated short names
        Assert.Contains("__result.Data = source.Data", generated);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
    }

    [Fact]
    public void NestedGenericType_CollectionProjectMethod_RetainsTypeArguments()
    {
        // A collection-project method with nested generics should produce correct type names
        // in the method signature.
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Item { public string Name { get; set; } = ""; }
                public class ItemDto { public string Name { get; set; } = ""; }

                [Forge]
                public static partial class MyForges
                {
                    public static partial ItemDto ToItemDto(Item source);
                    public static partial List<ItemDto> ToItemDtos(List<Item> source);
                }
            }
            """;

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        // Signature must include List<ItemDto> and List<Item>, not just "List"
        Assert.Contains("List<ItemDto>", generated);
        Assert.Contains("List<Item>", generated);
        Assert.False(result.HasCompilationErrors,
            string.Join("\n", result.CompilationDiagnostics));
    }

    // ─── 4. Write-only source properties ─────────────────────────────────────

    [Fact]
    public void WriteOnlySourceProperty_ExcludedFromMapping()
    {
        // A source property with only a setter (no getter) cannot be read and must not
        // appear in the generated mapping code.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Secret { set { } }
                }
                public class Dest
                {
                    public string Name { get; set; } = "";
                    public int Secret { get; set; }
                }

                [Forge]
                public static partial class MyForges
                {
                    public static partial Dest ToDest(Source source);
                }
            }
            """;

        var result = RunGenerator(source);

        var generated = AssertSingleGeneratedFile(result);
        // Name should be mapped
        Assert.Contains("__result.Name = source.Name", generated);
        // Secret from source is write-only — must NOT appear as a source read
        Assert.DoesNotContain("source.Secret", generated);
    }

    [Fact]
    public void WriteOnlySourceProperty_DoesNotPreventOtherMappings()
    {
        // A write-only source property should be silently skipped; other properties
        // must still be mapped normally.
        const string source = """
            using FreakyKit.Forge;
            namespace TestNs
            {
                public class Source
                {
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                    public string WriteOnly { set { } }
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

        var result = RunGenerator(source);
        AssertNoErrors(result);

        var generated = AssertSingleGeneratedFile(result);
        Assert.Contains("__result.Name = source.Name", generated);
        Assert.Contains("__result.Age = source.Age", generated);
        Assert.DoesNotContain("WriteOnly", generated);
    }

    // ─── 5. Model equality ───────────────────────────────────────────────────

    [Fact]
    public void ForgeClassModel_SameData_AreEqual()
    {
        var methods = new List<ForgeMethodModel>();
        var containing = new List<ContainingTypeInfo>();

        var a = new ForgeClassModel("TestNs", "MyForges", "public", "TestNs.MyForges", false, methods, containing);
        var b = new ForgeClassModel("TestNs", "MyForges", "public", "TestNs.MyForges", false, methods, containing);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ForgeClassModel_DifferentNamespace_AreNotEqual()
    {
        var methods = new List<ForgeMethodModel>();

        var a = new ForgeClassModel("Ns1", "MyForges", "public", "Ns1.MyForges", false, methods);
        var b = new ForgeClassModel("Ns2", "MyForges", "public", "Ns2.MyForges", false, methods);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ForgeClassModel_DifferentHasErrors_AreNotEqual()
    {
        var methods = new List<ForgeMethodModel>();

        var a = new ForgeClassModel("TestNs", "MyForges", "public", "TestNs.MyForges", false, methods);
        var b = new ForgeClassModel("TestNs", "MyForges", "public", "TestNs.MyForges", true, methods);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ForgeMethodModel_SameData_AreEqual()
    {
        var construction = new ConstructionModel(ConstructionKind.Parameterless, Array.Empty<ConstructorArgModel>());
        var assignments = new List<MemberAssignmentModel>();
        var nested = new List<ForgeMethodModel>();

        var a = new ForgeMethodModel("ToDto", "public", "TestNs.Source", "Source", "source",
            "TestNs.Dest", "Dest", construction, assignments, nested);
        var b = new ForgeMethodModel("ToDto", "public", "TestNs.Source", "Source", "source",
            "TestNs.Dest", "Dest", construction, assignments, nested);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ForgeMethodModel_DifferentMethodName_AreNotEqual()
    {
        var construction = new ConstructionModel(ConstructionKind.Parameterless, Array.Empty<ConstructorArgModel>());
        var assignments = new List<MemberAssignmentModel>();
        var nested = new List<ForgeMethodModel>();

        var a = new ForgeMethodModel("ToDto", "public", "TestNs.Source", "Source", "source",
            "TestNs.Dest", "Dest", construction, assignments, nested);
        var b = new ForgeMethodModel("MapTo", "public", "TestNs.Source", "Source", "source",
            "TestNs.Dest", "Dest", construction, assignments, nested);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ForgeMethodModel_DifferentGenerateExpression_AreNotEqual()
    {
        var construction = new ConstructionModel(ConstructionKind.Parameterless, Array.Empty<ConstructorArgModel>());
        var assignments = new List<MemberAssignmentModel>();
        var nested = new List<ForgeMethodModel>();

        var a = new ForgeMethodModel("ToDto", "public", "TestNs.Source", "Source", "source",
            "TestNs.Dest", "Dest", construction, assignments, nested,
            generateExpression: false);
        var b = new ForgeMethodModel("ToDto", "public", "TestNs.Source", "Source", "source",
            "TestNs.Dest", "Dest", construction, assignments, nested,
            generateExpression: true);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MemberAssignmentModel_SameData_AreEqual()
    {
        var a = new MemberAssignmentModel("Name", "source.Name");
        var b = new MemberAssignmentModel("Name", "source.Name");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void MemberAssignmentModel_DifferentDestMember_AreNotEqual()
    {
        var a = new MemberAssignmentModel("Name", "source.Name");
        var b = new MemberAssignmentModel("Title", "source.Name");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MemberAssignmentModel_DifferentIgnoreIfNull_AreNotEqual()
    {
        var a = new MemberAssignmentModel("Name", "source.Name", ignoreIfNull: false);
        var b = new MemberAssignmentModel("Name", "source.Name", ignoreIfNull: true);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ConstructionModel_SameData_AreEqual()
    {
        var argsA = new List<ConstructorArgModel> { new("name", "source.Name") };
        var argsB = new List<ConstructorArgModel> { new("name", "source.Name") };

        var a = new ConstructionModel(ConstructionKind.Parameterized, argsA);
        var b = new ConstructionModel(ConstructionKind.Parameterized, argsB);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ConstructionModel_DifferentKind_AreNotEqual()
    {
        var args = new List<ConstructorArgModel>();

        var a = new ConstructionModel(ConstructionKind.Parameterless, args);
        var b = new ConstructionModel(ConstructionKind.Parameterized, args);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ConstructorArgModel_SameData_AreEqual()
    {
        var a = new ConstructorArgModel("name", "source.Name", "source.Name");
        var b = new ConstructorArgModel("name", "source.Name", "source.Name");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ConstructorArgModel_DifferentExpression_AreNotEqual()
    {
        var a = new ConstructorArgModel("name", "source.Name");
        var b = new ConstructorArgModel("name", "source.FirstName");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ContainingTypeInfo_SameData_AreEqual()
    {
        var a = new ContainingTypeInfo("public", "class", "Outer");
        var b = new ContainingTypeInfo("public", "class", "Outer");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ContainingTypeInfo_DifferentKeyword_AreNotEqual()
    {
        var a = new ContainingTypeInfo("public", "class", "Outer");
        var b = new ContainingTypeInfo("public", "struct", "Outer");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ForgeClassModel_NullEquality_ReturnsFalse()
    {
        var model = new ForgeClassModel("TestNs", "MyForges", "public", "TestNs.MyForges", false, new List<ForgeMethodModel>());

        Assert.False(model.Equals((ForgeClassModel)null!));
        Assert.False(model.Equals((object)null!));
    }

    [Fact]
    public void ForgeMethodModel_NullEquality_ReturnsFalse()
    {
        var construction = new ConstructionModel(ConstructionKind.Parameterless, Array.Empty<ConstructorArgModel>());
        var model = new ForgeMethodModel("ToDto", "public", "TestNs.Source", "Source", "source",
            "TestNs.Dest", "Dest", construction, new List<MemberAssignmentModel>(), new List<ForgeMethodModel>());

        Assert.False(model.Equals((ForgeMethodModel)null!));
        Assert.False(model.Equals((object)null!));
    }
}
