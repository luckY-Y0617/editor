using NS.Framework.SqlSugar.Abstractions.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using NS.Module.TenantManagement.Domain;
using NS.Module.TenantManagement.Domain.Repositories;

namespace NS.Abp.Application.Worker;

public sealed class TenantProvisioningWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantProvisioningWorker> _logger;

    public TenantProvisioningWorker(IServiceScopeFactory scopeFactory, ILogger<TenantProvisioningWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 可配置：轮询间隔
        var delay = TimeSpan.FromSeconds(2);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TenantProvisioningWorker loop error.");
            }

            try { await Task.Delay(delay, stoppingToken); }
            catch { /* ignore */ }
        }
    }

    private async Task ProcessOneAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var currentTenant = sp.GetRequiredService<ICurrentTenant>();
        var uowManager = sp.GetRequiredService<IUnitOfWorkManager>();

        // Host scope：取 job + 抢锁 + 写回状态都在 Host
        using (currentTenant.Change(null))
        {
            TenantProvisioningJob? job;

            // UoW-A：找一个 runnable job
            using (var uow = uowManager.Begin(requiresNew: true, isTransactional: false))
            {
                var repo = sp.GetRequiredService<ITenantProvisioningJobRepository>();
                job = await repo.FindNextRunnableAsync(DateTime.UtcNow, ct);
                await uow.CompleteAsync(ct);
            }

            if (job == null) return;

            var owner = Environment.MachineName;
            var now = DateTime.UtcNow;
            var lockUntil = now.AddMinutes(5);

            bool locked;

            // UoW-B：CAS 抢 lease
            using (var uow = uowManager.Begin(requiresNew: true, isTransactional: false))
            {
                var repo = sp.GetRequiredService<ITenantProvisioningJobRepository>();
                locked = await repo.TryAcquireLeaseAsync(job.TenantId, owner, lockUntil, now, ct);
                await uow.CompleteAsync(ct);
            }

            if (!locked) return;

            // 执行 tenant 初始化（方式一里你已验证稳定的那段）
            try
            {
                await RunTenantBootstrapAsync(sp, job.TenantId, ct);

                // UoW-C：写回 job succeeded + tenant ready
                using (var uow = uowManager.Begin(requiresNew: true, isTransactional: false))
                {
                    var tenantRepo = sp.GetRequiredService<ISqlSugarTenantRepository>();
                    var t = await tenantRepo.FindAsync(job.TenantId, includeDetails: true);
                    if (t != null)
                    {
                        t.MarkReady(DateTime.UtcNow);
                        await tenantRepo.UpdateAsync(t, autoSave: true);
                    }

                    var repo = sp.GetRequiredService<ITenantProvisioningJobRepository>();
                    await repo.MarkSucceededAsync(job.TenantId, DateTime.UtcNow, ct);

                    await uow.CompleteAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provisioning failed for tenant {TenantId}", job.TenantId);

                // UoW-C：写回 job failed + tenant failed
                using (var uow = uowManager.Begin(requiresNew: true, isTransactional: false))
                {
                    var tenantRepo = sp.GetRequiredService<ISqlSugarTenantRepository>();
                    var t = await tenantRepo.FindAsync(job.TenantId, includeDetails: true);
                    if (t != null)
                    {
                        t.MarkFailed(ex.Message, DateTime.UtcNow);
                        await tenantRepo.UpdateAsync(t, autoSave: true);
                    }

                    var repo = sp.GetRequiredService<ITenantProvisioningJobRepository>();
                    await repo.MarkFailedAsync(job.TenantId, ex.Message, DateTime.UtcNow, ct);

                    await uow.CompleteAsync(ct);
                }
            }
        }
    }

    private static async Task RunTenantBootstrapAsync(IServiceProvider sp, Guid tenantId, CancellationToken ct)
    {
        var currentTenant = sp.GetRequiredService<ICurrentTenant>();
        var uowManager = sp.GetRequiredService<IUnitOfWorkManager>();
        var migrationRunner = sp.GetRequiredService<IMigrationRunner>();
        var dataSeeder = sp.GetRequiredService<IDataSeeder>();

        // Tenant scope + requiresNew UoW
        using (var uow = uowManager.Begin(requiresNew: true, isTransactional: false))
        using (currentTenant.Change(tenantId))
        {
            var mig = await migrationRunner.MigrateAsync(
                new MigrationRequest(
                    Scope: MigrationScopes.Tenant,
                    Flags: MigrationFlags.EnsureDatabaseCreated),
                ct);

            if (!mig.Succeeded)
                throw mig.Error ?? new Exception("Migration failed");

            await dataSeeder.SeedAsync(new DataSeedContext(tenantId));

            await uow.CompleteAsync(ct);
        }
    }
}
