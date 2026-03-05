
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Explicit mode: only methods decorated with [ForgeMethod] are treated as forge methods.
/// Other partial methods are ignored by the generator.
/// </summary>
[Forge(Mode = ForgeMode.Explicit)]
public static partial class ExplicitModeForges
{
    // This method IS generated because it has [ForgeMethod]
    [ForgeMethod]
    public static partial PersonExplicitDto ToExplicitDto(Person source);

    // This method is NOT generated — no [ForgeMethod] attribute in explicit mode
    // public static partial PersonDto ToDto(Person source);
}
