namespace Northstar.Domain.Security;

public static class LinkModes
{
    public const string Disabled = "disabled";
    public const string Internal = "internal";
    public const string External = "external";
    public const string Public = "public";

    public static bool IsSupported(string? linkMode)
    {
        return linkMode is Disabled or Internal or External or Public;
    }
}
