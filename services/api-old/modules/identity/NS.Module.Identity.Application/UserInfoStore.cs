using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Application.Contracts.Cache;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Application.Contracts.Teams.Dtos;
using NS.Module.Identity.Application.Contracts.Users;
using NS.Module.Identity.Application.Contracts.Users.Dtos;
using NS.Module.Identity.Domain.Teams;
using NS.Module.Identity.Domain.Users;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;

namespace NS.Module.Identity.Application;


public class UserInfoStore : IAuthUserProfileProvider, ITransientDependency
{
    private readonly ISqlSugarRepository<Team, Guid> _teamRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> _cache;
    private readonly SystemPermissionStore _permissionStore;
    private readonly ICurrentTenant _currentTenant;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UserInfoStore> _logger;

    public UserInfoStore(
        IUserRepository userRepository,
        ISqlSugarRepository<Team, Guid> teamRepository,
        IDistributedCache<UserInfoCacheItem, UserInfoCacheKey> cache,
        SystemPermissionStore permissionStore,
        ICurrentTenant currentTenant,
        IObjectMapper objectMapper,
        ILogger<UserInfoStore> logger)
    {
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _cache = cache;
        _permissionStore = permissionStore;
        _currentTenant = currentTenant;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    #region IAuthUserProfileProvider 实现

    public async Task<AuthUserDto> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var item = await GetAsync(userId, ct);

        return new AuthUserDto
        {
            Id = item.UserId,
            UserName = item.UserName,
            TenantId = item.TenantId,
            Email = item.Email ?? string.Empty,
            PhoneNumber = item.PhoneNumber,
            Avatar = string.IsNullOrWhiteSpace(item.AvatarUrl) ? null : item.AvatarUrl
        };
    }

    public async Task<List<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var set = await _permissionStore.GetSystemPermissionsAsync(userId, ct);
        return set.ToList();
    }

    public async Task WarmupAsync(Guid userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _ = await GetAsync(userId, ct);
        _ = await _permissionStore.GetSystemPermissionsAsync(userId, ct);
    }

    async Task IAuthUserProfileProvider.InvalidateAsync(Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await InvalidateAsync(userId, ct);
        await _permissionStore.InvalidateAsync(userId, ct);
    }

    public async Task<List<UserLookupDto>> FindByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new List<UserLookupDto>();
        }

        var items = await Task.WhenAll(ids.Select(id => GetAsync(id, cancellationToken)));
        return items.Select(MapToLookup).ToList();
    }

    public async Task<UserLookupDto?> FindByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        var item = await GetByUserNameAsync(userName, cancellationToken);
        return item == null ? null : MapToLookup(item);
    }

    public async Task<List<UserTeamDto>> GetUserTeamsAsync(Guid userId)
    {
        var query = await _teamRepository.GetQueryableAsync();

        var teams = await query
            .Includes(x => x.Members)
            .Where(team =>
                SqlFunc.Subqueryable<TeamMember>()
                    .Where(m => m.UserId == userId && m.TeamId == team.Id)
                    .Any()
            )
            .ToListAsync();

        
       return teams.Select(t => new UserTeamDto
        {
            TeamId = t.Id,
            Name = t.Name,
            Role = t.Members.First(m => m.UserId == userId).Role,
            Type = t.Type
        }).ToList();
    }

    #endregion

    #region 缓存操作

    public async Task<UserInfoCacheItem> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = new UserInfoCacheKey(userId, _currentTenant.Id);

        var cacheItem = await _cache.GetAsync(cacheKey, token: cancellationToken);
        if (cacheItem != null)
        {
            return cacheItem;
        }

        var user = await _userRepository.GetAsync(
            userId,
            includeDetails: true,
            cancellationToken: cancellationToken);

        if (user == null)
            throw new BusinessException("Identity:UserNotFound").WithData("UserId", userId);

        var item = new UserInfoCacheItem
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Gender = user.Profile.Gender,
            RoleIds = user.Roles.Select(r => r.Id).ToList(),
            CachedAtUtc = DateTime.UtcNow
        };

        await _cache.SetAsync(
            cacheKey,
            item,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3)
            },
            token: cancellationToken);

        _logger.LogDebug("UserInfoStore cache set: User={UserId}, Tenant={TenantId}", userId, _currentTenant.Id);

        return item;
    }

    public async Task<UserInfoCacheItem?> GetByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        var normalized = userName.Trim().ToUpperInvariant();
        var user = await _userRepository.FindAsync(
            x => x.NormalizedUserName == normalized,
            includeDetails: false,
            cancellationToken: cancellationToken);

        if (user == null)
        {
            return null;
        }

        return await GetAsync(user.Id, cancellationToken);
    }

    /// <summary>
    /// 失效指定用户的身份信息缓存（不含权限缓存）
    /// </summary>
    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = new UserInfoCacheKey(userId, _currentTenant.Id);

        await _cache.RemoveAsync(cacheKey, token: cancellationToken);

        _logger.LogDebug(
            "UserInfoStore cache invalidated: User={UserId}, Tenant={TenantId}",
            userId, _currentTenant.Id);
    }

    /// <summary>
    /// 强制刷新缓存（Invalidate + Get）
    /// </summary>
    public async Task<UserInfoCacheItem> RefreshAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await InvalidateAsync(userId, cancellationToken);
        return await GetAsync(userId, cancellationToken);
    }

    #endregion

    #region 私有方法

    private static UserLookupDto MapToLookup(UserInfoCacheItem item)
    {
        return new UserLookupDto
        {
            Id = item.UserId,
            UserName = item.UserName,
            Email = item.Email,
            AvatarUrl = string.IsNullOrWhiteSpace(item.AvatarUrl) ? null : item.AvatarUrl,
            TenantId = item.TenantId
        };
    }

    private static List<UserTeamDto> MapToTeams(List<Team> teams)
    {
        return teams.Select(x => new UserTeamDto
        {
            TeamId = x.Id,
            Name = x.Name,
            Type = x.Type,
            Role = x.Members.First().Role
        }).ToList();
    }

    #endregion
}
