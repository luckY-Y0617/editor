namespace Northstar.Domain.Knowledge.Comments;

public static class CommentThreadStatus
{
    public const string Open = "open";
    public const string Resolved = "resolved";

    public static bool IsValid(string status)
    {
        return status is Open or Resolved;
    }
}
