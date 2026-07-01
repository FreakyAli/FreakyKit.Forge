using System;
using System.Collections.Generic;
using System.Linq;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Describes how to construct the destination type.
/// </summary>
internal sealed class ConstructionModel : IEquatable<ConstructionModel>
{
    public ConstructionKind Kind { get; }

    /// <summary>
    /// For <see cref="ConstructionKind.Parameterized"/>: constructor parameter assignments in order.
    /// </summary>
    public IReadOnlyList<ConstructorArgModel> ConstructorArgs { get; }

    public ConstructionModel(ConstructionKind kind, IReadOnlyList<ConstructorArgModel> constructorArgs)
    {
        Kind = kind;
        ConstructorArgs = constructorArgs;
    }

    public bool Equals(ConstructionModel other)
    {
        if (other is null) return false;
        if (Kind != other.Kind) return false;
        return ConstructorArgs.SequenceEqual(other.ConstructorArgs);
    }

    public override bool Equals(object obj) => Equals(obj as ConstructionModel);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Kind.GetHashCode();
            foreach (var arg in ConstructorArgs)
                hash = hash * 31 + (arg?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
