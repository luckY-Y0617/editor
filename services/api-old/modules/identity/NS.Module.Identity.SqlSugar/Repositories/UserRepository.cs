using NS.Framework.Authentication.Abstractions.Security;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Domain.Shared.Errors;
using NS.Module.Identity.Domain.Users;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace NS.Module.Identity.SqlSugar.Repositories;

public class UserRepository(
    ISqlSugarDbContextProvider<IdentityDbContext> dbContextProvider,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ILogger<UserRepository> logger)
    : SqlSugarRepository<IdentityDbContext, User, Guid>(dbContextProvider),
        IUserRepository,
        ITransientDependency
{
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ILogger<UserRepository> _logger = logger;

    // ============================
    // 登录验证
    // ============================

    /// <summary>
    /// 根据用户名 + 密码查找并验证用户
    /// </summary>
    public async Task<User> FindAndVerifyAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            throw new BusinessException(IdentityErrorCodes.LoginFailed, "用户名或密码不能为空");
        }

        var normalizedUserName = userName.ToUpperInvariant();
        var user = await FindAsync(x => x.NormalizedUserName == normalizedUserName, false, cancellationToken);
        if (user == null)
        {
            throw new BusinessException(IdentityErrorCodes.LoginFailed, "用户名或密码错误");
        }

        var verification = _passwordHasher.Verify(user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new BusinessException(IdentityErrorCodes.LoginFailed, "用户名或密码错误");
        }

        return user;
    }

    // ============================
    // 唯一性检查
    // ============================

    public async Task<bool> UserNameExistsAsync(
        string normalizedUserName,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = await GetSugarQueryableAsync();

        query = query
            .Where(u => u.NormalizedUserName == normalizedUserName)
            .WhereIF(excludeUserId.HasValue, u => u.Id != excludeUserId);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = await GetSugarQueryableAsync();

        query = query
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .WhereIF(excludeUserId.HasValue, u => u.Id != excludeUserId);

        return await query.AnyAsync(cancellationToken);
    }

    // ============================
    // User ↔ Role Queries
    // ============================

    /// <summary>
    /// 获取用户所属的角色 Id 列表（当前租户上下文）
    /// </summary>
    public async Task<List<Guid>> GetRoleIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dbContext = await GetDbContextAsync();

        return await dbContext.Client.Queryable<UserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 判断用户是否属于指定角色（当前租户上下文）
    /// </summary>
    public async Task<bool> IsUserInRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dbContext = await GetDbContextAsync();

        return await dbContext.Client.Queryable<UserRole>()
            .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
            .AnyAsync(cancellationToken);
    }


    // ============================
    // User ↔ Role Mutations
    // ============================

    /// <summary>
    /// 重置用户在指定租户下的角色集合
    /// </summary>
    public async Task ResetRolesAsync(
        Guid userId,
        Guid? tenantId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dbContext = await GetDbContextAsync();
        var client = dbContext.Client;

        // 1. 删除该用户在当前租户下的所有角色关系
        await client.Deleteable<UserRole>()
            .Where(ur => ur.UserId == userId)
            .WhereIF(tenantId.HasValue, ur => ur.TenantId == tenantId)
            .ExecuteCommandAsync(cancellationToken);

        // 2. 如果没有新的角色要绑定，直接结束
        if (roleIds.Count == 0)
        {
            _logger.LogDebug(
                "用户 {UserId} 在租户 {TenantId} 下角色已清空",
                userId,
                tenantId);
            return;
        }

        // 3. 构造新的 UserRole 列表
        var entities = roleIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Select(roleId => new UserRole(
                userId: userId,
                roleId: roleId)
            {
                TenantId = tenantId
            })
            .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        // 4. 批量插入新关系
        await client.Insertable(entities)
            .ExecuteCommandAsync(cancellationToken);

        _logger.LogInformation(
            "用户 {UserId} 在租户 {TenantId} 下重置角色成功，角色数 {Count}",
            userId,
            tenantId,
            entities.Count);
    }
}
