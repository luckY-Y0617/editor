using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Module.Knowledge.Application.Contracts.Documents.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Knowledge.Application.Contracts.Documents;

public interface IDocumentAppService : IApplicationService
{
    Task<DocumentMetaDto> GetAsync(Guid baseId, Guid docId);
    
    // =========================
    // Tree & Basic Info
    // =========================

    /// <summary>
    /// GET /api/kbs/{baseId}/documents/tree
    /// </summary>
    Task<List<DocumentTreeNodeDto>> GetTreeAsync(Guid baseId);

    /// <summary>
    /// GET /api/kbs/{baseId}/documents/children?parentId=
    /// </summary>
    Task<List<DocumentDto>> GetChildrenAsync(Guid baseId, Guid? parentId);

    // =========================
    // CRUD
    // =========================

    /// <summary>
    /// POST /api/kbs/{baseId}/documents
    /// </summary>
    Task<DocumentDto> CreateAsync(Guid baseId, DocumentCreateDto input);

    /// <summary>
    /// PUT /api/kbs/{baseId}/documents/{id}/rename
    /// </summary>
    Task<DocumentDto> RenameAsync(Guid baseId, Guid id, DocumentRenameDto input);

    /// <summary>
    /// PUT /api/kbs/{baseId}/documents/{id}/move
    /// </summary>
    Task<DocumentDto> MoveAsync(Guid baseId, Guid id, DocumentMoveDto input);

    /// <summary>
    /// DELETE /api/kbs/{baseId}/documents/{id}?includeChildren=true
    /// </summary>
    Task DeleteAsync(Guid baseId, Guid id, bool includeChildren = false);

    // =========================
    // Content
    // =========================

    /// <summary>
    /// GET /api/kbs/{baseId}/documents/{id}/content
    /// </summary>
    Task<DocumentContentDto?> GetContentAsync(Guid baseId, Guid id);

    /// <summary>
    /// PUT /api/kbs/{baseId}/documents/{id}/content
    /// </summary>
    Task SaveContentAsync(Guid baseId, Guid id, SaveDocumentContentInput input);

    // =========================
    // Breadcrumb
    // =========================

    /// <summary>
    /// GET /api/kbs/{baseId}/documents/{id}/breadcrumb
    /// </summary>
    Task<List<DocumentBreadcrumbItemDto>> GetBreadcrumbAsync(Guid baseId, Guid id);
}
