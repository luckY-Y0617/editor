namespace Northstar.Domain.Security;

public static class AccessRequestStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Denied = "denied";
    public const string Cancelled = "cancelled";

    public static bool IsSupported(string? status)
    {
        return status is Pending or Approved or Denied or Cancelled;
    }
}
