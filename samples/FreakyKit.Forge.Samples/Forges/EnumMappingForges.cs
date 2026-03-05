
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Enum-to-enum mapping supports two strategies:
/// - Cast (default): (DestEnum)source.Value
/// - ByName: switch expression matching by member name
/// </summary>
[Forge]
public static partial class EnumMappingForges
{
    // Default cast strategy
    public static partial PersonWithStatusDto ToCastDto(Person source);

    // ByName strategy — generates a switch expression
    [ForgeMethod(MappingStrategy = ForgeMapping.ByName)]
    public static partial PersonWithStatusDto ToByNameDto(Person source);
}
