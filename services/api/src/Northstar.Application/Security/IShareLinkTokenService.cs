namespace Northstar.Application.Security;

public interface IShareLinkTokenService
{
    string GenerateToken();
    string HashToken(string token);
}
