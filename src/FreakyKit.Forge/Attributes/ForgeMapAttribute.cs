using System;

namespace FreakyKit.Forge;

/// <summary>
/// Maps a property or field to a differently-named member on the counterpart type.
/// When applied to a source member, the value specifies the destination member name.
/// When applied to a destination member, the value specifies the source member name.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
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

    /// <summary>
    /// When true, the assignment is wrapped in a null check: the destination member
    /// is only assigned when the source value is not null. Useful for update methods
    /// where you want to preserve existing values when the source is null.
    /// </summary>
    public bool IgnoreIfNull { get; set; }

    /// <summary>
    /// Per-member override of <see cref="ForgeMethodAttribute.ShareReference"/> for same-type
    /// mutable collection members. When set to <c>true</c>, this member is reference-shared
    /// regardless of the method-level setting. When set to <c>false</c>, this member is
    /// copy-constructed regardless of the method-level setting. Leave unset (the default) to
    /// inherit from <see cref="ForgeMethodAttribute.ShareReference"/>.
    ///
    /// Precedence: destination-side <c>[ForgeMap]</c> &gt; source-side <c>[ForgeMap]</c> &gt;
    /// <c>[ForgeMethod]</c> &gt; default (false / copy).
    ///
    /// When source-side and destination-side both set this with different values, FKF313 is
    /// emitted and the destination-side value wins.
    /// </summary>
    public bool ShareReference { get; set; }

    public ForgeMapAttribute(string name)
    {
        Name = name;
    }
}
