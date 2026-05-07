namespace Northstar.Domain.Organizations;

public static class OrganizationStatus
{
    public const string Active = "active";
    public const string Disabled = "disabled";

    public static bool IsValid(string status)
    {
        return status is Active or Disabled;
    }
}
