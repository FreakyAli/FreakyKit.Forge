namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// A property or field assignment in the generated method body.
/// </summary>
internal sealed class MemberAssignmentModel
{
    public string DestMemberName { get; }
    public string SourceExpression { get; }
    public bool IgnoreIfNull { get; }
    public string? NullCheckExpression { get; }
    public bool IsInitOnly { get; }

    /// <summary>
    /// Expression-tree-compatible right-hand-side for this assignment, if the conversion
    /// can be expressed as a translatable expression (no null-conditional, no method calls
    /// that EF can't translate). Null when the imperative <see cref="SourceExpression"/> is
    /// not safe to use inside <c>Expression&lt;Func&lt;,&gt;&gt;</c>. Mutable because nested-forge
    /// inlining is resolved in a post-extraction pass once all method models exist.
    /// </summary>
    public string? ExpressionAssignment { get; set; }

    /// <summary>
    /// For nested-forge members: the name of the forge method whose expression body should be
    /// inlined into this assignment at codegen time. Null when not a nested-forge assignment.
    /// </summary>
    public string? NestedForgeMethodName { get; }

    /// <summary>
    /// For nested-forge members: the outer source-accessor expression (e.g. "source.Address") to
    /// substitute in for the nested method's source parameter when inlining. Null when not nested.
    /// </summary>
    public string? NestedForgeSourceAccessor { get; }

    /// <summary>
    /// True when the nested-forge source value is a reference type and needs a null-guard ternary
    /// in the expression body. Ignored unless <see cref="NestedForgeMethodName"/> is set.
    /// </summary>
    public bool NestedForgeSourceIsRefType { get; }

    /// <summary>
    /// For collection members whose element type requires nested-forge conversion: the name of the
    /// forge method that converts each element. Null otherwise.
    /// </summary>
    public string? CollectionElementForgeMethod { get; }

    /// <summary>
    /// For collection members: the outer source accessor expression (e.g. "source.Items"). Used
    /// both to substitute into the Select lambda and to wrap with an outer null guard. Null otherwise.
    /// </summary>
    public string? CollectionSourceAccessor { get; }

    /// <summary>
    /// For collection members: the LINQ materializer suffix to apply after Select (e.g. ".ToList()"
    /// or ".ToArray()"). Only translatable materializers are stored; non-translatable ones leave
    /// this null and emit FKF506.
    /// </summary>
    public string? CollectionMaterializer { get; }

    /// <summary>
    /// For collection members: true when the source collection itself is a reference type and
    /// needs an outer null guard ternary.
    /// </summary>
    public bool CollectionSourceIsRefType { get; }

    public MemberAssignmentModel(string destMemberName, string sourceExpression, bool ignoreIfNull = false, string? nullCheckExpression = null, bool isInitOnly = false, string? expressionAssignment = null, string? nestedForgeMethodName = null, string? nestedForgeSourceAccessor = null, bool nestedForgeSourceIsRefType = false, string? collectionElementForgeMethod = null, string? collectionSourceAccessor = null, string? collectionMaterializer = null, bool collectionSourceIsRefType = false)
    {
        DestMemberName = destMemberName;
        SourceExpression = sourceExpression;
        IgnoreIfNull = ignoreIfNull;
        NullCheckExpression = nullCheckExpression;
        IsInitOnly = isInitOnly;
        ExpressionAssignment = expressionAssignment;
        NestedForgeMethodName = nestedForgeMethodName;
        NestedForgeSourceAccessor = nestedForgeSourceAccessor;
        NestedForgeSourceIsRefType = nestedForgeSourceIsRefType;
        CollectionElementForgeMethod = collectionElementForgeMethod;
        CollectionSourceAccessor = collectionSourceAccessor;
        CollectionMaterializer = collectionMaterializer;
        CollectionSourceIsRefType = collectionSourceIsRefType;
    }
}
