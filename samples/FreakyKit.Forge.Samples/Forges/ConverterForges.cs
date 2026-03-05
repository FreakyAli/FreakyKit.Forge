
namespace FreakyKit.Forge.Samples;

/// <summary>
/// [ForgeConverter] methods resolve type mismatches between source and destination members.
/// They must be static, non-void, with exactly one parameter, in the same forge class.
/// </summary>
[ForgeClass]
public static partial class ConverterForges
{
    public static partial EventDto ToEventDto(Event source);

    // DateTime → string converter
    [ForgeConverter]
    public static string ConvertDateTime(DateTime value) => value.ToString("yyyy-MM-dd HH:mm");

    // TimeSpan → double converter (total hours)
    [ForgeConverter]
    public static double ConvertTimeSpan(TimeSpan value) => value.TotalHours;
}
