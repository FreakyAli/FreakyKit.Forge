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

    /// <summary>
    /// Controls what happens when a source member is null during nested forging with <see cref="ForgeMethodAttribute.AllowNestedForging"/>.
    ///
    /// PRECONDITIONS (all must be true, or this property is silently ignored):
    /// - Source member must be a reference type (class, interface, nullable struct)
    /// - Destination member must map to a nested-forged type (there must be a forge method for the type)
    /// - The parent forge method must have <see cref="ForgeMethodAttribute.AllowNestedForging"/> = true
    ///
    /// When conditions are met:
    /// - <see cref="NullFallback.Null"/> (default): generate <c>source.Member != null ? ToDto(...) : null</c>
    /// - <see cref="NullFallback.DefaultConstruct"/>: generate <c>source.Member != null ? ToDto(...) : new DestType()</c>
    ///
    /// When conditions are NOT met, this property has no effect (no diagnostic is emitted unless combined with <see cref="IgnoreIfNull"/>, which is always an error).
    /// </summary>
    public NullFallback NullFallback { get; set; }

    /// <summary>
    /// When true, the assignment is wrapped in a default-value check: the destination member
    /// is only assigned when the source value is not equal to its type's default (null, 0, false, Guid.Empty, etc.).
    /// Useful for PATCH/partial-update APIs where null or default values mean "don't update this field".
    /// </summary>
    public bool IgnoreIfDefault { get; set; }

    /// <summary>
    /// The name of a static method on the forge class that returns <c>bool</c> and accepts
    /// the source type as a parameter. The method determines whether this member should be assigned.
    /// When the method returns false, the assignment is skipped.
    ///
    /// Example:
    /// <code>
    /// [ForgeMap("Price", Condition = nameof(IsValidPrice))]
    /// public decimal? NewPrice { get; set; }
    ///
    /// private static bool IsValidPrice(UpdateDto source) => source.NewPrice > 0;
    /// </code>
    ///
    /// The method must:
    /// - Be static
    /// - Accept exactly one parameter of the source type
    /// - Return bool
    /// - Be declared on the same forge class (or discovered via [ForgeUses])
    /// </summary>
    public string? Condition { get; set; }

    public ForgeMapAttribute(string name)
    {
        Name = name;
    }
}
