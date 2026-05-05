using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IAccessRequestService
{
    Task<AccessRequestDto> CreateAccessRequestAsync(CreateAccessRequestRequest request, CancellationToken cancellationToken = default);
    Task<AccessRequestsResponse> GetAccessRequestsAsync(Guid workspaceId, string? status, CancellationToken cancellationToken = default);
    Task<AccessRequestsResponse> GetResourceAccessRequestsAsync(string resourceType, Guid resourceId, CancellationToken cancellationToken = default);
    Task<AccessRequestDto> ReviewAccessRequestAsync(Guid requestId, ReviewAccessRequestRequest request, CancellationToken cancellationToken = default);
    Task<AccessRequestDto> CancelAccessRequestAsync(Guid requestId, CancelAccessRequestRequest? request, CancellationToken cancellationToken = default);
}
