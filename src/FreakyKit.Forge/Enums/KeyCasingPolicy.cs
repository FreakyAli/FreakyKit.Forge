namespace FreakyKit.Forge;

/// <summary>
/// Controls how dictionary keys are matched against destination member names.
/// </summary>
public enum KeyCasingPolicy
{
    /// <summary>
    /// Match member name exactly (e.g., "Name" matches "Name" but not "name").
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Case-insensitive matching (e.g., "Name", "name", "NAME" all match).
    /// </summary>
    IgnoreCase = 1,

    /// <summary>
    /// Transform member name to camelCase before lookup (e.g., "PersonFirstName" → "personFirstName").
    /// </summary>
    CamelCase = 2,

    /// <summary>
    /// Transform member name to snake_case before lookup (e.g., "PersonFirstName" → "person_first_name").
    /// </summary>
    SnakeCase = 3,
}
