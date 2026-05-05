namespace Northstar.Application.Security;

public interface IPermissionUserRepository
{
    Task<PermissionUserIdentity?> GetIdentityAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record PermissionUserIdentity(
    Guid Id,
    string? Email,
    string? ExternalProvider,
    string? ExternalSubjectId);
