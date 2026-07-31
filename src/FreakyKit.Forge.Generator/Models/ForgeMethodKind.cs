namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Classifies the type of forge method being generated.
/// Create: Initialize a new destination object from a source.
/// Update: Modify an existing destination object in place.
/// CollectionProject: Generate LINQ expression for source collection elements.
/// DictionaryProject: Generate foreach transformation for dictionary entries.
/// DictionaryToObject: Convert Dictionary<string, T> to a domain object.
/// ObjectToDictionary: Convert a domain object to Dictionary<string, T>.
/// PolymorphicDispatch: Generate switch expression dispatching to derived-type forge methods.
/// </summary>
internal enum ForgeMethodKind
{
    Create,
    Update,
    CollectionProject,
    DictionaryProject,
    DictionaryToObject,
    ObjectToDictionary,
    PolymorphicDispatch
}
