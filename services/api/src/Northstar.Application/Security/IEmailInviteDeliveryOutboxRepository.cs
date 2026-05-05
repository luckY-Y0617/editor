using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IEmailInviteDeliveryOutboxRepository
{
    Task<IReadOnlyList<EmailInviteDeliveryOutboxItem>> GetDueForUpdateAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        EmailInviteDeliveryOutboxItem item,
        CancellationToken cancellationToken = default);
}
