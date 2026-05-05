using Northstar.Domain.Users;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IAuthRepository
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindUserByExternalAsync(
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default);
    Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserCredential?> GetCredentialAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task AddOrUpdateCredentialAsync(UserCredential credential, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> GetRefreshTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);
    Task<AuthEvent?> GetLatestSuccessfulAuthEventAsync(
        Guid userId,
        IReadOnlyCollection<string> actions,
        CancellationToken cancellationToken = default);
    Task AddAuthEventAsync(AuthEvent authEvent, CancellationToken cancellationToken = default);
    Task<UserMfaMethod?> GetCurrentMfaMethodAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserMfaMethod?> GetCurrentMfaMethodForUpdateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddMfaMethodAsync(UserMfaMethod method, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthUserWorkspace>> GetUserWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default);
}
