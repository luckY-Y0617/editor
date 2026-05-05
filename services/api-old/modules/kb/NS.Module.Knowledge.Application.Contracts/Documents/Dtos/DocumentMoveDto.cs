using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Documents.Dtos;

public class DocumentMoveDto : EntityDto<Guid>
{
    public Guid? NewParentId { get; set; }
    public int NewOrder { get; set; }
}