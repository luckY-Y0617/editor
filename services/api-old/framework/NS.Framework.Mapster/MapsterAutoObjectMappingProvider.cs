using Mapster;
using Volo.Abp.ObjectMapping;

namespace NS.Framework.Mapster;

public class MapsterAutoObjectMappingProvider: IAutoObjectMappingProvider
{
    public TDestination Map<TSource, TDestination>(object source)
    {
        // 使用 Mapster 将 object 类型转换为目标类型
        return source.Adapt<TDestination>();
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        // 将一个 TSource 类型的源对象的属性，复制/映射到一个已有的 TDestination 对象上，而不是新建一个。
        return source.Adapt(destination);
    }
}