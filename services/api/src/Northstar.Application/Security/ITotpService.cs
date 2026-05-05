namespace Northstar.Application.Security;

public interface ITotpService
{
    string GenerateSecret();
    string BuildProvisioningUri(string issuer, string accountName, string secret);
    bool VerifyCode(string secret, string code, DateTimeOffset now, int stepSeconds, int allowedSkewSteps);
}
