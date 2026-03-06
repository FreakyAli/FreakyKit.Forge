using System;

namespace FreakyKit.Forge;

/// <summary>
/// Marks a static partial class as a Forge class.
/// The generator discovers and generates bodies for all forge methods within this class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ForgeAttribute : Attribute
{
    /// <summary>
    /// Controls which methods are treated as forge methods.
    /// Default is <see cref="ForgeMode.Implicit"/>.
    /// </summary>
    public ForgeMode Mode { get; set; } = ForgeMode.Implicit;

    /// <summary>
    /// When true, private forge methods are included. A single FKF011 info diagnostic is emitted on the class.
    /// When false (default), each private forge method emits FKF010 and is ignored.
    /// </summary>
    public bool ShouldIncludePrivate { get; set; } = false;
}
