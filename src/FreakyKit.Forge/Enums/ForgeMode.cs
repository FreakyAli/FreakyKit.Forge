namespace FreakyKit.Forge;

/// <summary>
/// Controls which methods in a forge class are treated as forge methods.
/// </summary>
public enum ForgeMode
{
    /// <summary>
    /// All properly-shaped static partial methods in the forge class are treated as forge methods.
    /// A "properly-shaped" method is static, partial, non-generic, and either:
    /// (1) returns a non-void type with exactly one parameter (create mode), or
    /// (2) returns void with exactly two parameters (update mode).
    /// </summary>
    Implicit = 0,

    /// <summary>
    /// Only methods explicitly decorated with <see cref="ForgeAttribute"/> are treated as forge methods.
    /// Methods without the attribute emit FKF002.
    /// </summary>
    Explicit = 1
}
