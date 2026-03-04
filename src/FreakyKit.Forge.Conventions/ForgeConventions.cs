namespace FreakyKit.Forge.Conventions;

/// <summary>
/// Optional naming and structural conventions for FreakyKit.Forge.
/// Provides guidance for organizing forge classes and methods.
/// No semantic changes to the forge pipeline — purely advisory.
/// </summary>
/// <remarks>
/// Stub implementation — conventions are advisory only in v1.
/// Future versions may add convention-checking analyzers.
/// </remarks>
public static class ForgeConventions
{
    /// <summary>
    /// Recommended suffix for forge class names.
    /// e.g., <c>PersonForges</c>, <c>OrderForges</c>.
    /// </summary>
    public const string RecommendedClassSuffix = "Forges";

    /// <summary>
    /// Recommended prefix pattern for forge method names mapping to a DTO.
    /// e.g., <c>ToDto</c>, <c>ToViewModel</c>.
    /// </summary>
    public const string RecommendedMethodPrefix = "To";

    /// <summary>
    /// Returns the recommended forge class name for a given source or domain type name.
    /// </summary>
    public static string ForgeClassName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return RecommendedClassSuffix;
        return typeName + RecommendedClassSuffix;
    }

    /// <summary>
    /// Returns the recommended forge method name for mapping to a destination type.
    /// </summary>
    public static string ForgeMethodName(string destinationTypeName)
    {
        if (string.IsNullOrWhiteSpace(destinationTypeName))
            return RecommendedMethodPrefix;
        return RecommendedMethodPrefix + destinationTypeName;
    }
}
