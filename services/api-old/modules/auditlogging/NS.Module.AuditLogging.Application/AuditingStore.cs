using NS.Module.AuditLogging.Domain.Entities;
using NS.Module.AuditLogging.Domain.Extensions;
using NS.Module.AuditLogging.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Json;
using Volo.Abp.Uow;

namespace NS.Module.AuditLogging.Application;

public class AuditingStore : IAuditingStore, ITransientDependency
{
    private readonly ILogger<AuditingStore> _logger;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly AbpAuditingOptions _auditingOptions;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IExceptionToErrorInfoConverter _exceptionConverter;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly AbpExceptionHandlingOptions _exceptionOptions;

    public AuditingStore(
        ILogger<AuditingStore> logger,
        IAuditLogRepository auditLogRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IOptions<AbpAuditingOptions> auditingOptions,
        IGuidGenerator guidGenerator,
        IExceptionToErrorInfoConverter exceptionConverter,
        IJsonSerializer jsonSerializer,
        IOptions<AbpExceptionHandlingOptions> exceptionOptions)
    {
        _logger = logger;
        _auditLogRepository = auditLogRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _auditingOptions = auditingOptions.Value;
        _guidGenerator = guidGenerator;
        _exceptionConverter = exceptionConverter;
        _jsonSerializer = jsonSerializer;
        _exceptionOptions = exceptionOptions.Value;
    }

    public virtual async Task SaveAsync(AuditLogInfo auditInfo)
    {
        if (!_auditingOptions.HideErrors)
        {
            await SaveLogAsync(auditInfo);
            return;
        }

        try
        {
            await SaveLogAsync(auditInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not save the audit log object: " + Environment.NewLine + auditInfo.ToString());
            _logger.LogException(ex, LogLevel.Error);
        }
    }

    protected virtual async Task SaveLogAsync(AuditLogInfo auditInfo)
    {
        using var uow = _unitOfWorkManager.Begin();
        var auditLog = ConvertToAuditLog(auditInfo);
        await _auditLogRepository.InsertAsync(auditLog);
        await uow.CompleteAsync();
    }

    private AuditLog ConvertToAuditLog(AuditLogInfo info)
    {
        var auditLogId = _guidGenerator.Create();

        var entityChanges = info.EntityChanges
            .Select(c => new EntityChange(auditLogId, c))
            .ToList();

        var actions = info.Actions
            .Select(a => new AuditLogAction(auditLogId, a))
            .ToList();

        var exceptions = info.Exceptions.Count > 0
            ? _jsonSerializer.Serialize(
                info.Exceptions.Select(ex => _exceptionConverter.Convert(ex, opt =>
                {
                    opt.SendExceptionsDetailsToClients = _exceptionOptions.SendExceptionsDetailsToClients;
                    opt.SendStackTraceToClients = _exceptionOptions.SendStackTraceToClients;
                })),
                indented: true)
            : null;

        var userName = string.IsNullOrWhiteSpace(info.UserName)
            ? (info.UserId.HasValue ? "user" : "anonymous")
            : info.UserName;

        return new AuditLog(
            info.ApplicationName,
            info.TenantId,
            info.TenantName,
            info.UserId,
            userName,
            info.ExecutionTime,
            info.ExecutionDuration,
            info.ClientIpAddress,
            info.ClientName,
            info.ClientId,
            info.CorrelationId,
            info.BrowserInfo,
            info.HttpMethod,
            info.Url,
            info.HttpStatusCode,
            info.ImpersonatorUserId,
            info.ImpersonatorUserName,
            info.ImpersonatorTenantId,
            info.ImpersonatorTenantName,
            info.ExtraProperties.DeepClone(_jsonSerializer),
            entityChanges,
            actions,
            exceptions,
            info.Comments.JoinAsString(Environment.NewLine)
        );
    }
}
