
namespace FreakyKit.Forge.Samples;

/// <summary>
/// GenerateReverse = true auto-generates a reverse mapping method.
/// You must specify ReverseName to name the generated method.
/// Note: reverse mappings are property-only (no fields, flattening, nested forging, etc.).
/// </summary>
// Use Explicit mode so the FromDto declaration isn't auto-detected as a second forge method
[ForgeClass(Mode = ForgeMode.Explicit)]
public static partial class ReverseForges
{
    [Forge(GenerateReverse = true, ReverseName = "FromDto")]
    public static partial PersonReverseDto ToReverseDto(Person source);

    // Declare the reverse method — the generator provides the implementation
    public static partial Person FromDto(PersonReverseDto source);
}
