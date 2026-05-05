namespace Northstar.Application.Security;

public sealed class PermissionPublicShareOptions
{
    public const string SectionName = "Permissions:PublicShareLinks";

    public bool Enabled { get; set; }
    public bool RequireExpiry { get; set; } = true;
    public bool ViewerOnly { get; set; } = true;
    public int MaxExpiryDays { get; set; } = 7;
    public PublicShareRateLimitOptions RateLimit { get; set; } = new();

    public TimeSpan EffectiveMaxExpiry()
    {
        return TimeSpan.FromDays(MaxExpiryDays > 0 ? MaxExpiryDays : 7);
    }
}

public sealed class PublicShareRateLimitOptions
{
    public int PermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; }
}
