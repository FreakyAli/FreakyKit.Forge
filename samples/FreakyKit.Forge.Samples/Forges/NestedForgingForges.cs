
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Nested forging: when source and destination have different nested types,
/// the generator calls another forge method in the same class to convert them.
/// </summary>
[Forge]
public static partial class NestedForgingForges
{
    [ForgeMethod(AllowNestedForging = true)]
    public static partial PersonWithAddressDto ToWithAddress(Person source);

    // This method is called automatically for HomeAddress: Address → AddressDto
    public static partial AddressDto ToAddressDto(Address source);
}
