using System;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Extracted, equatable model for a single polymorphic dispatch arm.
/// Maps a derived source type to a named forge method.
/// </summary>
internal sealed class PolymorphicMappingModel : IEquatable<PolymorphicMappingModel>
{
    public string DerivedSourceTypeFqn { get; }
    public string DerivedSourceTypeShortName { get; }
    public string MethodName { get; }
    public string MethodReturnTypeFqn { get; }

    public PolymorphicMappingModel(
        string derivedSourceTypeFqn,
        string derivedSourceTypeShortName,
        string methodName,
        string methodReturnTypeFqn)
    {
        DerivedSourceTypeFqn = derivedSourceTypeFqn;
        DerivedSourceTypeShortName = derivedSourceTypeShortName;
        MethodName = methodName;
        MethodReturnTypeFqn = methodReturnTypeFqn;
    }

    public bool Equals(PolymorphicMappingModel other)
    {
        if (other is null) return false;
        return DerivedSourceTypeFqn == other.DerivedSourceTypeFqn
            && DerivedSourceTypeShortName == other.DerivedSourceTypeShortName
            && MethodName == other.MethodName
            && MethodReturnTypeFqn == other.MethodReturnTypeFqn;
    }

    public override bool Equals(object obj) => Equals(obj as PolymorphicMappingModel);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (DerivedSourceTypeFqn?.GetHashCode() ?? 0);
            hash = hash * 31 + (DerivedSourceTypeShortName?.GetHashCode() ?? 0);
            hash = hash * 31 + (MethodName?.GetHashCode() ?? 0);
            hash = hash * 31 + (MethodReturnTypeFqn?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
