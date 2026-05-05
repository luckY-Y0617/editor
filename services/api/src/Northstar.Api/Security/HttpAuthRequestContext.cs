using Northstar.Application.Security;

namespace Northstar.Api.Security;

public sealed class HttpAuthRequestContext : IAuthRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpAuthRequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
