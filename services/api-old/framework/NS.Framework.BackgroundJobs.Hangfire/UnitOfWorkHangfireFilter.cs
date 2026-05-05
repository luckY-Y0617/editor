using System;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Uow;

namespace NS.Framework.BackgroundJobs.Hangfire;

/// <summary>
/// Hangfire UnitOfWork 过滤器
/// 1. 每个后台任务自动包装 UnitOfWork
/// 2. 保证 DbContext/事务/审计/多租户过滤与 Web 请求一致
/// 3. 异常时自动回滚，成功时自动提交
/// 4. 支持配置化的事务选项
/// </summary>
public sealed class UnitOfWorkHangfireFilter : JobFilterAttribute, IServerFilter, IElectStateFilter
{
    private const string ScopeKey = "__claymo_uow_scope";
    private const string UowKey = "__claymo_uow";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnitOfWorkHangfireFilter> _logger;

    public UnitOfWorkHangfireFilter(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        // 在构造函数中无法获取 ILogger，因为这是 Singleton
        // 日志将在运行时通过 scope 获取
        _logger = null!;
    }

    /// <summary>
    /// 任务执行前：创建 Scope 和 UOW
    /// </summary>
    public void OnPerforming(PerformingContext filterContext)
    {
        var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var logger = sp.GetService<ILogger<UnitOfWorkHangfireFilter>>();
        var opt = sp.GetRequiredService<IOptions<HangfireUnitOfWorkOptions>>().Value;
        var uowManager = sp.GetRequiredService<IUnitOfWorkManager>();

        try
        {
            var uow = uowManager.Begin(
                requiresNew: opt.RequiresNew,
                isTransactional: opt.IsTransactional,
                timeout: opt.Timeout,
                isolationLevel: opt.IsolationLevel
            );

            filterContext.Items[ScopeKey] = scope;
            filterContext.Items[UowKey] = uow;

            logger?.LogDebug(
                "Hangfire Job {JobId} started with UOW (Transactional={IsTransactional})",
                filterContext.BackgroundJob.Id,
                opt.IsTransactional);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to create UnitOfWork for Hangfire Job {JobId}", 
                filterContext.BackgroundJob.Id);
            scope.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 任务执行后：提交或回滚 UOW
    /// </summary>
    public void OnPerformed(PerformedContext filterContext)
    {
        var scope = filterContext.Items.TryGetValue(ScopeKey, out var scopeObj) 
            ? scopeObj as IServiceScope 
            : null;
        var uow = filterContext.Items.TryGetValue(UowKey, out var uowObj) 
            ? uowObj as IUnitOfWork 
            : null;

        var logger = scope?.ServiceProvider.GetService<ILogger<UnitOfWorkHangfireFilter>>();

        try
        {
            if (uow != null)
            {
                if (filterContext.Exception == null)
                {
                    // 成功：提交事务
                    uow.CompleteAsync().GetAwaiter().GetResult();
                    logger?.LogDebug(
                        "Hangfire Job {JobId} UOW completed successfully",
                        filterContext.BackgroundJob.Id);
                }
                else
                {
                    // 失败：不调用 Complete，让 Dispose 时自动回滚
                    logger?.LogWarning(
                        filterContext.Exception,
                        "Hangfire Job {JobId} failed, UOW will rollback",
                        filterContext.BackgroundJob.Id);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, 
                "Failed to complete UnitOfWork for Hangfire Job {JobId}",
                filterContext.BackgroundJob.Id);
            // 不重新抛出，让原始异常传播
        }
        finally
        {
            uow?.Dispose();
            scope?.Dispose();
        }
    }

    /// <summary>
    /// 状态选举：可用于自定义重试策略
    /// 
    /// 扩展点：
    /// - 对特定异常标记 DeletedState（不重试）
    /// - 对幂等任务允许更多重试
    /// - 记录审计日志
    /// </summary>
    public void OnStateElection(ElectStateContext context)
    {
        // 示例：对特定业务异常不重试
        // if (context.CandidateState is FailedState failedState)
        // {
        //     if (failedState.Exception is BusinessException)
        //     {
        //         context.CandidateState = new DeletedState { Reason = "业务异常，不重试" };
        //     }
        // }
    }
}
