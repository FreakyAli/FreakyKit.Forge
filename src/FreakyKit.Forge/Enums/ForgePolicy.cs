namespace FreakyKit.Forge;

/// <summary>
/// Tri-state policy for attribute properties that can be inherited from method-level settings or explicitly overridden.
/// </summary>
public enum ForgePolicy
{
    /// <summary>
    /// Inherit the setting from method-level configuration, or use the global default if unset at method level.
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// Explicitly set to true.
    /// </summary>
    True = 1,

    /// <summary>
    /// Explicitly set to false.
    /// </summary>
    False = 2
}
