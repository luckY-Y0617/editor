namespace Northstar.Application.Security;

public interface IAuthRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}
