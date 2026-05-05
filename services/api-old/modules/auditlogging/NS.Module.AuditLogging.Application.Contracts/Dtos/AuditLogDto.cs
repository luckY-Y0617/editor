using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

public class AuditLogDto : EntityDto<Guid>
{
    public string? ApplicationName { get; set; }
    
    public Guid? UserId { get; set; }
    
    public string? UserName { get; set; }
    
    public string? TenantName { get; set; }
    
    public Guid? ImpersonatorUserId { get; set; }
    
    public string? ImpersonatorUserName { get; set; }
    
    public Guid? ImpersonatorTenantId { get; set; }
    
    public string? ImpersonatorTenantName { get; set; }
    
    public DateTime? ExecutionTime { get; set; }
    
    public int? ExecutionDuration { get; set; }
    
    public string? ClientIpAddress { get; set; }
    
    public string? ClientName { get; set; }
    
    public string? ClientId { get; set; }
    
    public string? CorrelationId { get; set; }
    
    public string? BrowserInfo { get; set; }
    
    public string? HttpMethod { get; set; }
    
    public string? Url { get; set; }
    
    public int? HttpStatusCode { get; set; }
    
    public bool HasException { get; set; }
    
    public string? Comments { get; set; }
    
    public Dictionary<string, object>? ExtraProperties { get; set; }
}

