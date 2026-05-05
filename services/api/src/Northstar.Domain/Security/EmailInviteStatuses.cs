namespace Northstar.Domain.Security;

public static class EmailInviteStatuses
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Revoked = "revoked";
    public const string Expired = "expired";

    public static bool IsSupported(string? status)
    {
        return status is Pending or Accepted or Revoked or Expired;
    }
}
