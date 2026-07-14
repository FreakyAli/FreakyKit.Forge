namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Describes which constructor was selected for the destination type.
/// None: No viable constructor found; generation will fail.
/// Parameterless: The type has a public parameterless constructor (new DestType()).
/// Parameterized: The type's constructor requires parameters matched from source members.
/// </summary>
internal enum ConstructionKind
{
    None,
    Parameterless,
    Parameterized
}
