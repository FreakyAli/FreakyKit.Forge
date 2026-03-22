using Mapster;

namespace ForgeBenchmarks;

public static class MapsterConfig
{
    private static bool _configured;

    public static void Configure()
    {
        if (_configured) return;

        TypeAdapterConfig<FlatteningSource, FlatteningDestination>.NewConfig()
            .Map(d => d.HomeAddressStreet, s => s.HomeAddress.Street)
            .Map(d => d.HomeAddressCity, s => s.HomeAddress.City)
            .Map(d => d.HomeAddressState, s => s.HomeAddress.State)
            .Map(d => d.HomeAddressZipCode, s => s.HomeAddress.ZipCode);

        TypeAdapterConfig.GlobalSettings.Compile();
        _configured = true;
    }
}
