using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IEmailInviteDeliveryOutboxProcessor
{
    Task<EmailInviteDeliveryResult> ProcessAsync(
        EmailInviteDeliveryOutboxItem item,
        ResourceEmailInvite invite,
        string acceptUrl,
        CancellationToken cancellationToken = default);

    Task<EmailInviteDeliveryOutboxProcessResult> ProcessDueAsync(
        IReadOnlyDictionary<Guid, string> acceptUrlsByInviteId,
        DateTimeOffset? now = null,
        int batchSize = 25,
        CancellationToken cancellationToken = default);
}

public sealed record EmailInviteDeliveryOutboxProcessResult(
    int Attempted,
    int Sent,
    int Retrying,
    int Failed,
    int MissingAcceptUrl);
