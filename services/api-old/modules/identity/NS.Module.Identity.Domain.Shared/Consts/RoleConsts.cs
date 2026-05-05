namespace NS.Module.Identity.Domain.Shared.Consts;

public static class RoleConsts
{
    public const int RoleNameMaxLength = 50;
    public const int RoleCodeMaxLength = 50;
    public const int DescriptionMaxLength = 500;

    public static class DefaultRoleCodes
    {
        public const string Administrator = "admin";
        public const string TenantAdministrator = "tenant_admin";
        public const string TenantUser = "tenant_user";
    }
}


