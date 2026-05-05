using System.Text.Json;

namespace Northstar.Application.Files;

public interface IFileReferenceService
{
    Task<bool> ValidateAndSyncDocumentReferencesAsync(
        Guid documentId,
        Guid workspaceId,
        JsonElement content,
        Guid actorId,
        CancellationToken cancellationToken = default);
}
