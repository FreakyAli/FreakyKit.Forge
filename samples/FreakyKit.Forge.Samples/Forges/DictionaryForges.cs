
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Dictionary mapping: convert between Dictionary&lt;string, object&gt; and domain objects.
/// Use [ForgeDictionary] to control key casing, missing key handling, and null values.
/// </summary>
[Forge]
public static partial class DictionaryForges
{
    // Object → dictionary (skip null values in output)
    [ForgeMethod]
    [ForgeDictionary(NullValue = NullValuePolicy.Skip)]
    public static partial Dictionary<string, object> ToDict(AppSettings settings);

    // Dictionary → object (camelCase keys, use defaults for missing keys)
    [ForgeMethod]
    [ForgeDictionary(KeyCasing = KeyCasingPolicy.CamelCase, MissingKey = MissingKeyPolicy.UseDefault)]
    public static partial AppSettings FromDict(Dictionary<string, object> dict);
}
