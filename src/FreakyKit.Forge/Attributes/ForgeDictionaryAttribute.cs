using System;

namespace FreakyKit.Forge;

/// <summary>
/// Controls dictionary-to-object and object-to-dictionary mapping behavior.
/// Apply to a [ForgeMethod] when the method signature involves Dictionary&lt;string, T&gt; types.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ForgeDictionaryAttribute : Attribute
{
    /// <summary>
    /// Controls how dictionary keys are matched against destination member names.
    /// Default: Exact.
    /// </summary>
    public KeyCasingPolicy KeyCasing { get; set; } = KeyCasingPolicy.Exact;

    /// <summary>
    /// Controls behavior when a required key is not found during dict-to-object mapping.
    /// Default: Throw.
    /// </summary>
    public MissingKeyPolicy MissingKey { get; set; } = MissingKeyPolicy.Throw;

    /// <summary>
    /// Controls whether null values are included in object-to-dictionary conversion.
    /// Default: Include.
    /// </summary>
    public NullValuePolicy NullValue { get; set; } = NullValuePolicy.Include;
}
