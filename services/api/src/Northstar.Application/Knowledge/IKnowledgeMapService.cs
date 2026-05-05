using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface IKnowledgeMapService
{
    Task<KnowledgeMapResponse> GetMapAsync(Guid spaceId, CancellationToken cancellationToken = default);
}

