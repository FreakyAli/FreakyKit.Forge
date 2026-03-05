
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Explicit mode: only methods decorated with [Forge] are treated as forge methods.
/// Other partial methods are ignored by the generator.
/// </summary>
[ForgeClass(Mode = ForgeMode.Explicit)]
public static partial class ExplicitModeForges
{
    // This method IS generated because it has [Forge]
    [Forge]
    public static partial PersonExplicitDto ToExplicitDto(Person source);

    // This method is NOT generated — no [Forge] attribute in explicit mode
    // public static partial PersonDto ToDto(Person source);
}
