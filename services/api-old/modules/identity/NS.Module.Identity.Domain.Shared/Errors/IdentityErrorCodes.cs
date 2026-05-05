namespace NS.Module.Identity.Domain.Shared.Errors;

public static class IdentityErrorCodes
{
    // ============================
    // 用户相关错误（User）
    // ============================

    /// <summary>
    /// 用户名已存在
    /// </summary>
    public const string UserNameExists = "Identity:UserNameExists";

    /// <summary>
    /// 邮箱已存在
    /// </summary>
    public const string EmailExists = "Identity:EmailExists";

    /// <summary>
    /// 用户不存在
    /// </summary>
    public const string UserNotFound = "Identity:UserNotFound";

    /// <summary>
    /// 用户被禁用或锁定（不可登录）
    /// </summary>
    public const string AccountDisabled = "Identity:AccountDisabled";

    /// <summary>
    /// 用户名或密码错误（登录失败）
    /// </summary>
    public const string LoginFailed = "Identity:LoginFailed";

    /// <summary>
    /// 当前密码不正确（修改密码时）
    /// </summary>
    public const string UserPasswordMismatch = "Identity:UserPasswordMismatch";


    // ============================
    // 密码策略 / 安全（Password / Security）
    // ============================

    /// <summary>
    /// 密码不满足安全策略
    /// </summary>
    public const string PasswordPolicyViolation = "Identity:PasswordPolicyViolation";

    /// <summary>
    /// 密码不能为空（领域规则）
    /// </summary>
    public const string PasswordRequired = "Identity:PasswordRequired";

    /// <summary>
    /// 用户名不能为空（领域规则）
    /// </summary>
    public const string UserNameRequired = "Identity:UserNameRequired";


    // ============================
    // 角色相关错误（Roles）
    // ============================

    /// <summary>
    /// 角色编码已存在
    /// </summary>
    public const string RoleCodeExists = "Identity:RoleCodeExists";

    /// <summary>
    /// 角色不存在
    /// </summary>
    public const string RoleNotFound = "Identity:RoleNotFound";

    /// <summary>
    /// 系统角色不可删除
    /// </summary>
    public const string SystemRoleCannotBeDeleted =
        "Identity:SystemRoleCannotBeDeleted";

    /// <summary>
    /// 系统角色不可修改（名称 / 编码 / 类型）
    /// </summary>
    public const string SystemRoleImmutable =
        "Identity:SystemRoleImmutable";

    /// <summary>
    /// 系统角色权限不可修改
    /// </summary>
    public const string SystemRolePermissionImmutable =
        "Identity:SystemRolePermissionImmutable";

    /// <summary>
    /// 禁止修改当前用户所属角色的权限
    /// </summary>
    public const string CannotModifyOwnRolePermissions =
        "Identity:CannotModifyOwnRolePermissions";


    // ============================
    // 权限相关错误（Permissions）
    // ============================

    /// <summary>
    /// 权限不存在 / 无效的权限码
    /// </summary>
    public const string PermissionNotFound =
        "Identity:PermissionNotFound";

    /// <summary>
    /// 权限列表不能为空（权限分配时）
    /// </summary>
    public const string PermissionCodesRequired =
        "Identity:PermissionCodesRequired";

    /// <summary>
    /// 权限包含系统保留权限（非法操作）
    /// </summary>
    public const string PermissionContainsSystemReserved =
        "Identity:PermissionContainsSystemReserved";


    // ============================
    // 用户-角色关系（UserRole）
    // ============================

    /// <summary>
    /// 禁止移除最后一个租户管理员
    /// </summary>
    public const string LastTenantAdminCannotBeRemoved =
        "Identity:LastTenantAdminCannotBeRemoved";

    /// <summary>
    /// 用户不属于该角色
    /// </summary>
    public const string UserNotInRole =
        "Identity:UserNotInRole";


    // ============================
    // 验证码（Captcha）
    // ============================

    /// <summary>
    /// 验证码无效 / 验证失败
    /// </summary>
    public const string CaptchaInvalid = "Identity:CaptchaInvalid";

    /// <summary>
    /// 验证码已过期
    /// </summary>
    public const string CaptchaExpired = "Identity:CaptchaExpired";
}
