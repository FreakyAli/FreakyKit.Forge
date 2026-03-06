using System;

namespace FreakyKit.Forge;

/// <summary>
/// Excludes a property or field from forge mapping.
/// Members marked with this attribute are skipped entirely — no FKF100/FKF101 warnings are emitted.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ForgeIgnoreAttribute : Attribute
{
}
