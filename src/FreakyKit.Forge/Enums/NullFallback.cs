namespace FreakyKit.Forge;

/// <summary>
/// Controls what happens when a source member is null during nested forging.
/// </summary>
public enum NullFallback
{
    /// <summary>
    /// Return null for the destination member (default behavior).
    /// </summary>
    Null = 0,

    /// <summary>
    /// Construct a default instance of the destination type using its parameterless constructor,
    /// or an empty collection for collection types.
    /// </summary>
    DefaultConstruct = 1
}
