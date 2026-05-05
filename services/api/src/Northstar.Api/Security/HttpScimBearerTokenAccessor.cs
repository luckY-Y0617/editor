using System.Net.Http.Headers;
using Northstar.Application.Security;

namespace Northstar.Api.Security;

public sealed class HttpScimBearerTokenAccessor : IScimBearerTokenAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpScimBearerTokenAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetBearerToken()
    {
        var value = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(value) ||
            !AuthenticationHeaderValue.TryParse(value, out var header) ||
            !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return null;
        }

        return header.Parameter.Trim();
    }
}
