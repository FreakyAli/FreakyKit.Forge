
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Update methods: void return with two parameters (source, existing).
/// Instead of creating a new object, they update an existing destination in-place.
/// </summary>
[Forge]
public static partial class UpdateForges
{
    public static partial void UpdatePerson(Person source, PersonMutableDto existing);

    // Defining declaration (the generator detects this and calls it)
    static partial void OnAfterUpdatePerson(Person source, PersonMutableDto existing);

    // Implementing declaration (your custom logic)
    static partial void OnAfterUpdatePerson(Person source, PersonMutableDto existing)
    {
        existing.LastUpdated = DateTime.UtcNow;
    }
}
