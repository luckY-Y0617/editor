using System;
using NS.Module.Identity.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Identity.Application.Contracts.identities.Dtos;

public sealed class LoginLogDto : EntityDto<Guid>
{
    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public LoginTypeEnum LoginType { get; set; }

    public string? LoginIp { get; set; }

    public string? LoginLocation { get; set; }

    public string? UserAgent { get; set; }

    public string? Browser { get; set; }

    public string? Os { get; set; }

    public string? DeviceType { get; set; }

    public string? DeviceModel { get; set; }

    public string? ClientId { get; set; }

    public string? DeviceId { get; set; }

    public string? Fingerprint { get; set; }

    public string? AppVersion { get; set; }

    public string? AppChannel { get; set; }

    public string? NetworkType { get; set; }

    public string? LoginSource { get; set; }

    public LoginStatusEnum LoginStatus { get; set; }

    public string? FailureReason { get; set; }

    public string LoginTime { get; set; } = string.Empty;

    public string? LogoutTime { get; set; }

    public string? SessionId { get; set; }

    public Guid? TenantId { get; set; }

    public string? TraceId { get; set; }

    public string CreationTime { get; set; } = string.Empty;
}

