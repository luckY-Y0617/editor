namespace Northstar.Domain.Security;

public static class EmailInviteDeliveryOutboxStatuses
{
    public const string Pending = "pending";
    public const string RetryScheduled = "retry_scheduled";
    public const string Sent = "sent";
    public const string Failed = "failed";

    public static bool IsSupported(string? status)
    {
        return status is Pending or RetryScheduled or Sent or Failed;
    }
}
