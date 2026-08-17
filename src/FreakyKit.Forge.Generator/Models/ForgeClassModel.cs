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
    public bool GenerateExtensionMethods { get; }
    public IReadOnlyList<ForgeMethodModel> Methods { get; }

    /// <summary>
    /// Containing type declarations from outermost to innermost, each as (accessibility, keyword, name).
    /// Empty for top-level classes.
    /// </summary>
    public IReadOnlyList<ContainingTypeInfo> ContainingTypes { get; }

    /// <summary>
    /// Fully-qualified names of forge classes included via [ForgeUses] attribute.
    /// Empty if no [ForgeUses] is present. Order determines priority for method lookup.
    /// </summary>
    public IReadOnlyList<string> IncludedForgeClasses { get; }

    /// <summary>
    /// Fully-qualified names of forge classes included via [ForgeIncludes] attribute.
    /// Empty if no [ForgeIncludes] is present. These classes supply base-type property
    /// assignments that are inlined into compatible consuming methods.
    /// </summary>
    public IReadOnlyList<string> IncludedProfileClasses { get; }

    public ForgeClassModel(
        string @namespace,
        string className,
        string accessibility,
        string fullyQualifiedName,
        bool hasErrors,
        IReadOnlyList<ForgeMethodModel> methods,
        IReadOnlyList<ContainingTypeInfo>? containingTypes = null,
        bool generateExtensionMethods = true,
        IReadOnlyList<string>? includedForgeClasses = null,
        IReadOnlyList<string>? includedProfileClasses = null)
    {
        Namespace = @namespace;
        ClassName = className;
        Accessibility = accessibility;
        FullyQualifiedName = fullyQualifiedName;
        HasErrors = hasErrors;
        GenerateExtensionMethods = generateExtensionMethods;
        Methods = methods;
        ContainingTypes = containingTypes ?? Array.Empty<ContainingTypeInfo>();
        IncludedForgeClasses = includedForgeClasses ?? Array.Empty<string>();
        IncludedProfileClasses = includedProfileClasses ?? Array.Empty<string>();
    }

    public bool Equals(ForgeClassModel other)
    {
        if (other is null) return false;
        return Namespace == other.Namespace
            && ClassName == other.ClassName
            && Accessibility == other.Accessibility
            && FullyQualifiedName == other.FullyQualifiedName
            && HasErrors == other.HasErrors
            && GenerateExtensionMethods == other.GenerateExtensionMethods
            && Methods.SequenceEqual(other.Methods)
            && ContainingTypes.SequenceEqual(other.ContainingTypes)
            && IncludedForgeClasses.SequenceEqual(other.IncludedForgeClasses)
            && IncludedProfileClasses.SequenceEqual(other.IncludedProfileClasses);
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
            hash = hash * 31 + GenerateExtensionMethods.GetHashCode();
            foreach (var included in IncludedForgeClasses)
                hash = hash * 31 + (included?.GetHashCode() ?? 0);
            foreach (var profile in IncludedProfileClasses)
                hash = hash * 31 + (profile?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
