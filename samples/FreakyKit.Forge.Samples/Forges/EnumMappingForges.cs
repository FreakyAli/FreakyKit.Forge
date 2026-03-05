
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Enum-to-enum mapping supports two strategies:
/// - Cast (default): (DestEnum)source.Value
/// - ByName: switch expression matching by member name
/// </summary>
[ForgeClass]
public static partial class EnumMappingForges
{
    // Default cast strategy
    public static partial PersonWithStatusDto ToCastDto(Person source);

    // ByName strategy — generates a switch expression
    [Forge(EnumMappingStrategy = ForgeEnumMapping.ByName)]
    public static partial PersonWithStatusDto ToByNameDto(Person source);
}
