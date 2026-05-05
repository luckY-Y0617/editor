using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(string? query, Guid spaceId, CancellationToken cancellationToken = default);
}
