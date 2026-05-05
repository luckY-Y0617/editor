using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;
using UAParser;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.AspNetCore.Logging;

public sealed record RequestClientInfo(
    string? Ip,
    string? UserAgent,
    string? Location,
    string? Browser,
    string? Os,
    string? NetworkType,
    string? TraceId);

public interface IRequestClientInfoResolver : ITransientDependency
{
    /// <summary>
    /// 获取基础客户端信息（IP 和 UserAgent）。
    /// </summary>
    (string? Ip, string? UserAgent) GetBasicInfo();

    RequestClientInfo Resolve(
        string? locationHint,
        string? browserHint,
        string? osHint,
        string? networkTypeHint);
}

public sealed class RequestClientInfoResolver : IRequestClientInfoResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestClientInfoResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public (string? Ip, string? UserAgent) GetBasicInfo()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http == null) return (null, null);

        var ip = http.Connection.RemoteIpAddress?.ToString();
        var ua = http.Request.Headers.UserAgent.ToString();

        return (ip, string.IsNullOrWhiteSpace(ua) ? null : ua);
    }

    public RequestClientInfo Resolve(
        string? locationHint,
        string? browserHint,
        string? osHint,
        string? networkTypeHint)
    {
        var http = _httpContextAccessor.HttpContext;
        var ip = http?.Connection.RemoteIpAddress?.ToString();
        var userAgent = http?.Request.Headers.UserAgent.ToString();

        var location = ResolveLocation(ip) ?? locationHint;
        var (browser, os) = ResolveUserAgent(userAgent, browserHint, osHint);
        var networkType = ResolveNetworkType(http) ?? networkTypeHint;
        var traceId = ResolveTraceId(http);

        return new RequestClientInfo(
            ip,
            string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            location,
            browser,
            os,
            networkType,
            traceId);
    }

    private static string? ResolveTraceId(HttpContext? http)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            return traceId;
        }

        return http?.TraceIdentifier;
    }

    private static string? ResolveNetworkType(HttpContext? http)
    {
        if (http == null)
        {
            return null;
        }

        var headers = http.Request.Headers;
        var value = headers["X-Network-Type"].ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = headers["X-Client-Network"].ToString();
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            value = headers["Network-Type"].ToString();
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? ResolveLocation(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return null;
        }

        if (!IPAddress.TryParse(ip, out var address))
        {
            return null;
        }

        if (IPAddress.IsLoopback(address))
        {
            return "Loopback";
        }

        if (IsPrivate(address))
        {
            return "Private";
        }

        return "Unknown";
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127);
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
        }

        return false;
    }

    private static (string? Browser, string? Os) ResolveUserAgent(
        string? userAgent,
        string? browserHint,
        string? osHint)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return (browserHint, osHint);
        }

        var client = Parser.GetDefault().Parse(userAgent);
        var browser = string.IsNullOrWhiteSpace(client.UA.Family) ? browserHint : client.UA.Family;
        var os = string.IsNullOrWhiteSpace(client.OS.Family) ? osHint : client.OS.Family;

        if (userAgent.Contains("MicroMessenger", StringComparison.OrdinalIgnoreCase))
        {
            browser = "WeChat";
        }

        if (userAgent.Contains("DingTalk", StringComparison.OrdinalIgnoreCase))
        {
            browser = "DingTalk";
        }

        return (browser, os);
    }
}

