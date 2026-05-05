namespace Northstar.Application.Security;

public interface IMfaSecretProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}
