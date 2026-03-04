using System.Collections.Generic;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Describes how to construct the destination type.
/// </summary>
internal sealed class ConstructionModel
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
}
