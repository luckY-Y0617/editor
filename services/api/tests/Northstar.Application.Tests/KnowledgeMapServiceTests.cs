using Northstar.Application.Common;
using Northstar.Application.Knowledge;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Tests;

public sealed class KnowledgeMapServiceTests
{
    [Fact]
    public async Task GetMapAsync_WhenSpaceMissing_ThrowsNotFound()
    {
        var service = new KnowledgeMapService(
            new MissingKnowledgeQueryService(),
            new MissingWorkspaceResolver(),
            new AllowWorkspaceAccessService(),
            new AllowDocumentPermissionFilterService());

        var exception = await Assert.ThrowsAsync<ApplicationErrorException>(() =>
            service.GetMapAsync(Guid.NewGuid()));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    private sealed class MissingKnowledgeQueryService : IKnowledgeQueryService
    {
        public Task<BootstrapResponse?> GetBootstrapAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<BootstrapResponse?>(null);
        }

        public Task<KnowledgeMapResponse?> GetMapAsync(Guid spaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<KnowledgeMapResponse?>(null);
        }

        public Task<KnowledgeDocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<KnowledgeDocumentDto?>(null);
        }

        public Task<KnowledgeDocumentSummaryDto?> GetDocumentSummaryAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<KnowledgeDocumentSummaryDto?>(null);
        }
    }

    private sealed class MissingWorkspaceResolver : IResourceWorkspaceResolver
    {
        public Task<Guid?> GetWorkspaceIdForSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<LibraryPermissionResource?> GetLibraryPermissionResourceAsync(Guid libraryId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<LibraryPermissionResource?>(null);
        }

        public Task<Guid?> GetWorkspaceIdForDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<Guid?> GetWorkspaceIdForCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DocumentPermissionResource?>(null);
        }

        public Task<DocumentPermissionResource?> GetDocumentPermissionResourceIncludingDeletedAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DocumentPermissionResource?>(null);
        }

        public Task<IReadOnlyList<DocumentPermissionResource>> GetDocumentPermissionResourcesAsync(
            IReadOnlyCollection<Guid> documentIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DocumentPermissionResource>>(Array.Empty<DocumentPermissionResource>());
        }

        public Task<CollectionPermissionResource?> GetCollectionPermissionResourceAsync(
            Guid collectionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CollectionPermissionResource?>(null);
        }
    }

    private sealed class AllowWorkspaceAccessService : IWorkspaceAccessService
    {
        public Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Guid.NewGuid());
        }

        public Task EnsureCanViewWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EnsureCanAccessWorkspaceAsync(Guid workspaceId, string actionKey, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EnsureCanEditWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task EnsureCanManageWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class AllowDocumentPermissionFilterService : IDocumentPermissionFilterService
    {
        public Task<BootstrapResponse> FilterBootstrapAsync(
            BootstrapResponse response,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }

        public Task<KnowledgeMapResponse> FilterMapAsync(
            KnowledgeMapResponse response,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }

        public Task<SearchResponse> FilterSearchAsync(
            SearchResponse response,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }

        public Task<DocumentContextResponse> FilterContextAsync(
            DocumentContextResponse response,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }

        public Task<ExportSpaceResponse> FilterExportAsync(
            ExportSpaceResponse response,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(response);
        }
    }
}
