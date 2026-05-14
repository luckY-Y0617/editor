using Northstar.Application.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Tests;

internal sealed class TestEmailInviteRepository : IEmailInviteRepository
{
    public Task<IReadOnlyList<ResourceEmailInvite>> GetByResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ResourceEmailInvite>>(Array.Empty<ResourceEmailInvite>());
    }

    public Task<ResourceEmailInvite?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ResourceEmailInvite?>(null);
    }

    public Task<ResourceEmailInvite?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ResourceEmailInvite?>(null);
    }

    public Task<ResourceEmailInvite?> GetForUpdateAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ResourceEmailInvite?>(null);
    }

    public Task<ResourceEmailInvite?> GetPendingForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ResourceEmailInvite?>(null);
    }

    public Task<string?> GetAcceptedRoleForEmailAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task AddAsync(ResourceEmailInvite invite, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class TestPermissionUserRepository : IPermissionUserRepository
{
    private readonly Guid _userId;

    public TestPermissionUserRepository(Guid userId)
    {
        _userId = userId;
    }

    public Task<PermissionUserIdentity?> GetIdentityAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PermissionUserIdentity?>(
            userId == _userId
                ? new PermissionUserIdentity(userId, "user@example.test", null, null, "Test User")
                : null);
    }

    public Task<IReadOnlyDictionary<Guid, PermissionUserIdentity>> GetIdentitiesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, PermissionUserIdentity> users = userIds.Contains(_userId)
            ? new Dictionary<Guid, PermissionUserIdentity>
            {
                [_userId] = new PermissionUserIdentity(_userId, "user@example.test", null, null, "Test User")
            }
            : new Dictionary<Guid, PermissionUserIdentity>();
        return Task.FromResult(users);
    }
}
