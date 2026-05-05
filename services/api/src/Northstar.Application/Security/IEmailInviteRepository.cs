using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IEmailInviteRepository
{
    Task<IReadOnlyList<ResourceEmailInvite>> GetByResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<ResourceEmailInvite?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<ResourceEmailInvite?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<ResourceEmailInvite?> GetForUpdateAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default);

    Task<ResourceEmailInvite?> GetPendingForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        CancellationToken cancellationToken = default);

    Task<string?> GetAcceptedRoleForEmailAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task AddAsync(ResourceEmailInvite invite, CancellationToken cancellationToken = default);
}
