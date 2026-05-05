using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Knowledge.Application.Contracts.KnowledgeBases.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.KnowledgeBases;

public interface IKnowledgeBaseAppService
{
    Task<List<KnowledgeBaseDto>> GetListAsync(
        KnowledgeBaseGetListInput input,
        CancellationToken cancellationToken = default);

    Task<KnowledgeBaseContextDto> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);
    

    Task<KnowledgeBaseDto> CreateAsync(
        KnowledgeBaseCreateUpdateDto input,
        CancellationToken cancellationToken = default);

    Task<KnowledgeBaseDto> UpdateAsync(
        Guid id,
        KnowledgeBaseCreateUpdateDto input,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}