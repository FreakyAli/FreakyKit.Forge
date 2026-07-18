using System;

namespace FreakyKit.Forge;

/// <summary>
/// Declares that a forge class uses (includes) other forge classes for method discovery.
/// When a nested forge method lookup fails in the current class, the generator searches
/// included classes in the order specified.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ForgeUsesAttribute : Attribute
{
    /// <summary>
    /// The forge classes to include for method discovery. Order determines priority:
    /// the first class that has a matching method wins.
    /// </summary>
    public Type[] ForgeClasses { get; }

    /// <summary>
    /// Initializes a new instance with the specified forge classes.
    /// </summary>
    /// <param name="forgeClasses">One or more forge class types to include.</param>
    public ForgeUsesAttribute(params Type[] forgeClasses)
    {
        ForgeClasses = forgeClasses ?? Array.Empty<Type>();
    }
}
