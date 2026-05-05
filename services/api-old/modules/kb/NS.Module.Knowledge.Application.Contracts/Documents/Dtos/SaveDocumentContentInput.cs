using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Documents.Dtos;

/// <summary>
/// 保存内容输入模型
/// </summary>
public class SaveDocumentContentInput : EntityDto<Guid>
{
    public string ContentJson { get; set; } = default!;
    public string? ContentHtml { get; set; }
    public string? PlainText { get; set; }

    public bool IsAutoSave { get; set; }
    public string? ChangeSummary { get; set; }
}