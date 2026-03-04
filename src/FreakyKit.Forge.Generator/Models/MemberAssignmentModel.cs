namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// A property or field assignment in the generated method body.
/// </summary>
internal sealed class MemberAssignmentModel
{
    public string DestMemberName { get; }
    public string SourceExpression { get; }

    public MemberAssignmentModel(string destMemberName, string sourceExpression)
    {
        DestMemberName = destMemberName;
        SourceExpression = sourceExpression;
    }
}
