namespace FreakyKit.Forge;

/// <summary>
/// Controls how enum-to-enum mappings are generated when source and destination
/// have members with the same name but different enum types.
/// </summary>
public enum ForgeEnumMapping
{
    /// <summary>
    /// Generates a direct cast: <c>(DestEnum)source.Value</c>.
    /// This is the default and works when both enums share the same underlying integer values.
    /// </summary>
    Cast = 0,

    /// <summary>
    /// Generates a switch expression that maps each source enum member to the
    /// destination enum member with the same name.
    /// This is safer when the enums may have different underlying values.
    /// </summary>
    ByName = 1
}
