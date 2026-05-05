namespace Northstar.Domain.Security;

public static class EmailInviteDeliveryStatuses
{
    public const string Disabled = "disabled";
    public const string Sent = "sent";
    public const string Failed = "failed";

    public static bool IsSupported(string? status)
    {
        return status is Disabled or Sent or Failed;
    }
}
