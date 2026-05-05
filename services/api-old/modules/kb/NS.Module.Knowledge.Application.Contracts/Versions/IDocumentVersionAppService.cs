using System;
using System.Threading.Tasks;
using NS.Module.Knowledge.Application.Contracts.Documents.Dtos;
using NS.Module.Knowledge.Application.Contracts.Versions.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Knowledge.Application.Contracts.Versions;

/// <summary>
/// 文档版本应用服务
/// - 版本是 Document 的子资源
/// - 权限最终裁决落在 KnowledgeBase（BaseId）
/// </summary>
public interface IDocumentVersionAppService : IApplicationService
{
    /// <summary>
    /// 获取指定文档的版本列表
    /// </summary>
    Task<PagedResultDto<DocumentVersionDto>> GetListAsync(
        Guid baseId,
        Guid documentId,
        GetDocumentVersionsInput input);

    /// <summary>
    /// 获取单个文档版本详情
    /// </summary>
    Task<DocumentVersionDto> GetAsync(
        Guid baseId,
        Guid documentId,
        Guid versionId);

    /// <summary>
    /// 从历史版本恢复文档
    /// </summary>
    Task<DocumentDetailDto> RestoreAsync(
        Guid baseId,
        Guid documentId,
        Guid versionId);
}