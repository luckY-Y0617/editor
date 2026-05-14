namespace Northstar.Application.Knowledge;

public interface ISearchIndexMaintenanceService
{
    Task<SearchIndexMaintenanceResult> RebuildAsync(
        Guid? spaceId = null,
        CancellationToken cancellationToken = default);
}

public sealed record SearchIndexMaintenanceResult(
    int Created,
    int Updated,
    int Removed,
    int ActiveDocuments);
