
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Flattening maps nested source properties to flat destination members.
/// source.HomeAddress.City → result.HomeAddressCity (prefix match, one level deep).
/// </summary>
[Forge]
public static partial class FlatteningForges
{
    [ForgeMethod(AllowFlattening = true)]
    public static partial PersonFlatDto ToFlatDto(Person source);
}
