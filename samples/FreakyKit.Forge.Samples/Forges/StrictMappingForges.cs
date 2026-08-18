
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Strict mapping: StrictMapping = true escalates unmapped/unused members
/// from warnings to build errors. Use for critical mappings where silent
/// type drift would cause data loss.
///
/// Try adding a property to StrictSource or StrictDest without updating the
/// other — the build fails with FKF110/FKF111.
/// </summary>
[Forge]
public static partial class StrictMappingForges
{
    [ForgeMethod(StrictMapping = true)]
    public static partial StrictDest ToStrictDto(StrictSource source);
    // Unmapped destination members → FKF110 (Error)
    // Unused source members → FKF111 (Error)
}
