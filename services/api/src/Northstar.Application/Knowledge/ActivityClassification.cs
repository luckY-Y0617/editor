using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Activity;

namespace Northstar.Application.Knowledge;

public static class ActivityClassification
{
    public const string CategoryDocument = "document";

    public const string SignalLow = "low";
    public const string SignalNormal = "normal";
    public const string SignalHigh = "high";

    public const string SurfaceActivity = "activity";
    public const string SurfaceNotification = "notification";
    public const string SurfaceAudit = "audit";
    public const string SurfacePresence = "presence";

    private static readonly IReadOnlyList<string> ActivityOnly = [SurfaceActivity];

    public static ActivityClassificationDto ClassifyDocumentActivity(
        string action,
        Guid documentId,
        Guid? actorId)
    {
        return action switch
        {
            ActivityActions.DocumentUpdated => new ActivityClassificationDto(
                CategoryDocument,
                SignalLow,
                ActivityOnly,
                IsNotificationCandidate: false,
                IsAuditCandidate: false,
                IsCoalescible: true,
                BuildCoalescingKey(actorId, documentId, ActivityActions.DocumentUpdated)),

            ActivityActions.DocumentCreated or ActivityActions.DocumentImported => new ActivityClassificationDto(
                CategoryDocument,
                SignalNormal,
                ActivityOnly,
                IsNotificationCandidate: false,
                IsAuditCandidate: false,
                IsCoalescible: false,
                CoalescingKey: null),

            ActivityActions.DocumentMoved or
            ActivityActions.DocumentTagsUpdated or
            ActivityActions.DocumentArchived or
            ActivityActions.DocumentRestored or
            ActivityActions.DocumentDeleted or
            ActivityActions.DocumentVersionPublished or
            ActivityActions.DocumentVersionUnpublished or
            ActivityActions.DocumentVersionRestored => new ActivityClassificationDto(
                CategoryDocument,
                SignalHigh,
                ActivityOnly,
                IsNotificationCandidate: false,
                IsAuditCandidate: action is ActivityActions.DocumentArchived or ActivityActions.DocumentRestored or ActivityActions.DocumentDeleted,
                IsCoalescible: false,
                CoalescingKey: null),

            _ => new ActivityClassificationDto(
                CategoryDocument,
                SignalNormal,
                ActivityOnly,
                IsNotificationCandidate: false,
                IsAuditCandidate: false,
                IsCoalescible: false,
                CoalescingKey: null)
        };
    }

    private static string BuildCoalescingKey(Guid? actorId, Guid documentId, string action)
    {
        var actor = actorId?.ToString("N") ?? "system";
        return $"activity:{actor}:document:{documentId:N}:{action}";
    }
}
