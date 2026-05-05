using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfEmailInviteDeliveryOutboxRepository : IEmailInviteDeliveryOutboxRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfEmailInviteDeliveryOutboxRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<EmailInviteDeliveryOutboxItem>> GetDueForUpdateAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailInviteDeliveryOutbox
            .Where(item =>
                (item.Status == EmailInviteDeliveryOutboxStatuses.Pending ||
                    item.Status == EmailInviteDeliveryOutboxStatuses.RetryScheduled) &&
                item.NextAttemptAt <= now)
            .OrderBy(item => item.NextAttemptAt)
            .ThenBy(item => item.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(
        EmailInviteDeliveryOutboxItem item,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EmailInviteDeliveryOutbox.AddAsync(item, cancellationToken).AsTask();
    }
}
