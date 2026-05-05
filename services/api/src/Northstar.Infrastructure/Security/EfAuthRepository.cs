using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfAuthRepository : IAuthRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfAuthRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email != null && user.Email.ToLower() == email.ToLower() && user.DeletedAt == null, cancellationToken);
    }

    public Task<User?> FindUserByExternalAsync(
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user =>
                user.ExternalProvider == externalProvider &&
                user.ExternalSubjectId == externalSubjectId &&
                user.DeletedAt == null,
                cancellationToken);
    }

    public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken);
    }

    public Task<UserCredential?> GetCredentialAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserCredentials
            .FirstOrDefaultAsync(credential => credential.UserId == userId, cancellationToken);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public async Task AddOrUpdateCredentialAsync(
        UserCredential credential,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UserCredentials
            .FirstOrDefaultAsync(item => item.UserId == credential.UserId, cancellationToken);

        if (existing is null)
        {
            await _dbContext.UserCredentials.AddAsync(credential, cancellationToken);
            return;
        }

        existing.UpdatePassword(credential.PasswordHash, credential.PasswordHashAlgorithm);
    }

    public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
    }

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetRefreshTokenFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId)
            .ToListAsync(cancellationToken);
    }

    public Task<AuthEvent?> GetLatestSuccessfulAuthEventAsync(
        Guid userId,
        IReadOnlyCollection<string> actions,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AuthEvents
            .AsNoTracking()
            .Where(authEvent =>
                authEvent.UserId == userId &&
                authEvent.Succeeded &&
                actions.Contains(authEvent.Action))
            .OrderByDescending(authEvent => authEvent.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAuthEventAsync(AuthEvent authEvent, CancellationToken cancellationToken = default)
    {
        return _dbContext.AuthEvents.AddAsync(authEvent, cancellationToken).AsTask();
    }

    public Task<UserMfaMethod?> GetCurrentMfaMethodAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserMfaMethods
            .AsNoTracking()
            .Where(method =>
                method.UserId == userId &&
                method.MethodType == MfaMethodTypes.Totp &&
                (method.Status == MfaMethodStatuses.Pending || method.Status == MfaMethodStatuses.Enabled))
            .OrderByDescending(method => method.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<UserMfaMethod?> GetCurrentMfaMethodForUpdateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserMfaMethods
            .Where(method =>
                method.UserId == userId &&
                method.MethodType == MfaMethodTypes.Totp &&
                (method.Status == MfaMethodStatuses.Pending || method.Status == MfaMethodStatuses.Enabled))
            .OrderByDescending(method => method.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddMfaMethodAsync(UserMfaMethod method, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserMfaMethods.AddAsync(method, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<AuthUserWorkspace>> GetUserWorkspacesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from member in _dbContext.WorkspaceMembers.AsNoTracking()
            join workspace in _dbContext.Workspaces.AsNoTracking() on member.WorkspaceId equals workspace.Id
            where member.UserId == userId &&
                member.Status == WorkspaceMemberStatus.Active &&
                workspace.DeletedAt == null
            orderby workspace.Name
            select new AuthUserWorkspace(workspace.Id, workspace.Name, member.Role))
            .ToListAsync(cancellationToken);
    }
}
