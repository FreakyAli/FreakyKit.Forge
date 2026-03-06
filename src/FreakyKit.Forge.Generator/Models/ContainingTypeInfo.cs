namespace FreakyKit.Forge.Generator.Models;

internal sealed class ContainingTypeInfo
{
    public string Accessibility { get; }
    public string Keyword { get; }
    public string Name { get; }

    public ContainingTypeInfo(string accessibility, string keyword, string name)
    {
        Accessibility = accessibility;
        Keyword = keyword;
        Name = name;
    }
}
