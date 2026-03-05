
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Automatic nullable ↔ non-nullable conversion:
/// - int? → int uses .Value (emits FKF201 warning)
/// - int → int? is assigned directly
/// </summary>
[ForgeClass]
public static partial class NullableForges
{
    public static partial PersonScoreDto ToScoreDto(Person source);
}
