using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfSearchQueryService : ISearchQueryService
{
    private const int ResultLimit = 20;
    private const int ExcerptLength = 180;

    private readonly NorthstarDbContext _dbContext;

    public EfSearchQueryService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchResponse?> SearchAsync(
        string? query,
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        var spaceExists = await _dbContext.Spaces
            .AsNoTracking()
            .AnyAsync(space => space.Id == spaceId && space.DeletedAt == null, cancellationToken);

        if (!spaceExists)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResponse([]);
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        var rows = await (
            from index in _dbContext.DocumentSearchIndexes.AsNoTracking()
            join document in _dbContext.Documents.AsNoTracking() on index.DocumentId equals document.Id
            where index.SpaceId == spaceId &&
                document.DeletedAt == null &&
                document.Status != DocumentStatus.Archived &&
                (index.Title.ToLower().Contains(normalizedQuery) ||
                    index.TextContent.ToLower().Contains(normalizedQuery))
            orderby index.UpdatedAt descending
            select new
            {
                index.DocumentId,
                index.Title,
                document.CollectionId,
                index.TextContent,
                index.UpdatedAt
            })
            .Take(ResultLimit)
            .ToListAsync(cancellationToken);

        var results = rows
            .Select(row => new SearchResultDto(
                row.DocumentId.ToString(),
                "document",
                row.Title,
                row.CollectionId?.ToString() ?? string.Empty,
                CreateExcerpt(row.TextContent, normalizedQuery),
                row.UpdatedAt))
            .ToArray();

        return new SearchResponse(results);
    }

    private static string CreateExcerpt(string? text, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        var normalizedText = trimmed.ToLowerInvariant();
        var index = normalizedText.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (index < 0)
        {
            return trimmed.Length <= ExcerptLength ? trimmed : trimmed[..ExcerptLength];
        }

        var start = Math.Max(0, index - 40);
        var length = Math.Min(ExcerptLength, trimmed.Length - start);
        return trimmed.Substring(start, length);
    }
}
