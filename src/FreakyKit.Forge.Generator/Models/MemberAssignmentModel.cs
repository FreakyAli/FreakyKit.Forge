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

    public MemberAssignmentModel(string destMemberName, string sourceExpression, bool ignoreIfNull = false, string? nullCheckExpression = null)
    {
        DestMemberName = destMemberName;
        SourceExpression = sourceExpression;
        IgnoreIfNull = ignoreIfNull;
        NullCheckExpression = nullCheckExpression;
    }
}
