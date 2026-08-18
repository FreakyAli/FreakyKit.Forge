
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Cross-class method sharing with [ForgeUses].
///
/// AddressShortForges defines the Address → AddressShortDto mapping separately.
/// PersonCrossClassForges references it via [ForgeUses] so the generator can discover
/// AddressShortForges.ToShortDto for the nested Address → AddressShortDto conversion.
///
/// A local forwarding method is provided because the sample's multi-file compilation
/// requires the method to be discoverable in the current class context during generation.
/// In single-file tests, [ForgeUses] resolves cross-class methods automatically.
/// </summary>
[Forge]
[ForgeUses(typeof(AddressShortForges))]
public static partial class PersonCrossClassForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonWithShortAddressDto ToCrossClassDto(Person source);

    // Local forwarding method — required for multi-file sample compilation.
    // In single-file tests, [ForgeUses] resolves this from AddressShortForges directly.
    public static partial AddressShortDto ToShortDto(Address source);
}
