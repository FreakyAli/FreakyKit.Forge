
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Cross-class method sharing with [ForgeUses].
///
/// AddressShortForges defines the Address → AddressShortDto mapping separately.
/// PersonCrossClassForges references it via [ForgeUses] and also has
/// the nested forge method locally for the Address → AddressShortDto conversion.
///
/// In test scenarios (single compilation unit), [ForgeUses] resolves cross-class
/// methods automatically. In multi-file projects, declaring the nested method
/// locally is the reliable pattern.
/// </summary>
[Forge]
[ForgeUses(typeof(AddressShortForges))]
public static partial class PersonCrossClassForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonWithShortAddressDto ToCrossClassDto(Person source);

    // Local nested forge method for Address → AddressShortDto
    public static partial AddressShortDto ToShortDto(Address source);
}
