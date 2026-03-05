
namespace FreakyKit.Forge.Samples;

/// <summary>
/// By default only properties are mapped. Set IncludeFields = true to also map public fields.
/// </summary>
[ForgeClass]
public static partial class FieldMappingForges
{
    [Forge(IncludeFields = true)]
    public static partial MeasurementDto ToMeasurementDto(Measurement source);
}
