using System;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Identities.Repositories;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Identity.SqlSugar.Repositories;

public class ExternalAuthRepository : 
    SqlSugarRepository<IdentityDbContext, ExternalAuth, Guid>, 
    IExternalAuthRepository, 
    ITransientDependency
{
    public ExternalAuthRepository(ISqlSugarDbContextProvider<IdentityDbContext> dbContextProvider): base(dbContextProvider){}
    
    public async Task<ExternalAuth?> FindByProviderAsync(
        Guid userId,
        string providerName,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = await GetSugarQueryableAsync();

        return await query
            .Where(auth => auth.UserId == userId)
            .Where(auth => auth.ProviderName == providerName)
            .Where(auth => auth.ProviderKey == providerKey)
            .FirstAsync(cancellationToken);
    }
}

