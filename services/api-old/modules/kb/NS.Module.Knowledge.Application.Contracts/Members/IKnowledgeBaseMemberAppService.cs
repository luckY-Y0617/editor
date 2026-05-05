using System;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Knowledge.Application.Contracts.Members.Dtos;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Members;

public interface IKnowledgeBaseMemberAppService
{
    Task<ListResultDto<KnowledgeBaseMemberDto>> GetListAsync(
        Guid knowledgeBaseId,
        CancellationToken cancellationToken = default);

    Task<KnowledgeBaseMemberDto> AddOrUpdateAsync(
        Guid knowledgeBaseId,
        KnowledgeBaseMemberCreateDto input,
        CancellationToken cancellationToken = default);

    Task ChangeRoleAsync(
        Guid knowledgeBaseId,
        Guid userId,
        KnowledgeBaseMemberUpdateRoleDto input,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid knowledgeBaseId,
        Guid userId,
        CancellationToken cancellationToken = default);
}