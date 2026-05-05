namespace Northstar.Domain.Security;

public static class GroupTypes
{
    public const string Static = "static";
    public const string Dynamic = "dynamic";

    public static bool IsSupported(string? groupType)
    {
        return groupType is Static or Dynamic;
    }
}
