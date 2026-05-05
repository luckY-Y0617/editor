using System.Text;
using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public sealed class SpaceTransferService : ISpaceTransferService
{
    private const int MaxImportDocuments = 200;
    private const int MaxContentBytes = 2 * 1024 * 1024;

    private readonly ISpaceTransferRepository _transferRepository;
    private readonly IKnowledgeQueryService _queryService;
    private readonly IResourceWorkspaceResolver _workspaceResolver;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IDocumentPermissionFilterService _permissionFilterService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public SpaceTransferService(
        ISpaceTransferRepository transferRepository,
        IKnowledgeQueryService queryService,
        IResourceWorkspaceResolver workspaceResolver,
        IWorkspaceAccessService accessService,
        IDocumentPermissionFilterService permissionFilterService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _transferRepository = transferRepository;
        _queryService = queryService;
        _workspaceResolver = workspaceResolver;
        _accessService = accessService;
        _permissionFilterService = permissionFilterService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExportSpaceResponse> ExportAsync(
        Guid spaceId,
        bool includeArchived = true,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = await _workspaceResolver.GetWorkspaceIdForSpaceAsync(spaceId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        await _accessService.EnsureCanViewWorkspaceAsync(workspaceId, cancellationToken);

        var response = await _transferRepository.ExportAsync(spaceId, includeArchived, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
        return await _permissionFilterService.FilterExportAsync(response, cancellationToken);
    }

    public Task<ImportSpaceResponse> ImportAsync(
        Guid spaceId,
        ImportSpaceRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var workspaceId = await _workspaceResolver.GetWorkspaceIdForSpaceAsync(spaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");
            await _accessService.EnsureCanEditWorkspaceAsync(workspaceId, ct);
            var actorId = await _accessService.GetRequiredUserIdAsync(ct);

            ValidateImportRequest(request);

            var result = await _transferRepository.ImportAppendAsync(spaceId, request, actorId, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var map = await _queryService.GetMapAsync(spaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");

            return new ImportSpaceResponse(
                result.ImportedCollectionCount,
                result.ImportedDocumentCount,
                await _permissionFilterService.FilterMapAsync(map, ct));
        }, cancellationToken);
    }

    private static void ValidateImportRequest(ImportSpaceRequest request)
    {
        if (!string.Equals(request.Mode, "append", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "Import mode must be append.",
                new { field = "mode", reason = "Only append import is supported in Phase 5." });
        }

        if (request.Documents is null)
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "Import documents are required.",
                new { field = "documents", reason = "Documents array is required." });
        }

        if (request.Documents.Count > MaxImportDocuments)
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "Import document limit exceeded.",
                new { maxDocuments = MaxImportDocuments });
        }

        for (var index = 0; index < request.Documents.Count; index++)
        {
            var document = request.Documents[index];
            if (string.IsNullOrWhiteSpace(document.Title))
            {
                ThrowDocumentValidation(index, "title is required.");
            }

            if (document.Content.ValueKind != JsonValueKind.Object)
            {
                ThrowDocumentValidation(index, "content must be a JSON object.");
            }

            if (Encoding.UTF8.GetByteCount(document.Content.GetRawText()) > MaxContentBytes)
            {
                ThrowDocumentValidation(index, $"content must be at most {MaxContentBytes} bytes.");
            }
        }
    }

    private static void ThrowDocumentValidation(int documentIndex, string reason)
    {
        throw new ApplicationErrorException(
            ErrorCodes.ValidationError,
            "Import validation failed.",
            new { documentIndex, reason });
    }
}
