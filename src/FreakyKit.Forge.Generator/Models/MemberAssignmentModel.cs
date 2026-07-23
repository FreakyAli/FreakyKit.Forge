using System;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// A property or field assignment in the generated method body.
/// </summary>
internal sealed class MemberAssignmentModel : IEquatable<MemberAssignmentModel>
{
    public string DestMemberName { get; }
    public string SourceExpression { get; }
    public bool IgnoreIfNull { get; }
    public string? NullCheckExpression { get; }
    public bool IsInitOnly { get; }
    public bool IgnoreIfDefault { get; }
    public string? ConditionMethodName { get; }
    public string? SourceMemberName { get; }
    public string? SourceMemberType { get; }

    /// <summary>
    /// Expression-tree-compatible right-hand-side for this assignment. Null when the imperative
    /// <see cref="SourceExpression"/> is not safe to use inside <c>Expression&lt;Func&lt;,&gt;&gt;</c>.
    /// Set during post-extraction once all method models exist.
    /// </summary>
    public string? ExpressionAssignment { get; }

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

    /// <summary>
    /// For nested-forge members: the null fallback strategy (0 = Null, 1 = DefaultConstruct).
    /// Defaults to 0 (Null). Only applies to reference type sources.
    /// </summary>
    public int NestedForgeNullFallback { get; }

    public MemberAssignmentModel(string destMemberName, string sourceExpression, bool ignoreIfNull = false, string? nullCheckExpression = null, bool isInitOnly = false, string? expressionAssignment = null, string? nestedForgeMethodName = null, string? nestedForgeSourceAccessor = null, bool nestedForgeSourceIsRefType = false, string? collectionElementForgeMethod = null, string? collectionSourceAccessor = null, string? collectionMaterializer = null, bool collectionSourceIsRefType = false, int nestedForgeNullFallback = 0, bool ignoreIfDefault = false, string? conditionMethodName = null, string? sourceMemberName = null, string? sourceMemberType = null)
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
        NestedForgeNullFallback = nestedForgeNullFallback;
        IgnoreIfDefault = ignoreIfDefault;
        ConditionMethodName = conditionMethodName;
        SourceMemberName = sourceMemberName;
        SourceMemberType = sourceMemberType;
    }

    public bool Equals(MemberAssignmentModel other)
    {
        if (other is null) return false;
        return DestMemberName == other.DestMemberName
            && SourceExpression == other.SourceExpression
            && IgnoreIfNull == other.IgnoreIfNull
            && NullCheckExpression == other.NullCheckExpression
            && IsInitOnly == other.IsInitOnly
            && ExpressionAssignment == other.ExpressionAssignment
            && NestedForgeMethodName == other.NestedForgeMethodName
            && NestedForgeSourceAccessor == other.NestedForgeSourceAccessor
            && NestedForgeSourceIsRefType == other.NestedForgeSourceIsRefType
            && CollectionElementForgeMethod == other.CollectionElementForgeMethod
            && CollectionSourceAccessor == other.CollectionSourceAccessor
            && CollectionMaterializer == other.CollectionMaterializer
            && CollectionSourceIsRefType == other.CollectionSourceIsRefType
            && NestedForgeNullFallback == other.NestedForgeNullFallback
            && IgnoreIfDefault == other.IgnoreIfDefault
            && ConditionMethodName == other.ConditionMethodName
            && SourceMemberName == other.SourceMemberName
            && SourceMemberType == other.SourceMemberType;
    }

    public override bool Equals(object obj) => Equals(obj as MemberAssignmentModel);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (DestMemberName?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + IgnoreIfNull.GetHashCode();
            hash = hash * 31 + (NullCheckExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + IsInitOnly.GetHashCode();
            hash = hash * 31 + (ExpressionAssignment?.GetHashCode() ?? 0);
            hash = hash * 31 + (NestedForgeMethodName?.GetHashCode() ?? 0);
            hash = hash * 31 + (NestedForgeSourceAccessor?.GetHashCode() ?? 0);
            hash = hash * 31 + NestedForgeSourceIsRefType.GetHashCode();
            hash = hash * 31 + (CollectionElementForgeMethod?.GetHashCode() ?? 0);
            hash = hash * 31 + (CollectionSourceAccessor?.GetHashCode() ?? 0);
            hash = hash * 31 + (CollectionMaterializer?.GetHashCode() ?? 0);
            hash = hash * 31 + CollectionSourceIsRefType.GetHashCode();
            hash = hash * 31 + NestedForgeNullFallback.GetHashCode();
            hash = hash * 31 + IgnoreIfDefault.GetHashCode();
            hash = hash * 31 + (ConditionMethodName?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceMemberName?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceMemberType?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public MemberAssignmentModel WithExpressionAssignment(string? expr) =>
        new(
            DestMemberName, SourceExpression, IgnoreIfNull, NullCheckExpression, IsInitOnly,
            expr,
            NestedForgeMethodName, NestedForgeSourceAccessor, NestedForgeSourceIsRefType,
            CollectionElementForgeMethod, CollectionSourceAccessor, CollectionMaterializer,
            CollectionSourceIsRefType, NestedForgeNullFallback, IgnoreIfDefault,
            ConditionMethodName, SourceMemberName, SourceMemberType);
}
