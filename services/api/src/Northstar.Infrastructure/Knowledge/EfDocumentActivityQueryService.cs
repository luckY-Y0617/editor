using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Activity;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfDocumentActivityQueryService : IDocumentActivityQueryService
{
    private readonly NorthstarDbContext _dbContext;

    public EfDocumentActivityQueryService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentActivityResponse?> GetActivityAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Id == documentId && document.DeletedAt == null)
            .Select(document => new { document.Id, document.WorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var items = await _dbContext.ActivityEvents
            .AsNoTracking()
            .Where(activity => activity.WorkspaceId == document.WorkspaceId &&
                activity.EntityType == ActivityEntityTypes.Document &&
                activity.EntityId == document.Id)
            .OrderByDescending(activity => activity.CreatedAt)
            .Select(activity => new ActivityTimelineItemDto(
                activity.Id.ToString(),
                activity.Action,
                activity.CreatedAt,
                activity.Summary))
            .ToListAsync(cancellationToken);

        return new DocumentActivityResponse(items);
    }
}
