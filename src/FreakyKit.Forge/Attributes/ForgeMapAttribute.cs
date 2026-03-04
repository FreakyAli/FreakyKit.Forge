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

    public ForgeMapAttribute(string name)
    {
        Name = name;
    }
}
