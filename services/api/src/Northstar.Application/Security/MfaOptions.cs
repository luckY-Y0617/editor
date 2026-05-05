namespace Northstar.Application.Security;

public sealed class MfaOptions
{
    public const string SectionName = "Auth:Mfa";

    public string Issuer { get; init; } = "Northstar";
    public string? SecretProtectionKey { get; init; }
    public int StepUpWindowMinutes { get; init; } = 15;
    public int TotpStepSeconds { get; init; } = 30;
    public int TotpAllowedSkewSteps { get; init; } = 1;
}
