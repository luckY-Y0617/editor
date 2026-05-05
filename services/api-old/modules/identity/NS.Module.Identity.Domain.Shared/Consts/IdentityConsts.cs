namespace NS.Module.Identity.Domain.Shared.Consts;

public static class IdentityConsts
{
    public const string ModuleName = "Identity";
    public const string CachePrefix = "Identity";

    public static class CacheKeys
    {
        public const string UserInfo = $"{CachePrefix}:UserInfo";
        public const string UserPermissions = $"{CachePrefix}:UserPermissions";
    }
}


