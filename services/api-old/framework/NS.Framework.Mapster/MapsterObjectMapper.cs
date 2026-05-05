using Mapster;
using Volo.Abp.ObjectMapping;

namespace NS.Framework.Mapster;

/// <summary>
/// Mapster 实现的对象映射器
/// </summary>
public class MapsterObjectMapper : IObjectMapper
{
    private readonly IAutoObjectMappingProvider _autoMappingProvider;

    public MapsterObjectMapper(MapsterAutoObjectMappingProvider autoMappingProvider)
    {
        _autoMappingProvider = autoMappingProvider;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return source.Adapt<TDestination>();
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        return source.Adapt(destination);
    }

    public IAutoObjectMappingProvider AutoObjectMappingProvider => _autoMappingProvider;
}