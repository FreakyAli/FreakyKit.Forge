
namespace FreakyKit.Forge.Samples;

/// <summary>
/// IncludePrivateMethods = true lets the generator implement non-public static partial methods.
/// Useful for internal helpers that shouldn't be part of the public API.
/// </summary>
[ForgeClass(IncludePrivateMethods = true)]
public static partial class PrivateMethodForges
{
    // Public method — always generated
    public static partial PersonDto ToPublicDto(Person source);

    // Internal method — only generated because IncludePrivateMethods = true
    internal static partial PersonInternalDto ToInternalDto(Person source);

    // Expose the internal method for the demo
    public static PersonInternalDto MapInternal(Person source) => ToInternalDto(source);
}
