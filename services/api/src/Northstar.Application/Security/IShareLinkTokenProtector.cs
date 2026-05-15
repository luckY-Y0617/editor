namespace Northstar.Application.Security;

public interface IShareLinkTokenProtector
{
    string Protect(string token);

    string Unprotect(string protectedToken);
}
