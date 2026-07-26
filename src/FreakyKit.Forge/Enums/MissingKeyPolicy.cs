namespace FreakyKit.Forge;

/// <summary>
/// Controls behavior when a required dictionary key is not found during dict-to-object mapping.
/// </summary>
public enum MissingKeyPolicy
{
    /// <summary>
    /// Throw KeyNotFoundException if the key is not found (default, safest).
    /// </summary>
    Throw = 0,

    /// <summary>
    /// Assign the default value (e.g., default(int), null for reference types) if key is missing.
    /// </summary>
    UseDefault = 1,

    /// <summary>
    /// Skip the assignment entirely if key is missing (member left uninitialized or at default).
    /// </summary>
    Skip = 2,

    /// <summary>
    /// Assign null if key is missing (only valid for nullable destination types).
    /// </summary>
    ReturnNull = 3,
}
