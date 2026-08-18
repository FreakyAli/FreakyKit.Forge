using System;

namespace FreakyKit.Forge;

/// <summary>
/// Declares that a forge class inherits property assignments from the specified forge classes.
/// When a forge method maps DerivedSource → DerivedDto and an included class has a method
/// mapping BaseSource → BaseDto (where DerivedSource : BaseSource and DerivedDto : BaseDto),
/// the included method's assignments are merged into the derived method. Local assignments
/// take precedence over inherited ones.
/// </summary>
/// <remarks>
/// Unlike <see cref="ForgeUsesAttribute"/> (which enables cross-class method discovery for
/// nested forging), <c>[ForgeIncludes]</c> inlines the base-type property mappings directly
/// into the consuming method's generated body — no runtime delegation or separate method call.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ForgeIncludesAttribute : Attribute
{
    /// <summary>
    /// The forge classes whose compatible methods supply base-type property assignments.
    /// Order determines priority when multiple included classes map the same destination member:
    /// the first class wins.
    /// </summary>
    public Type[] ForgeClasses { get; }

    /// <summary>
    /// Initializes a new instance with the specified forge classes to include.
    /// </summary>
    /// <param name="forgeClasses">One or more forge class types to include for assignment inheritance.</param>
    public ForgeIncludesAttribute(params Type[] forgeClasses)
    {
        ForgeClasses = forgeClasses ?? Array.Empty<Type>();
    }
}
