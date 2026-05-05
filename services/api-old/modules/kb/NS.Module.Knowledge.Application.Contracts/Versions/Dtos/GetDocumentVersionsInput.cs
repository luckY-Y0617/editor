using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Versions.Dtos;

public class GetDocumentVersionsInput : PagedAndSortedResultRequestDto
{
    public Guid DocumentId { get; set; }
}