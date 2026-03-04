using System;

namespace FreakyKit.Forge;

/// <summary>
/// Marks a static method as a type converter for forge mapping.
/// The method must be static, non-void, and take exactly one parameter.
/// The parameter type is the source type and the return type is the destination type.
/// When a member type mismatch is encountered, the generator checks for a matching converter.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ForgeConverterAttribute : Attribute
{
}
