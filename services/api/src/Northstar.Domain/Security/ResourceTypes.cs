namespace Northstar.Domain.Security;

public static class ResourceTypes
{
    public const string Workspace = "workspace";
    public const string Collection = "collection";
    public const string Document = "document";

    public static bool IsScopedResource(string? resourceType)
    {
        return resourceType is Collection or Document;
    }

    public static bool IsSupported(string? resourceType)
    {
        return resourceType is Workspace or Collection or Document;
    }
}
