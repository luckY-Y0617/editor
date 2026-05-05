namespace Northstar.Domain.Security;

public static class PermissionRole
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Editor = "editor";
    public const string Commenter = "commenter";
    public const string Viewer = "viewer";

    public const int OwnerRank = 500;
    public const int AdminRank = 400;
    public const int EditorRank = 300;
    public const int CommenterRank = 200;
    public const int ViewerRank = 100;
}
