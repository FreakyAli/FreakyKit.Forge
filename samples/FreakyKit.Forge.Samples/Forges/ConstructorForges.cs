
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Constructor mapping: the generator picks a constructor whose parameters
/// can all be satisfied from source members. Remaining settable properties
/// are assigned after construction.
/// PersonRecordDto(string name, int age) ← source.Name, source.Age via ctor
/// PersonRecordDto.Email ← source.Email via property setter
/// </summary>
[ForgeClass]
public static partial class ConstructorForges
{
    public static partial PersonRecordDto ToRecordDto(ConstructorSource source);
}

public class ConstructorSource
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Email { get; set; } = "";
}
