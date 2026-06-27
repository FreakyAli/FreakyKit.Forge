using System;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// A single argument to pass to the destination constructor.
/// </summary>
internal sealed class ConstructorArgModel : IEquatable<ConstructorArgModel>
{
    public string ParameterName { get; }
    public string SourceExpression { get; }

    /// <summary>
    /// Expression-tree-compatible form of <see cref="SourceExpression"/>, or null when the conversion
    /// has no translatable encoding (e.g. nested type conversion via converter or nested forge).
    /// </summary>
    public string? ExpressionAssignment { get; }

    public ConstructorArgModel(string parameterName, string sourceExpression, string? expressionAssignment = null)
    {
        ParameterName = parameterName;
        SourceExpression = sourceExpression;
        ExpressionAssignment = expressionAssignment;
    }

    public bool Equals(ConstructorArgModel other)
    {
        if (other is null) return false;
        return ParameterName == other.ParameterName
            && SourceExpression == other.SourceExpression
            && ExpressionAssignment == other.ExpressionAssignment;
    }

    public override bool Equals(object obj) => Equals(obj as ConstructorArgModel);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (ParameterName?.GetHashCode() ?? 0);
            hash = hash * 31 + (SourceExpression?.GetHashCode() ?? 0);
            hash = hash * 31 + (ExpressionAssignment?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
