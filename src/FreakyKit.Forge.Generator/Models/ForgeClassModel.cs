using System.Collections.Generic;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Extracted, equatable model for a forge class discovered during generation.
/// Carries all information needed to generate the partial class implementation.
/// </summary>
internal sealed class ForgeClassModel
{
    public string Namespace { get; }
    public string ClassName { get; }
    public string Accessibility { get; }
    public string FullyQualifiedName { get; }
    public bool HasErrors { get; }
    public IReadOnlyList<ForgeMethodModel> Methods { get; }

    /// <summary>
    /// Containing type declarations from outermost to innermost, each as (accessibility, keyword, name).
    /// Empty for top-level classes.
    /// </summary>
    public IReadOnlyList<ContainingTypeInfo> ContainingTypes { get; }

    public ForgeClassModel(
        string @namespace,
        string className,
        string accessibility,
        string fullyQualifiedName,
        bool hasErrors,
        IReadOnlyList<ForgeMethodModel> methods,
        IReadOnlyList<ContainingTypeInfo>? containingTypes = null)
    {
        Namespace = @namespace;
        ClassName = className;
        Accessibility = accessibility;
        FullyQualifiedName = fullyQualifiedName;
        HasErrors = hasErrors;
        Methods = methods;
        ContainingTypes = containingTypes ?? System.Array.Empty<ContainingTypeInfo>();
    }
}
