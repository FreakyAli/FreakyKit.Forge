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

}
