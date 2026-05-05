using System;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Versions.Dtos;

public class DocumentVersionDto : CreationAuditedEntityDto<Guid>
{
    public Guid DocumentId { get; set; }
    public string SnapshotJson { get; set; } = default!;
    public string? SnapshotHtml { get; set; }
    public int WordCount { get; set; }
    public string? ChangeSummary { get; set; }
    public DocumentVersionSource Source { get; set; }
    public int VersionNumber { get; set; }
}