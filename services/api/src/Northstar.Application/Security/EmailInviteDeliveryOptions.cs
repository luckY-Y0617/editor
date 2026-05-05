namespace Northstar.Application.Security;

public sealed class EmailInviteDeliveryOptions
{
    public const string SectionName = "Permissions:EmailInvites:Delivery";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "noop";
    public string? PublicBaseUrl { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 300;
    public SmtpEmailInviteDeliveryOptions Smtp { get; set; } = new();
}

public sealed class SmtpEmailInviteDeliveryOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
