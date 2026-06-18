namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// A single argument to pass to the destination constructor.
/// </summary>
internal sealed class ConstructorArgModel
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
}
