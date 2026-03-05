
namespace FreakyKit.Forge.Samples;

/// <summary>
/// [ForgeMap] lets you map between differently-named members.
/// Apply it on either the source or destination side.
/// Here PersonSummary.Name has [ForgeMap("FirstName")] — it reads from source.FirstName.
/// </summary>
[ForgeClass]
public static partial class ForgeMapForges
{
    public static partial PersonSummary ToPersonSummary(Person source);
}
