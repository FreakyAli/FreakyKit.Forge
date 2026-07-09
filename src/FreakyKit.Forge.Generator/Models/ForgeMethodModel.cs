using System;
using System.Collections.Generic;
using System.Linq;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Extracted, equatable model for a single forge method.
/// Carries all information needed to generate the method body.
/// </summary>
internal sealed class ForgeMethodModel : IEquatable<ForgeMethodModel>
{
    public string MethodName { get; }
    public string Accessibility { get; }
    public string SourceTypeFqn { get; }
    public string SourceTypeShortName { get; }
    public string SourceParameterName { get; }
    public string DestTypeFqn { get; }
    public string DestTypeShortName { get; }
    public ConstructionModel Construction { get; }
    public IReadOnlyList<MemberAssignmentModel> Assignments { get; }
    public IReadOnlyList<ForgeMethodModel> NestedMethods { get; }
    public ForgeMethodKind MethodKind { get; }
    public string DestParameterName { get; }
    public string? BeforeHookName { get; }
    public string? AfterHookName { get; }
    public string? SourceFilePath { get; }
    public int SourceLineNumber { get; }
    /// <summary>
    /// For CollectionProject methods: the full LINQ projection expression (everything after "return").
    /// Null for Create and Update methods.
    /// </summary>
    public string? CollectionProjectExpression { get; }

    /// <summary>
    /// For DictionaryProject methods: the concrete Dictionary&lt;K,V&gt; type name to use in "new" expressions.
    /// Differs from DestTypeShortName when the return type is an interface (IDictionary, IReadOnlyDictionary).
    /// </summary>
    public string? ConcreteDictInstantiationName { get; }

    /// <summary>
    /// When true, the generator emits an additional static <c>Expression&lt;Func&lt;TSrc, TDest&gt;&gt;</c>
    /// property alongside the imperative method body. Default false.
    /// </summary>
    public bool GenerateExpression { get; }

    /// <summary>
    /// Name of the emitted expression property. Conventionally <c>{MethodName}Expression</c>.
    /// </summary>
    public string ExpressionPropertyName { get; }

    public ForgeMethodModel(
        string methodName,
        string accessibility,
        string sourceTypeFqn,
        string sourceTypeShortName,
        string sourceParameterName,
        string destTypeFqn,
        string destTypeShortName,
        ConstructionModel construction,
        IReadOnlyList<MemberAssignmentModel> assignments,
        IReadOnlyList<ForgeMethodModel> nestedMethods,
        ForgeMethodKind methodKind = ForgeMethodKind.Create,
        string destParameterName = "",
        string? beforeHookName = null,
        string? afterHookName = null,
        string? sourceFilePath = null,
        int sourceLineNumber = 0,
        string? collectionProjectExpression = null,
        string? concreteDictInstantiationName = null,
        bool generateExpression = false,
        string? expressionPropertyName = null)
    {
        MethodName = methodName;
        Accessibility = accessibility;
        SourceTypeFqn = sourceTypeFqn;
        SourceTypeShortName = sourceTypeShortName;
        SourceParameterName = sourceParameterName;
        DestTypeFqn = destTypeFqn;
        DestTypeShortName = destTypeShortName;
        Construction = construction;
        Assignments = assignments;
        NestedMethods = nestedMethods;
        MethodKind = methodKind;
        DestParameterName = destParameterName;
        BeforeHookName = beforeHookName;
        AfterHookName = afterHookName;
        SourceFilePath = sourceFilePath;
        SourceLineNumber = sourceLineNumber;
        CollectionProjectExpression = collectionProjectExpression;
        ConcreteDictInstantiationName = concreteDictInstantiationName;
        GenerateExpression = generateExpression;
        ExpressionPropertyName = expressionPropertyName ?? $"{methodName}Expression";
    }

    public bool Equals(ForgeMethodModel other)
    {
        if (other is null) return false;
        return MethodName == other.MethodName
            && Accessibility == other.Accessibility
            && SourceTypeFqn == other.SourceTypeFqn
            && SourceTypeShortName == other.SourceTypeShortName
            && SourceParameterName == other.SourceParameterName
            && DestTypeFqn == other.DestTypeFqn
            && DestTypeShortName == other.DestTypeShortName
            && Equals(Construction, other.Construction)
            && Assignments.SequenceEqual(other.Assignments)
            && NestedMethods.SequenceEqual(other.NestedMethods)
            && MethodKind == other.MethodKind
            && DestParameterName == other.DestParameterName
            && BeforeHookName == other.BeforeHookName
            && AfterHookName == other.AfterHookName
            && SourceFilePath == other.SourceFilePath
            && SourceLineNumber == other.SourceLineNumber
            && CollectionProjectExpression == other.CollectionProjectExpression
            && ConcreteDictInstantiationName == other.ConcreteDictInstantiationName
            && GenerateExpression == other.GenerateExpression
            && ExpressionPropertyName == other.ExpressionPropertyName;
    }

    public override bool Equals(object obj) => Equals(obj as ForgeMethodModel);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (MethodName?.GetHashCode() ?? 0);
            hash = hash * 31 + (Accessibility?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceTypeFqn?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceTypeShortName?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceParameterName?.GetHashCode() ?? 0);
            hash = hash * 31 + (DestTypeFqn?.GetHashCode() ?? 0);
            hash = hash * 31 + (DestTypeShortName?.GetHashCode() ?? 0);
            hash = hash * 31 + (Construction?.GetHashCode() ?? 0);
            hash = hash * 31 + Assignments.Count;
            hash = hash * 31 + NestedMethods.Count;
            hash = hash * 31 + MethodKind.GetHashCode();
            hash = hash * 31 + (DestParameterName?.GetHashCode() ?? 0);
            hash = hash * 31 + (BeforeHookName?.GetHashCode() ?? 0);
            hash = hash * 31 + (AfterHookName?.GetHashCode() ?? 0);
            hash = hash * 31 + (CollectionProjectExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + (ConcreteDictInstantiationName?.GetHashCode() ?? 0);
            hash = hash * 31 + GenerateExpression.GetHashCode();
            hash = hash * 31 + (ExpressionPropertyName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
