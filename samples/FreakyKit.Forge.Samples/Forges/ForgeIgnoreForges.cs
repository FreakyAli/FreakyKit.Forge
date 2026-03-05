
namespace FreakyKit.Forge.Samples;

/// <summary>
/// [ForgeIgnore] excludes a member from mapping entirely.
/// Here we use a wrapper source type where InternalNotes is ignored.
/// </summary>
[ForgeClass]
public static partial class ForgeIgnoreForges
{
    public static partial PersonPublicDto ToPublicDto(PersonIgnored source);
}

/// <summary>
/// Wrapper with [ForgeIgnore] on InternalNotes so it's never mapped.
/// </summary>
public class PersonIgnored
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    [ForgeIgnore]
    public string InternalNotes { get; set; } = "";
}
