using System;
using System.Net;
using Volo.Abp.Application.Dtos;

namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

public class GetAuditLogListInput : PagedAndSortedResultRequestDto
{
    public DateTime? StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public string? HttpMethod { get; set; }
    
    public string? Url { get; set; }
    
    public Guid? UserId { get; set; }
    
    public string? UserName { get; set; }
    
    public string? ApplicationName { get; set; }
    
    public string? ClientIpAddress { get; set; }
    
    public string? CorrelationId { get; set; }
    
    public int? MinExecutionDuration { get; set; }
    
    public int? MaxExecutionDuration { get; set; }
    
    public bool? HasException { get; set; }
    
    public HttpStatusCode? HttpStatusCode { get; set; }
    
    public bool IncludeDetails { get; set; } = false;
}

