
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Simplest usage: implicit mode maps all matching properties automatically.
/// No [Forge] attribute needed — any static partial method with the right shape is a forge method.
/// </summary>
[ForgeClass]
public static partial class BasicForges
{
    public static partial PersonDto ToPersonDto(Person source);
}
