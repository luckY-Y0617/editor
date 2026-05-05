using System;
using System.Collections.Generic;

namespace NS.Module.Knowledge.Application.Contracts.Tags.Dtos;

public class SetDocumentTagsInput
{
    public Guid DocumentId { get; set; }
    public List<Guid> TagIds { get; set; } = new();
}