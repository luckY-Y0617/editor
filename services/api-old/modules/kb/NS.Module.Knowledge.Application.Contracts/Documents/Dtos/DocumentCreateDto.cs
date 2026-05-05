using System;
using NS.Module.Knowledge.Domain.Shared.Enums;

namespace NS.Module.Knowledge.Application.Contracts.Documents.Dtos;

public class DocumentCreateDto
{
    public Guid? ParentId { get; set; }
    public string Title { get; set; } = default!;
    public DocumentType Type { get; set; } = DocumentType.Normal;
    public string? InitialContentJson { get; set; }
}