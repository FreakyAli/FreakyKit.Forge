using System;

namespace FreakyKit.Forge;

/// <summary>
/// Marks a static partial method as a forge method and configures its mapping behavior.
/// In <see cref="ForgeMode.Explicit"/> mode on the containing class, this attribute is required.
/// In <see cref="ForgeMode.Implicit"/> mode, this attribute is optional and provides per-method configuration.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ForgeMethodAttribute : Attribute
{
    /// <summary>
    /// When true, fields from the source and destination types are included in member discovery.
    /// Emits FKF401 (info) when enabled. Fields are ignored by default (FKF400).
    /// </summary>
    public bool ShouldIncludeFields { get; set; } = false;

    /// <summary>
    /// When true, the generator will call an existing forge method to convert nested types
    /// whose names match but whose types differ.
    /// When false (default), a type mismatch where a forge method exists emits FKF300.
    /// </summary>
    public bool AllowNestedForging { get; set; } = false;

    /// <summary>
    /// Controls how enum-to-enum mappings are generated when source and destination
    /// members share the same name but have different enum types.
    /// Default is <see cref="ForgeMapping.Cast"/>.
    /// </summary>
    public ForgeMapping MappingStrategy { get; set; } = ForgeMapping.Cast;

    /// <summary>
    /// When true, the generator will attempt to flatten nested properties.
    /// For example, a destination member named "AddressCity" will be mapped to "source.Address.City"
    /// if no direct match is found. Only one level of nesting is supported.
    /// </summary>
    public bool AllowFlattening { get; set; } = false;

    /// <summary>
    /// When true, all property assignments are wrapped in a null check:
    /// the destination member is only assigned when the source value is not null.
    /// Can be overridden per-member using <see cref="ForgeMapAttribute.IgnoreIfNull"/>.
    /// </summary>
    public bool IgnoreIfNull { get; set; } = false;

    /// <summary>
    /// When true, unmapped destination members (FKF110) and unused source members (FKF111)
    /// are reported as errors instead of warnings, ensuring complete mapping coverage.
    /// This catches mapping drift when source or destination types change.
    /// </summary>
    public bool StrictMapping { get; set; } = false;

    /// <summary>
    /// When true, the generator emits an additional static property of type
    /// <c>Expression&lt;Func&lt;TSource, TDest&gt;&gt;</c> alongside the partial method body,
    /// suitable for use in <c>IQueryable.Select(...)</c> against EF Core / LINQ providers.
    /// The property is named <c>{MethodName}Expression</c>.
    /// Emits FKF504 (error) if set on an update method, FKF505 (warning) if before/after hooks are present.
    /// </summary>
    public bool GenerateExpression { get; set; } = false;

    /// <summary>
    /// Controls how Forge handles same-type mutable collection members (e.g. <c>List&lt;T&gt;</c> on
    /// both source and destination). When false (default), the generator emits a copy-constructor
    /// expression (<c>new List&lt;T&gt;(source.X)</c>) so the destination owns an independent
    /// collection instance. When true, the generator emits direct reference assignment
    /// (<c>dto.Tags = source.Tags</c>), sharing the same list between source and destination —
    /// faster and allocation-free, but mutations to the destination will affect the source.
    ///
    /// Affects: <c>List&lt;T&gt;</c>, <c>Dictionary&lt;K,V&gt;</c>, <c>HashSet&lt;T&gt;</c>,
    /// <c>T[]</c>, and their interfaces (<c>IList</c>, <c>ICollection</c>, etc.).
    /// Does not affect immutable types (<c>string</c>, <c>ImmutableArray</c>, etc.) or same-type
    /// custom classes (those use <c>AllowNestedForging</c> to control deep-copy).
    ///
    /// Can be overridden per-member via <see cref="ForgeMapAttribute.ShareReference"/>.
    /// Emits FKF311 (info) for each member that is reference-shared per this flag.
    /// </summary>
    public bool ShareReference { get; set; } = false;
}
