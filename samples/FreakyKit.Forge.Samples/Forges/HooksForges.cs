
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Before/after hooks let you run custom logic around mapping.
/// Convention: OnBefore{MethodName}(source) and OnAfter{MethodName}(source, result).
/// Declare them as partial methods and provide the implementation body.
/// </summary>
[ForgeClass]
public static partial class HooksForges
{
    public static partial PersonDto ToPersonDtoWithHooks(Person source);

    // Defining declarations (the generator detects these and calls them)
    static partial void OnBeforeToPersonDtoWithHooks(Person source);
    static partial void OnAfterToPersonDtoWithHooks(Person source, PersonDto result);

    // Implementing declarations (your custom logic)
    static partial void OnBeforeToPersonDtoWithHooks(Person source)
    {
        Console.WriteLine($"  [Hook] Before mapping person '{source.FirstName}'");
    }

    static partial void OnAfterToPersonDtoWithHooks(Person source, PersonDto result)
    {
        Console.WriteLine($"  [Hook] After mapping → PersonDto Id={result.Id}");
    }
}
