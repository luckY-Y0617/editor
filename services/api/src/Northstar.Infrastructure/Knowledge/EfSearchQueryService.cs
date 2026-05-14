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

        if (_dbContext.Database.IsNpgsql())
        {
            return await SearchPostgresAsync(query, spaceId, cancellationToken);
        }

        return await SearchFallbackAsync(query, spaceId, cancellationToken);
    }

    private async Task<SearchResponse> SearchPostgresAsync(
        string query,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        var trimmedQuery = query.Trim();
        var indexes = await _dbContext.DocumentSearchIndexes.FromSql($"""
            WITH search_terms AS (
                SELECT
                    websearch_to_tsquery('simple', {trimmedQuery}) AS ts_query,
                    lower({trimmedQuery}) AS plain_query
            )
            SELECT
                search_index.document_id,
                search_index.workspace_id,
                search_index.space_id,
                search_index.title,
                search_index.text_content,
                search_index.updated_at
            FROM document_search_index AS search_index
            INNER JOIN documents AS document ON document.id = search_index.document_id
            CROSS JOIN search_terms
            WHERE search_index.space_id = {spaceId}
                AND document.deleted_at IS NULL
                AND document.status <> 'archived'
                AND (
                    search_index.search_vector @@ search_terms.ts_query
                    OR lower(search_index.title) LIKE '%' || search_terms.plain_query || '%'
                    OR lower(search_index.text_content) LIKE '%' || search_terms.plain_query || '%'
                    OR similarity(lower(search_index.title), search_terms.plain_query) > 0.2
                )
            ORDER BY
                ts_rank_cd(search_index.search_vector, search_terms.ts_query) DESC,
                similarity(lower(search_index.title), search_terms.plain_query) DESC,
                search_index.updated_at DESC
            LIMIT {ResultLimit}
            """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var documentIds = indexes.Select(index => index.DocumentId).ToArray();
        var collectionIds = await _dbContext.Documents
            .AsNoTracking()
            .Where(document => documentIds.Contains(document.Id))
            .ToDictionaryAsync(document => document.Id, document => document.CollectionId, cancellationToken);

        var rows = indexes.Select(index => new SearchRow
        {
            DocumentId = index.DocumentId,
            Title = index.Title,
            CollectionId = collectionIds.TryGetValue(index.DocumentId, out var collectionId) ? collectionId : null,
            TextContent = index.TextContent,
            UpdatedAt = index.UpdatedAt
        });

        return ToResponse(rows, trimmedQuery.ToLowerInvariant());
    }

    private async Task<SearchResponse> SearchFallbackAsync(
        string query,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
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

        return ToResponse(rows.Select(row => new SearchRow
        {
            DocumentId = row.DocumentId,
            Title = row.Title,
            CollectionId = row.CollectionId,
            TextContent = row.TextContent,
            UpdatedAt = row.UpdatedAt
        }), normalizedQuery);
    }

    private static SearchResponse ToResponse(IEnumerable<SearchRow> rows, string normalizedQuery)
    {
        return new SearchResponse(rows
            .Select(row => new SearchResultDto(
                row.DocumentId.ToString(),
                "document",
                row.Title,
                row.CollectionId?.ToString() ?? string.Empty,
                CreateExcerpt(row.TextContent, normalizedQuery),
                row.UpdatedAt))
            .ToArray());
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

    private sealed class SearchRow
    {
        public Guid DocumentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid? CollectionId { get; set; }
        public string TextContent { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
