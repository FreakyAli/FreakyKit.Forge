using System;

namespace FreakyKit.Forge;

/// <summary>
/// Declares a polymorphic type mapping arm on a forge method. The method becomes a pure dispatch
/// method that generates a switch expression over derived source types.
/// Each attribute maps a derived source type to an explicitly named forge method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ForgePolymorphicAttribute : Attribute
{
    /// <summary>
    /// The derived source type to match in the switch expression pattern.
    /// Must be assignable from the method's source parameter type.
    /// </summary>
    public Type DerivedSourceType { get; }

    /// <summary>
    /// The name of the forge method to call for this derived type.
    /// Must exist in the same forge class or in a [ForgeUses] included class.
    /// </summary>
    public string MappingMethodName { get; }

    /// <summary>
    /// Initializes a new polymorphic mapping arm.
    /// </summary>
    /// <param name="derivedSourceType">The derived source type to match.</param>
    /// <param name="mappingMethodName">The name of the method to dispatch to (use nameof).</param>
    public ForgePolymorphicAttribute(Type derivedSourceType, string mappingMethodName)
    {
        DerivedSourceType = derivedSourceType ?? throw new ArgumentNullException(nameof(derivedSourceType));
        MappingMethodName = mappingMethodName ?? throw new ArgumentNullException(nameof(mappingMethodName));
    }
}
