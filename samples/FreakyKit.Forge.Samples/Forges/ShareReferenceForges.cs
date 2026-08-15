
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Reference semantics: same-type mutable collections are deep-copied by default.
/// Use ShareReference = true to share the same collection instance (faster, but
/// mutations leak across source and destination).
///
/// Per-member override: even when the method says "share all", individual members
/// can opt back into deep-copying via [ForgeMap(ShareReference = ForgePolicy.False)].
/// </summary>
[Forge]
public static partial class ShareReferenceForges
{
    // Method-level: share all same-type collections by reference
    [ForgeMethod(ShareReference = ForgePolicy.True)]
    public static partial PersonTagsDto ToSharedDto(Person source);
    // Generates:
    //   __result.Tags = source.Tags;                                        // shared (method-level)
    //   __result.Orders = source.Orders != null ? new List<Order>(source.Orders) : null;  // copied (per-member override)
}
