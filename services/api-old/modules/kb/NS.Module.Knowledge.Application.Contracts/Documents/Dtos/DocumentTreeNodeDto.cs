using System.Collections.Generic;

namespace NS.Module.Knowledge.Application.Contracts.Documents.Dtos;

/// <summary>
/// 树节点结构
/// </summary>
public class DocumentTreeNodeDto : DocumentDto
{
    public List<DocumentTreeNodeDto> Children { get; set; } = new();
}