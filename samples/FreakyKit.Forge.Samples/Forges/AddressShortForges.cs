
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Standalone forge class for Address → AddressShortDto.
/// Used by PersonCrossClassForges via [ForgeUses].
/// </summary>
[Forge]
public static partial class AddressShortForges
{
    public static partial AddressShortDto ToShortDto(Address source);
}
