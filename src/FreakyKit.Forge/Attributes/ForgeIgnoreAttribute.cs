using System;

namespace FreakyKit.Forge;

/// <summary>
/// Controls which side(s) of the mapping a <see cref="ForgeIgnoreAttribute"/> applies to.
/// </summary>
public enum ForgeIgnoreSide
{
    /// <summary>Exclude the member on both source and destination sides (default).</summary>
    Both = 0,
    /// <summary>Exclude only when the member appears on the source side. Suppresses FKF101.</summary>
    Source = 1,
    /// <summary>Exclude only when the member appears on the destination side. Suppresses FKF100.</summary>
    Destination = 2
}

/// <summary>
/// Excludes a property or field from forge mapping.
/// By default the member is skipped on both sides — no FKF100/FKF101 warnings are emitted.
/// Use <see cref="Side"/> to restrict exclusion to one side only.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ForgeIgnoreAttribute : Attribute
{
    /// <summary>
    /// Controls which side of the mapping this ignore applies to.
    /// Default is <see cref="ForgeIgnoreSide.Both"/>.
    /// </summary>
    public ForgeIgnoreSide Side { get; set; } = ForgeIgnoreSide.Both;
}
