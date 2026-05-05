using SqlSugar;
using Volo.Abp.Domain.Entities;
using Check = Volo.Abp.Check;

namespace NS.Framework.SqlSugar.Abstractions;

public class SqlSugarEntityOptions<TEntity> where TEntity : IEntity
{
    public static SqlSugarEntityOptions<TEntity> Empty => new ();
    
    public Func<ISugarQueryable<TEntity>, ISugarQueryable<TEntity>>? DefaultWithDetailsFunc { get; set; }
}

public class SqlSugarEntityOptions
{
    private readonly IDictionary<Type, object> _options = new Dictionary<Type, object>();

    public SqlSugarEntityOptions<TEntity>? GetOrNull<TEntity>() where TEntity : IEntity
    {
        return _options.GetOrDefault(typeof(TEntity)) as SqlSugarEntityOptions<TEntity>;
    }

    public void Entity<TEntity>(Action<SqlSugarEntityOptions<TEntity>> optionsAction) 
        where TEntity : IEntity
    {
        Check.NotNull(optionsAction, nameof(optionsAction));

        optionsAction(
            (_options.GetOrAdd(
                typeof(TEntity),
                () => new SqlSugarEntityOptions<TEntity>()
            ) as SqlSugarEntityOptions<TEntity>)!
        );
    }
    
}

