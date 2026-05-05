using NS.Framework.SqlSugar;
using Volo.Abp.DependencyInjection;
namespace NS.Module.Knowledge.SqlSugar;

public class KnowledgeSqlSugarDbContext : SqlSugarDbContext
{
    public KnowledgeSqlSugarDbContext(IAbpLazyServiceProvider lazyServiceProvider)
        : base(lazyServiceProvider)
    {
    }
}