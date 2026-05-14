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
            .Select(document => new { document.Id, document.Title, document.WorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var activityRows = await (
            from activity in _dbContext.ActivityEvents.AsNoTracking()
            join actor in _dbContext.Users.AsNoTracking()
                on activity.ActorId equals (Guid?)actor.Id into actorRows
            from actor in actorRows.DefaultIfEmpty()
            where activity.WorkspaceId == document.WorkspaceId &&
                activity.EntityType == ActivityEntityTypes.Document &&
                activity.EntityId == document.Id
            orderby activity.CreatedAt descending
            select new
            {
                activity.Id,
                activity.Action,
                activity.CreatedAt,
                activity.Summary,
                activity.ActorId,
                Actor = actor == null ? null : new ActivityActorDto(actor.Id.ToString(), actor.DisplayName)
            })
            .ToListAsync(cancellationToken);

        var items = activityRows
            .Select(activity => new ActivityTimelineItemDto(
                activity.Id.ToString(),
                activity.Action,
                activity.CreatedAt,
                activity.Summary,
                activity.Actor,
                new ActivityDocumentDto(document.Id.ToString(), document.Title),
                ActivityClassification.ClassifyDocumentActivity(
                    activity.Action,
                    document.Id,
                    activity.ActorId)))
            .ToArray();

        return new DocumentActivityResponse(items);
    }
}
