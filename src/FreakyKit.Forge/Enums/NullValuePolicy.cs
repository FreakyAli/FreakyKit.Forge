namespace FreakyKit.Forge;

/// <summary>
/// Controls behavior for null values when generating object-to-dictionary conversions.
/// </summary>
public enum NullValuePolicy
{
    /// <summary>
    /// Include all values in the dictionary, even if null (default).
    /// </summary>
    Include = 0,

    /// <summary>
    /// Skip null values; do not add them to the dictionary.
    /// </summary>
    Skip = 1,
}
