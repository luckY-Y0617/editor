namespace Northstar.Domain.Security;

public static class InheritanceModes
{
    public const string Inherit = "inherit";
    public const string Restricted = "restricted";

    public static bool IsSupported(string? inheritanceMode)
    {
        return inheritanceMode is Inherit or Restricted;
    }
}
