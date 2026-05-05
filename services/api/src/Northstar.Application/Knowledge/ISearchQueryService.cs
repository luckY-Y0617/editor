using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface ISearchQueryService
{
    Task<SearchResponse?> SearchAsync(string? query, Guid spaceId, CancellationToken cancellationToken = default);
}
