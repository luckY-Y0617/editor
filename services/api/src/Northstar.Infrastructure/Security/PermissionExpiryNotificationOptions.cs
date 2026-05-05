namespace Northstar.Infrastructure.Security;

public sealed class PermissionExpiryNotificationOptions
{
    public const string SectionName = "Permissions:ExpiryNotifications";

    public bool Enabled { get; set; } = true;
    public int ScanIntervalMinutes { get; set; } = 60;
    public int ExpiringWindowHours { get; set; } = 24;
}
