using System;

namespace FreakyKit.Forge;

/// <summary>
/// Maps a property or field to a differently-named member on the counterpart type.
/// When applied to a source member, the value specifies the destination member name.
/// When applied to a destination member, the value specifies the source member name.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ForgeMapAttribute : Attribute
{
    /// <summary>
    /// The name of the counterpart member to map to/from.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The default value to use when the source is null during a Nullable&lt;T&gt; to T mapping.
    /// When set, generates <c>source.Prop ?? defaultValue</c> instead of <c>source.Prop.Value</c>,
    /// preventing InvalidOperationException at runtime.
    /// </summary>
    public object? DefaultValue { get; set; }

    public ForgeMapAttribute(string name)
    {
        Name = name;
    }
}
