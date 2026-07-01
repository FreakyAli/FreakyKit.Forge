using System;
using System.Collections.Generic;
using System.Linq;

namespace FreakyKit.Forge.Generator.Models;

/// <summary>
/// Extracted, equatable model for a forge class discovered during generation.
/// Carries all information needed to generate the partial class implementation.
/// </summary>
internal sealed class ForgeClassModel : IEquatable<ForgeClassModel>
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
        ContainingTypes = containingTypes ?? Array.Empty<ContainingTypeInfo>();
    }

    public bool Equals(ForgeClassModel other)
    {
        if (other is null) return false;
        return Namespace == other.Namespace
            && ClassName == other.ClassName
            && Accessibility == other.Accessibility
            && FullyQualifiedName == other.FullyQualifiedName
            && HasErrors == other.HasErrors
            && Methods.SequenceEqual(other.Methods)
            && ContainingTypes.SequenceEqual(other.ContainingTypes);
    }

    public override bool Equals(object obj) => Equals(obj as ForgeClassModel);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (Namespace?.GetHashCode() ?? 0);
            hash = hash * 31 + (ClassName?.GetHashCode() ?? 0);
            hash = hash * 31 + (Accessibility?.GetHashCode() ?? 0);
            hash = hash * 31 + (FullyQualifiedName?.GetHashCode() ?? 0);
            hash = hash * 31 + HasErrors.GetHashCode();
            return hash;
        }
    }
}
