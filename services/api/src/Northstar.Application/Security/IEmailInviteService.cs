using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IEmailInviteService
{
    Task<EmailInvitesResponse> GetInvitesAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<CreateEmailInviteResponse> CreateInviteAsync(
        string resourceType,
        Guid resourceId,
        CreateEmailInviteRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeInviteAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default);

    Task<ResolveEmailInviteResponse> ResolveInviteAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<AcceptEmailInviteResponse> AcceptInviteAsync(
        string token,
        CancellationToken cancellationToken = default);
}
