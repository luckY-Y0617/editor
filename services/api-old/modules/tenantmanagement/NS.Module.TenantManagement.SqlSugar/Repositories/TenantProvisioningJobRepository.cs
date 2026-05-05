using NS.Framework.SqlSugar;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.TenantManagement.Domain;
using NS.Module.TenantManagement.Domain.Repositories;
using NS.Module.TenantManagement.Domain.Shared.Enums;
using Volo.Abp.DependencyInjection;

namespace NS.Module.TenantManagement.SqlSugar.Repositories;

public class TenantProvisioningJobRepository
    : SqlSugarRepository<SqlSugarDbContext, TenantProvisioningJob, Guid>,
      ITenantProvisioningJobRepository,
      ITransientDependency
{
    public TenantProvisioningJobRepository(
        ISqlSugarDbContextProvider<SqlSugarDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task EnqueueAsync(Guid tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // 用仓储 FindAsync（先查再写）替代 Storageable Upsert
        var existing = await FindAsync(
            j => j.TenantId == tenantId,
            includeDetails: false,
            cancellationToken: ct);

        if (existing == null)
        {
            var job = new TenantProvisioningJob
            {
                TenantId = tenantId,
                State = TenantProvisioningJobState.Queued,
                AttemptCount = 0,
                NextRunAtUtc = now,
                LastError = null,
                LockOwner = null,
                LockUntilUtc = null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await InsertAsync(job, autoSave: true, cancellationToken: ct);
            return;
        }

        existing.State = TenantProvisioningJobState.Queued;
        existing.AttemptCount = 0;
        existing.NextRunAtUtc = now;
        existing.LastError = null;
        existing.LockOwner = null;
        existing.LockUntilUtc = null;
        existing.UpdatedAtUtc = now;

        await UpdateAsync(existing, autoSave: true, cancellationToken: ct);
    }

    public async Task<TenantProvisioningJob?> FindNextRunnableAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var query = await GetSugarQueryableAsync();

        return await query
            .Where(j =>
                j.State == TenantProvisioningJobState.Queued &&
                (j.NextRunAtUtc == null || j.NextRunAtUtc <= utcNow) &&
                (j.LockUntilUtc == null || j.LockUntilUtc <= utcNow))
            .OrderBy(j => j.UpdatedAtUtc)
            .FirstAsync(ct);
    }

    public async Task<bool> TryAcquireLeaseAsync(
        Guid tenantId,
        string owner,
        DateTime lockUntilUtc,
        DateTime utcNow,
        CancellationToken ct = default)
    {
        // 这里保留“原子 CAS 抢锁”，避免并发竞争窗口
        var ctx = await GetDbContextAsync();

        var affected = await ctx.Client.Updateable<TenantProvisioningJob>()
            .SetColumns(j => new TenantProvisioningJob
            {
                State = TenantProvisioningJobState.Running,
                LockOwner = owner,
                LockUntilUtc = lockUntilUtc,
                UpdatedAtUtc = utcNow
            })
            .Where(j => j.TenantId == tenantId)
            .Where(j => j.LockUntilUtc == null || j.LockUntilUtc <= utcNow)
            .ExecuteCommandAsync(ct);

        return affected == 1;
    }

    public async Task MarkSucceededAsync(Guid tenantId, DateTime utcNow, CancellationToken ct = default)
    {
        var job = await FindAsync(
            j => j.TenantId == tenantId,
            includeDetails: false,
            cancellationToken: ct);

        if (job == null) return;

        job.State = TenantProvisioningJobState.Succeeded;
        job.LockOwner = null;
        job.LockUntilUtc = null;
        job.LastError = null;
        job.UpdatedAtUtc = utcNow;

        await UpdateAsync(job, autoSave: true, cancellationToken: ct);
    }

    public async Task MarkFailedAsync(Guid tenantId, string error, DateTime utcNow, CancellationToken ct = default)
    {
        var job = await FindAsync(
            j => j.TenantId == tenantId,
            includeDetails: false,
            cancellationToken: ct);

        if (job == null) return;

        var msg = error.Length > 2000 ? error[..2000] : error;

        job.State = TenantProvisioningJobState.Failed;
        job.AttemptCount +=  1;
        job.NextRunAtUtc = null;
        job.LastError = msg;
        job.LockOwner = null;
        job.LockUntilUtc = null;
        job.UpdatedAtUtc = utcNow;

        await UpdateAsync(job, autoSave: true, cancellationToken: ct);
    }
}
