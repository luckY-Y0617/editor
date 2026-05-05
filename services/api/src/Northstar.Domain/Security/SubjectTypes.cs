namespace Northstar.Domain.Security;

public static class SubjectTypes
{
    public const string User = "user";
    public const string Group = "group";
    public const string ShareLink = "share_link";
    public const string EmailInvite = "email_invite";

    public static bool IsSupported(string? subjectType)
    {
        return subjectType is User or Group;
    }
}
