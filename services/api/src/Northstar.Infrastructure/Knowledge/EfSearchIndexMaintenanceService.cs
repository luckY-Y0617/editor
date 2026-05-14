using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Infrastructure.Persistence;
using Northstar.Infrastructure.Search;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfSearchIndexMaintenanceService : ISearchIndexMaintenanceService
{
    private readonly NorthstarDbContext _dbContext;

    public EfSearchIndexMaintenanceService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchIndexMaintenanceResult> RebuildAsync(
        Guid? spaceId = null,
        CancellationToken cancellationToken = default)
    {
        var activeRows = await (
            from document in _dbContext.Documents
            join draft in _dbContext.DocumentDrafts on document.Id equals draft.DocumentId
            where document.DeletedAt == null &&
                document.Status != DocumentStatus.Archived &&
                (!spaceId.HasValue || document.SpaceId == spaceId.Value)
            select new
            {
                document.Id,
                document.WorkspaceId,
                document.SpaceId,
                document.Title,
                draft.TextContent
            })
            .ToListAsync(cancellationToken);

        var activeIds = activeRows.Select(row => row.Id).ToHashSet();
        var existingIndexes = await _dbContext.DocumentSearchIndexes
            .Where(index => !spaceId.HasValue || index.SpaceId == spaceId.Value)
            .ToListAsync(cancellationToken);
        var existingByDocumentId = existingIndexes.ToDictionary(index => index.DocumentId);

        var staleIndexes = existingIndexes
            .Where(index => !activeIds.Contains(index.DocumentId))
            .ToArray();
        _dbContext.DocumentSearchIndexes.RemoveRange(staleIndexes);

        var created = 0;
        var updated = 0;
        foreach (var row in activeRows)
        {
            if (!existingByDocumentId.TryGetValue(row.Id, out var index))
            {
                await _dbContext.DocumentSearchIndexes.AddAsync(
                    new DocumentSearchIndex(
                        row.Id,
                        row.WorkspaceId,
                        row.SpaceId,
                        row.Title,
                        row.TextContent),
                    cancellationToken);
                created++;
                continue;
            }

            if (index.SpaceId != row.SpaceId ||
                !string.Equals(index.Title, row.Title, StringComparison.Ordinal) ||
                !string.Equals(index.TextContent, row.TextContent, StringComparison.Ordinal))
            {
                index.Update(row.Title, row.TextContent, row.SpaceId);
                updated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new SearchIndexMaintenanceResult(created, updated, staleIndexes.Length, activeRows.Count);
    }
}
