
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Conditional mapping: skip assignments based on null checks, default checks,
/// or custom conditions. Useful for PATCH/update APIs.
/// </summary>
[Forge]
public static partial class ConditionalForges
{
    // Method-level IgnoreIfNull: only assign when source value is not null.
    // Perfect for partial updates where null means "don't change".
    [ForgeMethod(IgnoreIfNull = ForgePolicy.True)]
    public static partial void PatchPerson(PersonPatchDto source, Person existing);
    // Generates:
    //   if (source.FirstName != null) existing.FirstName = source.FirstName;
    //   if (source.LastName != null) existing.LastName = source.LastName;
    //   ...
}
