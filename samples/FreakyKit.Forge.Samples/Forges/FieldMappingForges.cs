
namespace FreakyKit.Forge.Samples;

/// <summary>
/// By default only properties are mapped. Set ShouldIncludeFields = true to also map public fields.
/// </summary>
[Forge]
public static partial class FieldMappingForges
{
    [ForgeMethod(ShouldIncludeFields = true)]
    public static partial MeasurementDto ToMeasurementDto(Measurement source);
}
