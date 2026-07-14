using System;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Metadata about the containing type (class) that holds a forge method.
/// Used to reconstruct the original class declaration in generated code.
/// </summary>
internal sealed class ContainingTypeInfo : IEquatable<ContainingTypeInfo>
{
    /// <summary>The accessibility modifier (public, internal, private, etc.).</summary>
    public string Accessibility { get; }

    /// <summary>The type keyword (class, struct, record, etc.).</summary>
    public string Keyword { get; }

    /// <summary>The type name.</summary>
    public string Name { get; }

    public ContainingTypeInfo(string accessibility, string keyword, string name)
    {
        Accessibility = accessibility;
        Keyword = keyword;
        Name = name;
    }

    public bool Equals(ContainingTypeInfo other)
    {
        if (other is null) return false;
        return Accessibility == other.Accessibility
            && Keyword == other.Keyword
            && Name == other.Name;
    }

    public override bool Equals(object obj) => Equals(obj as ContainingTypeInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (Accessibility?.GetHashCode() ?? 0);
            hash = hash * 31 + (Keyword?.GetHashCode() ?? 0);
            hash = hash * 31 + (Name?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
