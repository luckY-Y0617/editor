namespace Northstar.Domain.Security;

public static class ShareLinkAudiences
{
    public const string Workspace = "workspace";
    public const string External = "external";
    public const string Public = "public";

    public static bool IsSupported(string? audience)
    {
        return audience is Workspace or External or Public;
    }
}
