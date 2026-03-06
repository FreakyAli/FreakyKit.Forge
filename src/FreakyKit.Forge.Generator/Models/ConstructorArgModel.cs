namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// A single argument to pass to the destination constructor.
/// </summary>
internal sealed class ConstructorArgModel
{
    public string ParameterName { get; }
    public string SourceExpression { get; }

    public ConstructorArgModel(string parameterName, string sourceExpression)
    {
        ParameterName = parameterName;
        SourceExpression = sourceExpression;
    }
}
