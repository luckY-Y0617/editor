namespace NS.Framework.Authorization.Abstractions.Permissions;

/// <summary>
/// 权限定义提供者
/// - 负责通过代码声明权限定义
/// - 由 PermissionDefinitionManager 聚合
/// </summary>
public interface IPermissionDefinitionProvider
{
    void Define(IPermissionDefinitionContext context);
}
