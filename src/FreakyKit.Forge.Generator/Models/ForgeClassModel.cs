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
    public string FullyQualifiedName { get; }
    public bool HasErrors { get; }
    public IReadOnlyList<ForgeMethodModel> Methods { get; }

    public ForgeClassModel(
        string @namespace,
        string className,
        string fullyQualifiedName,
        bool hasErrors,
        IReadOnlyList<ForgeMethodModel> methods)
    {
        Namespace = @namespace;
        ClassName = className;
        FullyQualifiedName = fullyQualifiedName;
        HasErrors = hasErrors;
        Methods = methods;
    }
}
