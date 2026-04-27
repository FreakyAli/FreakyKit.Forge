using System.Collections.Generic;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Extracted, equatable model for a single forge method.
/// Carries all information needed to generate the method body.
/// </summary>
internal sealed class ForgeMethodModel
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
        string? concreteDictInstantiationName = null)
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
    }
}
