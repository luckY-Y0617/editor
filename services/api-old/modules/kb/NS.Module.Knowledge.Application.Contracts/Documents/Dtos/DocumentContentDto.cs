using System;

namespace NS.Module.Knowledge.Application.Contracts.Documents.Dtos;

/// <summary>
/// 文档内容（Tiptap JSON / HTML）
/// </summary>
public class DocumentContentDto
{
    public Guid DocumentId { get; set; }
    public string ContentJson { get; set; } = default!;
    public string? ContentHtml { get; set; }
    
    
    public DateTime UpdatedTime { get; set; }
}