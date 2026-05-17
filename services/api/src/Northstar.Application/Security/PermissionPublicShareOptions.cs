namespace Northstar.Application.Security;

public sealed class PermissionPublicShareOptions
{
    public const string SectionName = "Permissions:PublicShareLinks";

    public bool Enabled { get; set; }
    public bool RequireExpiry { get; set; } = true;
    public bool ViewerOnly { get; set; } = true;
    public int MaxExpiryDays { get; set; } = 7;
    public bool RequirePassword { get; set; }
    public bool RequirePasswordForCollection { get; set; }
    public bool RequirePasswordForLibrary { get; set; }
    public bool DefaultDisableDownload { get; set; } = true;
    public bool DefaultDisablePrint { get; set; }
    public bool DefaultDisableCopy { get; set; }
    public bool DefaultWatermarkEnabled { get; set; }
    public bool RequireWatermarkForCollection { get; set; }
    public bool RequireWatermarkForLibrary { get; set; }
    public bool DisallowDownloadForPublicLinks { get; set; } = true;
    public bool AllowDocumentScope { get; set; } = true;
    public bool AllowCollectionScope { get; set; } = true;
    public bool AllowLibraryScope { get; set; }
    public bool AllowNoExpiry { get; set; }
    public int? MaxExpiryDaysForLibrary { get; set; }
    public PublicShareRateLimitOptions RateLimit { get; set; } = new();

    public TimeSpan EffectiveMaxExpiry(string? resourceType = null)
    {
        var configuredDays = MaxExpiryDays > 0 ? MaxExpiryDays : 7;
        if (resourceType == Northstar.Domain.Security.ResourceTypes.Library &&
            MaxExpiryDaysForLibrary.HasValue &&
            MaxExpiryDaysForLibrary.Value > 0)
        {
            configuredDays = Math.Min(configuredDays, MaxExpiryDaysForLibrary.Value);
        }

        return TimeSpan.FromDays(configuredDays);
    }
}

public sealed class PublicShareRateLimitOptions
{
    public int PermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; }
}
