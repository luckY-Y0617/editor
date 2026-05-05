using System;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Identity.Domain.Identities.Repositories;

public interface IExternalAuthRepository : ISqlSugarRepository<ExternalAuth, Guid>
{
    Task<ExternalAuth?> FindByProviderAsync(Guid userId, string providerName, string providerKey, CancellationToken cancellationToken = default);
}

