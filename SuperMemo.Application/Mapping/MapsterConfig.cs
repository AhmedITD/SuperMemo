using Mapster;

namespace SuperMemo.Application.Mapping;

public static class MapsterConfig
{
    public static void ConfigureMappings()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MapsterConfig).Assembly);
    }
}
