using System;
using NS.Module.Identity.Domain.Shared.Consts;
using NS.Module.Identity.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Check = Volo.Abp.Check;

namespace NS.Module.Identity.Domain.Identities;

[SugarTable("id_login_logs")]
[SugarIndex("IX_LoginLogs_LoginTime", nameof(LoginTime), OrderByType.Desc)]
[SugarIndex("IX_LoginLogs_Tenant", nameof(TenantId), OrderByType.Asc)]
public class LoginLog : CreationAuditedAggregateRoot<Guid>
{
    
    [SugarColumn(IsNullable = true)]
    public string? UserName { get; private set; }
    
    public LoginTypeEnum LoginType { get; private set; } = LoginTypeEnum.UserNamePassword;
    
    [SugarColumn(IsNullable = true)]
    public string? LoginIp { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? LoginLocation { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? UserAgent { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? Browser { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? Os { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? DeviceType { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? DeviceModel { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? ClientId { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? DeviceId { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? Fingerprint { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? AppVersion { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? AppChannel { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? NetworkType { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? LoginSource { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? TraceId { get; private set; }
    public LoginStatusEnum LoginStatus { get; private set; } = LoginStatusEnum.Success;
    
    [SugarColumn(IsNullable = true)]
    public string? FailureReason { get; private set; }
    public DateTime LoginTime { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public DateTime? LogoutTime { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public string? SessionId { get; private set; }
    
    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    public LoginLog()
    {
    }

    public LoginLog(DateTime loginTime) 
    {
        LoginTime = loginTime;
    }

    public void SetUser(Guid? userId, string? userName)
    {
        CreatorId = userId;

        if (string.IsNullOrWhiteSpace(userName))
        {
            UserName = null;
            return;
        }

        Check.Length(userName, nameof(userName), LoginLogConsts.UserNameMaxLength);
        UserName = userName;
    }

    public void SetLoginType(LoginTypeEnum loginType)
    {
        LoginType = loginType;
    }

    public void SetClientInfo(string? ip, string? location, string? userAgent, string? browser, string? os, string? deviceType)
    {
        SetIp(ip);
        SetLocation(location);
        SetUserAgent(userAgent);
        SetBrowser(browser);
        SetOs(os);
        SetDeviceType(deviceType);
    }

    public void SetClientContext(
        string? clientId,
        string? deviceId,
        string? fingerprint,
        string? deviceModel,
        string? appVersion,
        string? appChannel,
        string? networkType,
        string? loginSource)
    {
        SetClientId(clientId);
        SetDeviceId(deviceId);
        SetFingerprint(fingerprint);
        SetDeviceModel(deviceModel);
        SetAppVersion(appVersion);
        SetAppChannel(appChannel);
        SetNetworkType(networkType);
        SetLoginSource(loginSource);
    }

    public void SetIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            LoginIp = null;
            return;
        }

        Check.Length(ip, nameof(ip), LoginLogConsts.IpAddressMaxLength);
        LoginIp = ip;
    }

    public void SetLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            LoginLocation = null;
            return;
        }

        Check.Length(location, nameof(location), LoginLogConsts.LocationMaxLength);
        LoginLocation = location;
    }

    public void SetUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            UserAgent = null;
            return;
        }

        Check.Length(userAgent, nameof(userAgent), LoginLogConsts.UserAgentMaxLength);
        UserAgent = userAgent;
    }

    public void SetBrowser(string? browser)
    {
        if (string.IsNullOrWhiteSpace(browser))
        {
            Browser = null;
            return;
        }

        Check.Length(browser, nameof(browser), LoginLogConsts.BrowserMaxLength);
        Browser = browser;
    }

    public void SetOs(string? os)
    {
        if (string.IsNullOrWhiteSpace(os))
        {
            Os = null;
            return;
        }

        Check.Length(os, nameof(os), LoginLogConsts.OsMaxLength);
        Os = os;
    }

    public void SetDeviceType(string? deviceType)
    {
        if (string.IsNullOrWhiteSpace(deviceType))
        {
            DeviceType = null;
            return;
        }

        Check.Length(deviceType, nameof(deviceType), LoginLogConsts.DeviceTypeMaxLength);
        DeviceType = deviceType;
    }

    public void SetDeviceModel(string? deviceModel)
    {
        if (string.IsNullOrWhiteSpace(deviceModel))
        {
            DeviceModel = null;
            return;
        }

        Check.Length(deviceModel, nameof(deviceModel), LoginLogConsts.DeviceModelMaxLength);
        DeviceModel = deviceModel;
    }

    public void SetClientId(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            ClientId = null;
            return;
        }

        Check.Length(clientId, nameof(clientId), LoginLogConsts.ClientIdMaxLength);
        ClientId = clientId;
    }

    public void SetDeviceId(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            DeviceId = null;
            return;
        }

        Check.Length(deviceId, nameof(deviceId), LoginLogConsts.DeviceIdMaxLength);
        DeviceId = deviceId;
    }

    public void SetFingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            Fingerprint = null;
            return;
        }

        Check.Length(fingerprint, nameof(fingerprint), LoginLogConsts.FingerprintMaxLength);
        Fingerprint = fingerprint;
    }

    public void SetAppVersion(string? appVersion)
    {
        if (string.IsNullOrWhiteSpace(appVersion))
        {
            AppVersion = null;
            return;
        }

        Check.Length(appVersion, nameof(appVersion), LoginLogConsts.AppVersionMaxLength);
        AppVersion = appVersion;
    }

    public void SetAppChannel(string? appChannel)
    {
        if (string.IsNullOrWhiteSpace(appChannel))
        {
            AppChannel = null;
            return;
        }

        Check.Length(appChannel, nameof(appChannel), LoginLogConsts.AppChannelMaxLength);
        AppChannel = appChannel;
    }

    public void SetNetworkType(string? networkType)
    {
        if (string.IsNullOrWhiteSpace(networkType))
        {
            NetworkType = null;
            return;
        }

        Check.Length(networkType, nameof(networkType), LoginLogConsts.NetworkTypeMaxLength);
        NetworkType = networkType;
    }

    public void SetLoginSource(string? loginSource)
    {
        if (string.IsNullOrWhiteSpace(loginSource))
        {
            LoginSource = null;
            return;
        }

        Check.Length(loginSource, nameof(loginSource), LoginLogConsts.LoginSourceMaxLength);
        LoginSource = loginSource;
    }

    public void SetTraceId(string? traceId)
    {
        if (string.IsNullOrWhiteSpace(traceId))
        {
            TraceId = null;
            return;
        }

        Check.Length(traceId, nameof(traceId), LoginLogConsts.TraceIdMaxLength);
        TraceId = traceId;
    }

    public void SetLoginStatus(LoginStatusEnum status, string? failureReason = null)
    {
        LoginStatus = status;

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            FailureReason = null;
            return;
        }

        FailureReason = failureReason;
    }

    public void SetLoginTime(DateTime loginTime)
    {
        LoginTime = loginTime;
    }

    public void SetLogoutTime(DateTime? logoutTime)
    {
        LogoutTime = logoutTime;
    }

    public void SetSessionId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            SessionId = null;
            return;
        }

        Check.Length(sessionId, nameof(sessionId), LoginLogConsts.SessionIdMaxLength);
        SessionId = sessionId;
    }
}


