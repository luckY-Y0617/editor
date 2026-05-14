namespace Northstar.Application.Security;

public interface IPermissionUserRepository
{
    Task<PermissionUserIdentity?> GetIdentityAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, PermissionUserIdentity>> GetIdentitiesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}

public sealed record PermissionUserIdentity(
    Guid Id,
    string? Email,
    string? ExternalProvider,
    string? ExternalSubjectId,
    string DisplayName);
