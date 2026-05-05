using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Identity.Domain.Identities.Repositories;

public interface ILoginLogRepository : ISqlSugarRepository<LoginLog, Guid>
{
    Task<List<LoginLog>> GetRecentAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);
}

