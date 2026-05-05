using System;

namespace NS.Module.Knowledge.Domain.Shared.Events;

public sealed class DocumentCreatedEto
{
    public Guid TenantId { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;

    public Guid KnowledgeBaseId { get; set; }
    public string KnowledgeBaseName { get; set; } = string.Empty;

    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }

    public string DedupKey => $"knowledge:doc:{DocumentId}:created";
}